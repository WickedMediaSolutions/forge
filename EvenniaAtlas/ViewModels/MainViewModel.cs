
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using EvenniaAtlas.Models;
using EvenniaAtlas.Services;
using Microsoft.Win32;

namespace EvenniaAtlas.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly ProjectFileService _fileService = new();
    private readonly EvenniaExportService _exportService = new();
    private MapProject _project = new();
    private string? _currentFilePath;
    private bool _isDirty;
    private int _roomCounter;
    private int _connectionCounter;
    private int _spawnCounter = 1;
    private readonly Stack<UndoAction> _undoStack = new();
    private readonly Stack<UndoAction> _redoStack = new();
    private bool _firstRoomPrompted;
    // Property-edit coalescing: maps property keys to pre-edit values
    private readonly Dictionary<string, object?> _beforeValues = new();

    public MainViewModel()
    {
        Rooms = new ObservableCollection<RoomModel>();
        Connections = new ObservableCollection<ConnectionModel>();

        AddItemSpawnCommand = new RelayCommand(_ => AddItemSpawn(), _ => SelectedRoom != null && _project != null);
        RemoveItemSpawnCommand = new RelayCommand(_ => RemoveItemSpawn(), _ => HasSelectedItemSpawn);
        AddNpcSpawnCommand = new RelayCommand(_ => AddNpcSpawn(), _ => SelectedRoom != null && _project != null);
        RemoveNpcSpawnCommand = new RelayCommand(_ => RemoveNpcSpawn(), _ => HasSelectedNpcSpawn);

        StartNewProject();
    }

    public ObservableCollection<RoomModel> Rooms { get; }
    public ObservableCollection<ConnectionModel> Connections { get; }
    public ObservableCollection<SpawnEntryViewModel> SelectedRoomItemSpawns { get; } = new();
    public ObservableCollection<SpawnEntryViewModel> SelectedRoomNpcSpawns { get; } = new();
    public MapProject CurrentProject => _project;
    public IEnumerable<ItemModel> AvailableItems => _project?.Items ?? Enumerable.Empty<ItemModel>();
    public IEnumerable<NpcModel> AvailableNpcs => _project?.Npcs ?? Enumerable.Empty<NpcModel>();
    public event Action? MapNeedsRefresh;

    private int _currentZ;
    public int CurrentZ { get => _currentZ; set { SetField(ref _currentZ, value); OnPropertyChanged(nameof(FloorLabel)); RefreshMap(); } }
    public string FloorLabel => "Floor " + _currentZ;

    // Zoom constants
    public const double ZoomMin = 0.25;
    public const double ZoomMax = 3.0;
    public const double ZoomDefault = 1.0;
    public const double ZoomIncrement = 0.10;

    private double _zoomScale = ZoomDefault;
    public double ZoomScale
    {
        get => _zoomScale;
        set
        {
            var clamped = Math.Clamp(value, ZoomMin, ZoomMax);
            if (SetField(ref _zoomScale, clamped))
            {
                OnPropertyChanged(nameof(ZoomPercentage));
                RefreshMap();
            }
        }
    }
    public string ZoomPercentage => $"{_zoomScale * 100:F0}%";
    public void ZoomIn()  => ZoomScale += ZoomIncrement;
    public void ZoomOut() => ZoomScale -= ZoomIncrement;

    private bool _buildMode;
    public bool BuildMode { get => _buildMode; set { SetField(ref _buildMode, value); OnPropertyChanged(nameof(BuildModeText)); } }
    public string BuildModeText => _buildMode ? "Build: ON" : "Build: OFF";

    private bool _autoReverse = true;
    public bool AutoReverse { get => _autoReverse; set { SetField(ref _autoReverse, value); OnPropertyChanged(nameof(AutoReverseText)); } }
    public string AutoReverseText => _autoReverse ? "Auto-R: ON" : "Auto-R: OFF";

    public string DefaultRoomTitle { get => _project.DefaultRoomTitle; set { _project.DefaultRoomTitle = value ?? "Room"; MarkDirty(); OnPropertyChanged(); } }

    public string ProjectName => _project.Name;
    public string ProjectId => _project.Id;
    public bool IsDirty => _isDirty;
    public string TitleBarText { get { var n = string.IsNullOrEmpty(_project.Name) ? "Untitled" : _project.Name; return "Rites of Passage: The Forge - " + n + (_isDirty ? " *" : ""); } }
    public string StatusText { get { var sel = SelectedRoom; if (sel != null) return "Room: " + sel.Id + "  (" + sel.X + ", " + sel.Y + ", " + sel.Z + ")"; return "Rooms: " + Rooms.Count + "  Exits: " + Connections.Count + "  " + FloorLabel; } }

    private RoomModel? _selectedRoom;
    public RoomModel? SelectedRoom { get => _selectedRoom; set { if (!SetField(ref _selectedRoom, value)) return; SelectedConnection = null; SelectedItemSpawn = null; SelectedNpcSpawn = null; SyncSelectedRoomSpawns(); FireRoomPropsChanged(); OnPropertyChanged(nameof(StatusText)); } }

    private ConnectionModel? _selectedConnection;
    public ConnectionModel? SelectedConnection { get => _selectedConnection; set { if (!SetField(ref _selectedConnection, value)) return; if (value != null && _selectedRoom != null) { _selectedRoom = null; FireRoomPropsChanged(); } OnPropertyChanged(nameof(IsRoomSelected)); OnPropertyChanged(nameof(IsConnectionSelected)); OnPropertyChanged(nameof(StatusText)); FireConnPropsChanged(); } }

    public bool IsRoomSelected => _selectedRoom != null;
    public bool IsConnectionSelected => _selectedConnection != null;
    public bool IsDoorSelected => SelectedConnection?.ExitType == ExitType.Door;

    private SpawnEntryViewModel? _selectedItemSpawn;
    public SpawnEntryViewModel? SelectedItemSpawn
    {
        get => _selectedItemSpawn;
        set
        {
            if (!SetField(ref _selectedItemSpawn, value)) return;
            OnPropertyChanged(nameof(HasSelectedItemSpawn));
        }
    }
    public bool HasSelectedItemSpawn => _selectedItemSpawn != null;

    private SpawnEntryViewModel? _selectedNpcSpawn;
    public SpawnEntryViewModel? SelectedNpcSpawn
    {
        get => _selectedNpcSpawn;
        set
        {
            if (!SetField(ref _selectedNpcSpawn, value)) return;
            OnPropertyChanged(nameof(HasSelectedNpcSpawn));
        }
    }
    public bool HasSelectedNpcSpawn => _selectedNpcSpawn != null;

    // ---- Spawn commands ----

    public ICommand AddItemSpawnCommand { get; }
    public ICommand RemoveItemSpawnCommand { get; }
    public ICommand AddNpcSpawnCommand { get; }
    public ICommand RemoveNpcSpawnCommand { get; }

    public bool ShowDoorProperties => SelectedConnection?.ExitType == ExitType.Door;

    public string? SelectedRoomTitle { get => _selectedRoom?.Title; set { if (_selectedRoom != null) { _selectedRoom.Title = value ?? ""; MarkDirty(); OnPropertyChanged(); RefreshStatus(); } } }
    public string? SelectedRoomType { get => _selectedRoom?.RoomType; set { if (_selectedRoom != null) { _selectedRoom.RoomType = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }
    public string? SelectedRoomDescription { get => _selectedRoom?.Description; set { if (_selectedRoom != null) { _selectedRoom.Description = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }
    public string? SelectedRoomTags { get => _selectedRoom?.TagsString; set { if (_selectedRoom != null) { _selectedRoom.TagsString = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }
    public string? SelectedRoomNotes { get => _selectedRoom?.Notes; set { if (_selectedRoom != null) { _selectedRoom.Notes = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }

    // Evennia-native room properties
    public string? SelectedRoomTypeclassPath { get => _selectedRoom?.TypeclassPath; set { if (_selectedRoom != null) { _selectedRoom.TypeclassPath = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }
    public string? SelectedRoomLockString { get => _selectedRoom?.LockString; set { if (_selectedRoom != null) { _selectedRoom.LockString = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }

    public ObservableCollection<AliasModel>? RoomAliases => _selectedRoom?.Aliases;
    public ObservableCollection<TagModel>? RoomEvenniaTags => _selectedRoom?.EvenniaTags;
    public ObservableCollection<AttributeModel>? RoomAttributes => _selectedRoom?.Attributes;
    public ObservableCollection<string>? RoomPermissions => _selectedRoom?.Permissions;

    public void AddRoomAlias() { if (_selectedRoom == null) return; var alias = new AliasModel(); var idx = _selectedRoom.Aliases.Count; _selectedRoom.Aliases.Add(alias); MarkDirty(); PushUndo("Add Room Alias", () => { _selectedRoom.Aliases.Remove(alias); MarkDirty(); }, () => { _selectedRoom.Aliases.Add(alias); MarkDirty(); }); }
    public void RemoveRoomAlias(AliasModel a) { if (_selectedRoom == null) return; var idx = _selectedRoom.Aliases.IndexOf(a); if (idx < 0) return; var clone = new AliasModel { Key = a.Key, Category = a.Category }; _selectedRoom.Aliases.RemoveAt(idx); MarkDirty(); PushUndo("Remove Room Alias", () => { _selectedRoom.Aliases.Insert(Math.Min(idx, _selectedRoom.Aliases.Count), clone); MarkDirty(); }, () => { _selectedRoom.Aliases.Remove(clone); MarkDirty(); }); }
    public void AddRoomEvenniaTag() { if (_selectedRoom == null) return; var tag = new TagModel(); var idx = _selectedRoom.EvenniaTags.Count; _selectedRoom.EvenniaTags.Add(tag); MarkDirty(); PushUndo("Add Room Tag", () => { _selectedRoom.EvenniaTags.Remove(tag); MarkDirty(); }, () => { _selectedRoom.EvenniaTags.Add(tag); MarkDirty(); }); }
    public void RemoveRoomEvenniaTag(TagModel t) { if (_selectedRoom == null) return; var idx = _selectedRoom.EvenniaTags.IndexOf(t); if (idx < 0) return; var clone = new TagModel { Key = t.Key, Category = t.Category, Data = t.Data }; _selectedRoom.EvenniaTags.RemoveAt(idx); MarkDirty(); PushUndo("Remove Room Tag", () => { _selectedRoom.EvenniaTags.Insert(Math.Min(idx, _selectedRoom.EvenniaTags.Count), clone); MarkDirty(); }, () => { _selectedRoom.EvenniaTags.Remove(clone); MarkDirty(); }); }
    public void AddRoomAttribute() { if (_selectedRoom == null) return; var attr = new AttributeModel(); var idx = _selectedRoom.Attributes.Count; _selectedRoom.Attributes.Add(attr); MarkDirty(); PushUndo("Add Room Attribute", () => { _selectedRoom.Attributes.Remove(attr); MarkDirty(); }, () => { _selectedRoom.Attributes.Add(attr); MarkDirty(); }); }
    public void RemoveRoomAttribute(AttributeModel a) { if (_selectedRoom == null) return; var idx = _selectedRoom.Attributes.IndexOf(a); if (idx < 0) return; var clone = new AttributeModel { Key = a.Key, Value = a.Value, Category = a.Category, LockString = a.LockString }; _selectedRoom.Attributes.RemoveAt(idx); MarkDirty(); PushUndo("Remove Room Attribute", () => { _selectedRoom.Attributes.Insert(Math.Min(idx, _selectedRoom.Attributes.Count), clone); MarkDirty(); }, () => { _selectedRoom.Attributes.Remove(clone); MarkDirty(); }); }
    public void AddRoomPermission() { if (_selectedRoom == null) return; var perm = ""; var idx = _selectedRoom.Permissions.Count; _selectedRoom.Permissions.Add(perm); MarkDirty(); PushUndo("Add Room Permission", () => { _selectedRoom.Permissions.RemoveAt(idx); MarkDirty(); }, () => { _selectedRoom.Permissions.Insert(Math.Min(idx, _selectedRoom.Permissions.Count), perm); MarkDirty(); }); }
    public void RemoveRoomPermission(string p) { if (_selectedRoom == null) return; var idx = _selectedRoom.Permissions.IndexOf(p); if (idx < 0) return; _selectedRoom.Permissions.RemoveAt(idx); MarkDirty(); PushUndo("Remove Room Permission", () => { _selectedRoom.Permissions.Insert(Math.Min(idx, _selectedRoom.Permissions.Count), p); MarkDirty(); }, () => { _selectedRoom.Permissions.RemoveAt(idx); MarkDirty(); }); }

    public int SelectedRoomX
    {
        get => _selectedRoom?.X ?? 0;
        set { if (_selectedRoom != null && !MoveRoom(_selectedRoom, value, _selectedRoom.Y, _selectedRoom.Z)) OnPropertyChanged(nameof(SelectedRoomX)); }
    }
    public int SelectedRoomY
    {
        get => _selectedRoom?.Y ?? 0;
        set { if (_selectedRoom != null && !MoveRoom(_selectedRoom, _selectedRoom.X, value, _selectedRoom.Z)) OnPropertyChanged(nameof(SelectedRoomY)); }
    }
    public int SelectedRoomZ
    {
        get => _selectedRoom?.Z ?? 0;
        set { if (_selectedRoom != null && !MoveRoom(_selectedRoom, _selectedRoom.X, _selectedRoom.Y, value)) OnPropertyChanged(nameof(SelectedRoomZ)); }
    }

    public ObservableCollection<ConnectionModel> RoomExits
    { get { var list = new ObservableCollection<ConnectionModel>(); if (_selectedRoom != null) foreach (var c in Connections.Where(c => c.SourceRoomId == _selectedRoom.Id)) list.Add(c); return list; } }

    public Direction SelectedConnectionDirection { get => SelectedConnection?.Direction ?? Direction.North; set { if (SelectedConnection != null) { var oldDir = SelectedConnection.Direction; var oldRev = SelectedConnection.ReverseDirection; SelectedConnection.Direction = value; SelectedConnection.ReverseDirection = DirectionHelper.GetReverseDirection(value); MarkDirty(); OnPropertyChanged(); PushUndo("Change Direction", () => { SelectedConnection.Direction = oldDir; SelectedConnection.ReverseDirection = oldRev; MarkDirty(); OnPropertyChanged(); }, () => { SelectedConnection.Direction = value; SelectedConnection.ReverseDirection = DirectionHelper.GetReverseDirection(value); MarkDirty(); OnPropertyChanged(); }); } } }
    public string? SelectedConnectionAliases { get => SelectedConnection?.Aliases; set { if (SelectedConnection != null) { SelectedConnection.Aliases = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }
    public ExitType SelectedConnectionExitType { get => SelectedConnection?.ExitType ?? ExitType.Normal; set { if (SelectedConnection == null) return; var oldType = SelectedConnection.ExitType; var oldDoor = SelectedConnection.Door != null ? new DoorModel { Name = SelectedConnection.Door.Name, Typeclass = SelectedConnection.Door.Typeclass, StartsOpen = SelectedConnection.Door.StartsOpen, StartsClosed = SelectedConnection.Door.StartsClosed, StartsLocked = SelectedConnection.Door.StartsLocked, Lockable = SelectedConnection.Door.Lockable, KeyId = SelectedConnection.Door.KeyId, SynchronizeOpposite = SelectedConnection.Door.SynchronizeOpposite, TraverseLockString = SelectedConnection.Door.TraverseLockString, LockedFailureMessage = SelectedConnection.Door.LockedFailureMessage, ClosedFailureMessage = SelectedConnection.Door.ClosedFailureMessage, Description = SelectedConnection.Door.Description } : null; ConnectionModel? rev = null; DoorModel? oldRevDoor = null; if (SelectedConnection.Door?.SynchronizeOpposite == true) { rev = FindReverseConnection(SelectedConnection); if (rev?.Door != null) oldRevDoor = new DoorModel { Name = rev.Door.Name, Typeclass = rev.Door.Typeclass, StartsOpen = rev.Door.StartsOpen, StartsClosed = rev.Door.StartsClosed, StartsLocked = rev.Door.StartsLocked, Lockable = rev.Door.Lockable, KeyId = rev.Door.KeyId, SynchronizeOpposite = rev.Door.SynchronizeOpposite, TraverseLockString = rev.Door.TraverseLockString, LockedFailureMessage = rev.Door.LockedFailureMessage, ClosedFailureMessage = rev.Door.ClosedFailureMessage, Description = rev.Door.Description }; } SelectedConnection.ExitType = value; if (value == ExitType.Door && SelectedConnection.Door == null) SelectedConnection.Door = new DoorModel(); MarkDirty(); OnPropertyChanged(nameof(IsDoorSelected)); OnPropertyChanged(nameof(ShowDoorProperties)); SyncDoorWithReverse(SelectedConnection); var newType = value; var newDoor = SelectedConnection.Door != null ? new DoorModel { Name = SelectedConnection.Door.Name, Typeclass = SelectedConnection.Door.Typeclass, StartsOpen = SelectedConnection.Door.StartsOpen, StartsClosed = SelectedConnection.Door.StartsClosed, StartsLocked = SelectedConnection.Door.StartsLocked, Lockable = SelectedConnection.Door.Lockable, KeyId = SelectedConnection.Door.KeyId, SynchronizeOpposite = SelectedConnection.Door.SynchronizeOpposite, TraverseLockString = SelectedConnection.Door.TraverseLockString, LockedFailureMessage = SelectedConnection.Door.LockedFailureMessage, ClosedFailureMessage = SelectedConnection.Door.ClosedFailureMessage, Description = SelectedConnection.Door.Description } : null; DoorModel? newRevDoor = null; if (SelectedConnection.Door?.SynchronizeOpposite == true && rev != null) { rev = FindReverseConnection(SelectedConnection); if (rev?.Door != null) newRevDoor = new DoorModel { Name = rev.Door.Name, Typeclass = rev.Door.Typeclass, StartsOpen = rev.Door.StartsOpen, StartsClosed = rev.Door.StartsClosed, StartsLocked = rev.Door.StartsLocked, Lockable = rev.Door.Lockable, KeyId = rev.Door.KeyId, SynchronizeOpposite = rev.Door.SynchronizeOpposite, TraverseLockString = rev.Door.TraverseLockString, LockedFailureMessage = rev.Door.LockedFailureMessage, ClosedFailureMessage = rev.Door.ClosedFailureMessage, Description = rev.Door.Description }; } PushUndo("Change Exit Type", () => { SelectedConnection.ExitType = oldType; SelectedConnection.Door = oldDoor; if (rev != null) rev.Door = oldRevDoor; MarkDirty(); OnPropertyChanged(nameof(IsDoorSelected)); OnPropertyChanged(nameof(ShowDoorProperties)); }, () => { SelectedConnection.ExitType = newType; SelectedConnection.Door = newDoor; if (rev != null) rev.Door = newRevDoor; MarkDirty(); OnPropertyChanged(nameof(IsDoorSelected)); OnPropertyChanged(nameof(ShowDoorProperties)); }); } }
    public bool SelectedConnectionIsOneWay { get => SelectedConnection?.IsOneWay ?? false; set { if (SelectedConnection != null) { var old = SelectedConnection.IsOneWay; SelectedConnection.IsOneWay = value; MarkDirty(); OnPropertyChanged(); PushUndo("Toggle One-Way", () => { SelectedConnection.IsOneWay = old; MarkDirty(); OnPropertyChanged(); }, () => { SelectedConnection.IsOneWay = value; MarkDirty(); OnPropertyChanged(); }); } } }
    public string? SelectedConnectionDoorName { get => SelectedConnection?.Door?.Name; set { if (SelectedConnection?.Door != null) { SelectedConnection.Door.Name = value ?? ""; MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); } } }
    public bool SelectedConnectionStartsOpen { get => SelectedConnection?.Door?.StartsOpen ?? false; set { if (SelectedConnection?.Door == null) return; var door = SelectedConnection.Door; var oldOpen = door.StartsOpen; var oldClosed = door.StartsClosed; var syn = door.SynchronizeOpposite; ConnectionModel? rev = null; bool? oldRevOpen = null; bool? oldRevClosed = null; if (syn) { rev = FindReverseConnection(SelectedConnection); if (rev?.Door != null) { oldRevOpen = rev.Door.StartsOpen; oldRevClosed = rev.Door.StartsClosed; } } door.StartsOpen = value; if (value) door.StartsClosed = false; MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); PushUndo("Toggle Starts Open", () => { door.StartsOpen = oldOpen; door.StartsClosed = oldClosed; if (syn && rev?.Door != null) { rev.Door.StartsOpen = oldRevOpen!.Value; rev.Door.StartsClosed = oldRevClosed!.Value; } MarkDirty(); OnPropertyChanged(); }, () => { door.StartsOpen = value; if (value) door.StartsClosed = false; SyncDoorWithReverse(SelectedConnection); MarkDirty(); OnPropertyChanged(); }); } }
    public bool SelectedConnectionStartsClosed { get => SelectedConnection?.Door?.StartsClosed ?? true; set { if (SelectedConnection?.Door == null) return; var door = SelectedConnection.Door; var oldC = door.StartsClosed; var oldO = door.StartsOpen; var syn = door.SynchronizeOpposite; ConnectionModel? rev = null; bool? oldRevC = null; bool? oldRevO = null; if (syn) { rev = FindReverseConnection(SelectedConnection); if (rev?.Door != null) { oldRevC = rev.Door.StartsClosed; oldRevO = rev.Door.StartsOpen; } } door.StartsClosed = value; if (value) door.StartsOpen = false; MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); PushUndo("Toggle Starts Closed", () => { door.StartsClosed = oldC; door.StartsOpen = oldO; if (syn && rev?.Door != null) { rev.Door.StartsClosed = oldRevC!.Value; rev.Door.StartsOpen = oldRevO!.Value; } MarkDirty(); OnPropertyChanged(); }, () => { door.StartsClosed = value; if (value) door.StartsOpen = false; SyncDoorWithReverse(SelectedConnection); MarkDirty(); OnPropertyChanged(); }); } }
    public bool SelectedConnectionStartsLocked { get => SelectedConnection?.Door?.StartsLocked ?? false; set { if (SelectedConnection?.Door == null) return; var door = SelectedConnection.Door; var oldL = door.StartsLocked; var oldC = door.StartsClosed; var oldO = door.StartsOpen; var (oldCur, oldRev) = SnapshotPairedDoor(SelectedConnection); door.StartsLocked = value; if (value) { door.StartsClosed = true; door.StartsOpen = false; } MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); PushUndo("Toggle Starts Locked", () => { door.StartsLocked = oldL; door.StartsClosed = oldC; door.StartsOpen = oldO; if (oldRev != null) { var rev = FindReverseConnection(SelectedConnection); if (rev?.Door != null) { rev.Door.StartsLocked = oldRev.StartsLocked; rev.Door.StartsClosed = oldRev.StartsClosed; rev.Door.StartsOpen = oldRev.StartsOpen; } } MarkDirty(); OnPropertyChanged(); }, () => { door.StartsLocked = value; if (value) { door.StartsClosed = true; door.StartsOpen = false; } SyncDoorWithReverse(SelectedConnection); MarkDirty(); OnPropertyChanged(); }); } }
    public string? SelectedConnectionKeyId { get => SelectedConnection?.Door?.KeyId; set { if (SelectedConnection?.Door != null) { SelectedConnection.Door.KeyId = value ?? ""; MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); } } }

    // Expanded door model wrappers
    public string? SelectedDoorTypeclass { get => SelectedConnection?.Door?.Typeclass; set { if (SelectedConnection?.Door != null) { SelectedConnection.Door.Typeclass = value ?? ""; MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); } } }
    public bool SelectedDoorLockable { get => SelectedConnection?.Door?.Lockable ?? true; set { if (SelectedConnection?.Door == null) return; var door = SelectedConnection.Door; var old = door.Lockable; var (oldCur, oldRev) = SnapshotPairedDoor(SelectedConnection); door.Lockable = value; MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); PushUndo("Toggle Lockable", () => { door.Lockable = old; if (oldRev != null) { var rev = FindReverseConnection(SelectedConnection); if (rev?.Door != null) rev.Door.Lockable = oldRev.Lockable; } MarkDirty(); OnPropertyChanged(); }, () => { door.Lockable = value; SyncDoorWithReverse(SelectedConnection); MarkDirty(); OnPropertyChanged(); }); } }
    public bool SelectedDoorSynchronizeOpposite { get => SelectedConnection?.Door?.SynchronizeOpposite ?? true; set { if (SelectedConnection?.Door == null) return; var old = SelectedConnection.Door.SynchronizeOpposite; SelectedConnection.Door.SynchronizeOpposite = value; MarkDirty(); OnPropertyChanged(); PushUndo("Toggle Sync Opposite", () => { SelectedConnection.Door.SynchronizeOpposite = old; MarkDirty(); OnPropertyChanged(); }, () => { SelectedConnection.Door.SynchronizeOpposite = value; MarkDirty(); OnPropertyChanged(); }); } }
    public string? SelectedDoorTraverseLockString { get => SelectedConnection?.Door?.TraverseLockString; set { if (SelectedConnection?.Door != null) { SelectedConnection.Door.TraverseLockString = value ?? ""; MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); } } }
    public string? SelectedDoorLockedFailure { get => SelectedConnection?.Door?.LockedFailureMessage; set { if (SelectedConnection?.Door != null) { SelectedConnection.Door.LockedFailureMessage = value ?? ""; MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); } } }
    public string? SelectedDoorClosedFailure { get => SelectedConnection?.Door?.ClosedFailureMessage; set { if (SelectedConnection?.Door != null) { SelectedConnection.Door.ClosedFailureMessage = value ?? ""; MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); } } }
    public string? SelectedDoorDescription { get => SelectedConnection?.Door?.Description; set { if (SelectedConnection?.Door != null) { SelectedConnection.Door.Description = value ?? ""; MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); } } }

    public ObservableCollection<AliasModel>? DoorAliases => SelectedConnection?.Door?.DoorAliases;
    public ObservableCollection<TagModel>? DoorTags => SelectedConnection?.Door?.DoorTags;
    public ObservableCollection<AttributeModel>? DoorAttributes => SelectedConnection?.Door?.DoorAttributes;
    public ObservableCollection<string>? DoorPermissions => SelectedConnection?.Door?.DoorPermissions;

    public void AddDoorAlias() { if (SelectedConnection?.Door == null) return; var door = SelectedConnection.Door; var syn = door.SynchronizeOpposite; var rev = syn ? FindReverseConnection(SelectedConnection!) : null; var before = CloneAliases(door.DoorAliases); var revBefore = rev?.Door != null ? CloneAliases(rev.Door.DoorAliases) : null; door.DoorAliases.Add(new AliasModel()); MarkDirty(); SyncDoorWithReverse(SelectedConnection!); var after = CloneAliases(door.DoorAliases); var revAfter = rev?.Door != null ? CloneAliases(rev.Door.DoorAliases) : null; PushUndo("Add Door Alias", () => { door.DoorAliases = new ObservableCollection<AliasModel>(before); if (syn && rev?.Door != null) rev.Door.DoorAliases = new ObservableCollection<AliasModel>(revBefore!); MarkDirty(); }, () => { door.DoorAliases = new ObservableCollection<AliasModel>(after); if (syn && rev?.Door != null) rev.Door.DoorAliases = new ObservableCollection<AliasModel>(revAfter!); MarkDirty(); }); }
    public void RemoveDoorAlias(AliasModel a) { if (SelectedConnection?.Door == null) return; var door = SelectedConnection.Door; var idx = door.DoorAliases.IndexOf(a); if (idx < 0) return; var syn = door.SynchronizeOpposite; var rev = syn ? FindReverseConnection(SelectedConnection!) : null; var before = CloneAliases(door.DoorAliases); var revBefore = rev?.Door != null ? CloneAliases(rev.Door.DoorAliases) : null; door.DoorAliases.RemoveAt(idx); MarkDirty(); SyncDoorWithReverse(SelectedConnection!); var after = CloneAliases(door.DoorAliases); var revAfter = rev?.Door != null ? CloneAliases(rev.Door.DoorAliases) : null; PushUndo("Remove Door Alias", () => { door.DoorAliases = new ObservableCollection<AliasModel>(before); if (syn && rev?.Door != null) rev.Door.DoorAliases = new ObservableCollection<AliasModel>(revBefore!); MarkDirty(); }, () => { door.DoorAliases = new ObservableCollection<AliasModel>(after); if (syn && rev?.Door != null) rev.Door.DoorAliases = new ObservableCollection<AliasModel>(revAfter!); MarkDirty(); }); }
    public void AddDoorTag() { if (SelectedConnection?.Door == null) return; var door = SelectedConnection.Door; var syn = door.SynchronizeOpposite; var rev = syn ? FindReverseConnection(SelectedConnection!) : null; var before = CloneTags(door.DoorTags); var revBefore = rev?.Door != null ? CloneTags(rev.Door.DoorTags) : null; door.DoorTags.Add(new TagModel()); MarkDirty(); SyncDoorWithReverse(SelectedConnection!); var after = CloneTags(door.DoorTags); var revAfter = rev?.Door != null ? CloneTags(rev.Door.DoorTags) : null; PushUndo("Add Door Tag", () => { door.DoorTags = new ObservableCollection<TagModel>(before); if (syn && rev?.Door != null) rev.Door.DoorTags = new ObservableCollection<TagModel>(revBefore!); MarkDirty(); }, () => { door.DoorTags = new ObservableCollection<TagModel>(after); if (syn && rev?.Door != null) rev.Door.DoorTags = new ObservableCollection<TagModel>(revAfter!); MarkDirty(); }); }
    public void RemoveDoorTag(TagModel t) { if (SelectedConnection?.Door == null) return; var door = SelectedConnection.Door; var idx = door.DoorTags.IndexOf(t); if (idx < 0) return; var syn = door.SynchronizeOpposite; var rev = syn ? FindReverseConnection(SelectedConnection!) : null; var before = CloneTags(door.DoorTags); var revBefore = rev?.Door != null ? CloneTags(rev.Door.DoorTags) : null; door.DoorTags.RemoveAt(idx); MarkDirty(); SyncDoorWithReverse(SelectedConnection!); var after = CloneTags(door.DoorTags); var revAfter = rev?.Door != null ? CloneTags(rev.Door.DoorTags) : null; PushUndo("Remove Door Tag", () => { door.DoorTags = new ObservableCollection<TagModel>(before); if (syn && rev?.Door != null) rev.Door.DoorTags = new ObservableCollection<TagModel>(revBefore!); MarkDirty(); }, () => { door.DoorTags = new ObservableCollection<TagModel>(after); if (syn && rev?.Door != null) rev.Door.DoorTags = new ObservableCollection<TagModel>(revAfter!); MarkDirty(); }); }
    public void AddDoorAttribute() { if (SelectedConnection?.Door == null) return; var door = SelectedConnection.Door; var syn = door.SynchronizeOpposite; var rev = syn ? FindReverseConnection(SelectedConnection!) : null; var before = CloneAttrs(door.DoorAttributes); var revBefore = rev?.Door != null ? CloneAttrs(rev.Door.DoorAttributes) : null; door.DoorAttributes.Add(new AttributeModel()); MarkDirty(); SyncDoorWithReverse(SelectedConnection!); var after = CloneAttrs(door.DoorAttributes); var revAfter = rev?.Door != null ? CloneAttrs(rev.Door.DoorAttributes) : null; PushUndo("Add Door Attribute", () => { door.DoorAttributes = new ObservableCollection<AttributeModel>(before); if (syn && rev?.Door != null) rev.Door.DoorAttributes = new ObservableCollection<AttributeModel>(revBefore!); MarkDirty(); }, () => { door.DoorAttributes = new ObservableCollection<AttributeModel>(after); if (syn && rev?.Door != null) rev.Door.DoorAttributes = new ObservableCollection<AttributeModel>(revAfter!); MarkDirty(); }); }
    public void RemoveDoorAttribute(AttributeModel a) { if (SelectedConnection?.Door == null) return; var door = SelectedConnection.Door; var idx = door.DoorAttributes.IndexOf(a); if (idx < 0) return; var syn = door.SynchronizeOpposite; var rev = syn ? FindReverseConnection(SelectedConnection!) : null; var before = CloneAttrs(door.DoorAttributes); var revBefore = rev?.Door != null ? CloneAttrs(rev.Door.DoorAttributes) : null; door.DoorAttributes.RemoveAt(idx); MarkDirty(); SyncDoorWithReverse(SelectedConnection!); var after = CloneAttrs(door.DoorAttributes); var revAfter = rev?.Door != null ? CloneAttrs(rev.Door.DoorAttributes) : null; PushUndo("Remove Door Attribute", () => { door.DoorAttributes = new ObservableCollection<AttributeModel>(before); if (syn && rev?.Door != null) rev.Door.DoorAttributes = new ObservableCollection<AttributeModel>(revBefore!); MarkDirty(); }, () => { door.DoorAttributes = new ObservableCollection<AttributeModel>(after); if (syn && rev?.Door != null) rev.Door.DoorAttributes = new ObservableCollection<AttributeModel>(revAfter!); MarkDirty(); }); }
    public void AddDoorPermission() { if (SelectedConnection?.Door == null) return; var door = SelectedConnection.Door; var syn = door.SynchronizeOpposite; var rev = syn ? FindReverseConnection(SelectedConnection!) : null; var before = ClonePerms(door.DoorPermissions); var revBefore = rev?.Door != null ? ClonePerms(rev.Door.DoorPermissions) : null; door.DoorPermissions.Add(""); MarkDirty(); SyncDoorWithReverse(SelectedConnection!); var after = ClonePerms(door.DoorPermissions); var revAfter = rev?.Door != null ? ClonePerms(rev.Door.DoorPermissions) : null; PushUndo("Add Door Permission", () => { door.DoorPermissions = new ObservableCollection<string>(before); if (syn && rev?.Door != null) rev.Door.DoorPermissions = new ObservableCollection<string>(revBefore!); MarkDirty(); }, () => { door.DoorPermissions = new ObservableCollection<string>(after); if (syn && rev?.Door != null) rev.Door.DoorPermissions = new ObservableCollection<string>(revAfter!); MarkDirty(); }); }
    public void RemoveDoorPermission(string p) { if (SelectedConnection?.Door == null) return; var door = SelectedConnection.Door; var idx = door.DoorPermissions.IndexOf(p); if (idx < 0) return; var syn = door.SynchronizeOpposite; var rev = syn ? FindReverseConnection(SelectedConnection!) : null; var before = ClonePerms(door.DoorPermissions); var revBefore = rev?.Door != null ? ClonePerms(rev.Door.DoorPermissions) : null; door.DoorPermissions.RemoveAt(idx); MarkDirty(); SyncDoorWithReverse(SelectedConnection!); var after = ClonePerms(door.DoorPermissions); var revAfter = rev?.Door != null ? ClonePerms(rev.Door.DoorPermissions) : null; PushUndo("Remove Door Permission", () => { door.DoorPermissions = new ObservableCollection<string>(before); if (syn && rev?.Door != null) rev.Door.DoorPermissions = new ObservableCollection<string>(revBefore!); MarkDirty(); }, () => { door.DoorPermissions = new ObservableCollection<string>(after); if (syn && rev?.Door != null) rev.Door.DoorPermissions = new ObservableCollection<string>(revAfter!); MarkDirty(); }); }
    public string SelectedConnectionSrcId => SelectedConnection?.SourceRoomId ?? "";
    public string SelectedConnectionDstId => SelectedConnection?.DestinationRoomId ?? "";

    // Evennia-native connection properties
    public string? SelectedConnectionKeyName { get => SelectedConnection?.KeyName; set { if (SelectedConnection != null) { SelectedConnection.KeyName = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }
    public string? SelectedConnectionDescription { get => SelectedConnection?.Description; set { if (SelectedConnection != null) { SelectedConnection.Description = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }
    public string? SelectedConnectionTypeclassPath { get => SelectedConnection?.TypeclassPath; set { if (SelectedConnection != null) { SelectedConnection.TypeclassPath = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }
    public string? SelectedConnectionLockString { get => SelectedConnection?.LockString; set { if (SelectedConnection != null) { SelectedConnection.LockString = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }

    public ObservableCollection<AliasModel>? ConnectionAliases => SelectedConnection?.AliasesList;
    public ObservableCollection<TagModel>? ConnectionTags => SelectedConnection?.EvenniaTags;
    public ObservableCollection<AttributeModel>? ConnectionAttributes => SelectedConnection?.Attributes;
    public ObservableCollection<string>? ConnectionPermissions => SelectedConnection?.Permissions;

    public void AddConnectionAlias() { if (SelectedConnection == null) return; var alias = new AliasModel(); var idx = SelectedConnection.AliasesList.Count; SelectedConnection.AliasesList.Add(alias); MarkDirty(); PushUndo("Add Exit Alias", () => { SelectedConnection.AliasesList.Remove(alias); MarkDirty(); }, () => { SelectedConnection.AliasesList.Add(alias); MarkDirty(); }); }
    public void RemoveConnectionAlias(AliasModel a) { if (SelectedConnection == null) return; var idx = SelectedConnection.AliasesList.IndexOf(a); if (idx < 0) return; var clone = new AliasModel { Key = a.Key, Category = a.Category }; SelectedConnection.AliasesList.RemoveAt(idx); MarkDirty(); PushUndo("Remove Exit Alias", () => { SelectedConnection.AliasesList.Insert(Math.Min(idx, SelectedConnection.AliasesList.Count), clone); MarkDirty(); }, () => { SelectedConnection.AliasesList.Remove(clone); MarkDirty(); }); }
    public void AddConnectionTag() { if (SelectedConnection == null) return; var tag = new TagModel(); var idx = SelectedConnection.EvenniaTags.Count; SelectedConnection.EvenniaTags.Add(tag); MarkDirty(); PushUndo("Add Exit Tag", () => { SelectedConnection.EvenniaTags.Remove(tag); MarkDirty(); }, () => { SelectedConnection.EvenniaTags.Add(tag); MarkDirty(); }); }
    public void RemoveConnectionTag(TagModel t) { if (SelectedConnection == null) return; var idx = SelectedConnection.EvenniaTags.IndexOf(t); if (idx < 0) return; var clone = new TagModel { Key = t.Key, Category = t.Category, Data = t.Data }; SelectedConnection.EvenniaTags.RemoveAt(idx); MarkDirty(); PushUndo("Remove Exit Tag", () => { SelectedConnection.EvenniaTags.Insert(Math.Min(idx, SelectedConnection.EvenniaTags.Count), clone); MarkDirty(); }, () => { SelectedConnection.EvenniaTags.Remove(clone); MarkDirty(); }); }
    public void AddConnectionAttribute() { if (SelectedConnection == null) return; var attr = new AttributeModel(); var idx = SelectedConnection.Attributes.Count; SelectedConnection.Attributes.Add(attr); MarkDirty(); PushUndo("Add Exit Attribute", () => { SelectedConnection.Attributes.Remove(attr); MarkDirty(); }, () => { SelectedConnection.Attributes.Add(attr); MarkDirty(); }); }
    public void RemoveConnectionAttribute(AttributeModel a) { if (SelectedConnection == null) return; var idx = SelectedConnection.Attributes.IndexOf(a); if (idx < 0) return; var clone = new AttributeModel { Key = a.Key, Value = a.Value, Category = a.Category, LockString = a.LockString }; SelectedConnection.Attributes.RemoveAt(idx); MarkDirty(); PushUndo("Remove Exit Attribute", () => { SelectedConnection.Attributes.Insert(Math.Min(idx, SelectedConnection.Attributes.Count), clone); MarkDirty(); }, () => { SelectedConnection.Attributes.Remove(clone); MarkDirty(); }); }
    public void AddConnectionPermission() { if (SelectedConnection == null) return; var perm = ""; var idx = SelectedConnection.Permissions.Count; SelectedConnection.Permissions.Add(perm); MarkDirty(); PushUndo("Add Exit Permission", () => { SelectedConnection.Permissions.RemoveAt(idx); MarkDirty(); }, () => { SelectedConnection.Permissions.Insert(Math.Min(idx, SelectedConnection.Permissions.Count), perm); MarkDirty(); }); }
    public void RemoveConnectionPermission(string p) { if (SelectedConnection == null) return; var idx = SelectedConnection.Permissions.IndexOf(p); if (idx < 0) return; SelectedConnection.Permissions.RemoveAt(idx); MarkDirty(); PushUndo("Remove Exit Permission", () => { SelectedConnection.Permissions.Insert(Math.Min(idx, SelectedConnection.Permissions.Count), p); MarkDirty(); }, () => { SelectedConnection.Permissions.RemoveAt(idx); MarkDirty(); }); }

    private void SyncDoorWithReverse(ConnectionModel conn)
    {
        if (conn.Door == null || !conn.Door.SynchronizeOpposite) return;
        var rev = FindReverseConnection(conn);
        if (rev == null) return;

        rev.ExitType = conn.ExitType;
        rev.Door = new DoorModel
        {
            Name = conn.Door.Name,
            Typeclass = conn.Door.Typeclass,
            StartsOpen = conn.Door.StartsOpen,
            StartsClosed = conn.Door.StartsClosed,
            StartsLocked = conn.Door.StartsLocked,
            Lockable = conn.Door.Lockable,
            KeyId = conn.Door.KeyId,
            SynchronizeOpposite = conn.Door.SynchronizeOpposite,
            TraverseLockString = conn.Door.TraverseLockString,
            LockedFailureMessage = conn.Door.LockedFailureMessage,
            ClosedFailureMessage = conn.Door.ClosedFailureMessage,
            Description = conn.Door.Description
        };
        // Copy collection data to reverse
        rev.Door.DoorAliases = new ObservableCollection<AliasModel>(conn.Door.DoorAliases.Select(a => new AliasModel { Key = a.Key, Category = a.Category }));
        rev.Door.DoorTags = new ObservableCollection<TagModel>(conn.Door.DoorTags.Select(t => new TagModel { Key = t.Key, Category = t.Category, Data = t.Data }));
        rev.Door.DoorAttributes = new ObservableCollection<AttributeModel>(conn.Door.DoorAttributes.Select(a => new AttributeModel { Key = a.Key, Value = a.Value, Category = a.Category, LockString = a.LockString }));
        rev.Door.DoorPermissions = new ObservableCollection<string>(conn.Door.DoorPermissions);
    }

    /// <summary>
    /// Finds the reverse connection by structural pair matching:
    /// source/destination are swapped AND direction matches ReverseDirection.
    /// For doors, also validates SharedDoorId when non-empty.
    /// </summary>
    public ConnectionModel? FindReverseConnection(ConnectionModel conn)
    {
        if (string.IsNullOrEmpty(conn.SourceRoomId) || string.IsNullOrEmpty(conn.DestinationRoomId))
            return null;

        return Connections.FirstOrDefault(c =>
            c.Id != conn.Id &&
            c.SourceRoomId == conn.DestinationRoomId &&
            c.DestinationRoomId == conn.SourceRoomId &&
            c.Direction == conn.ReverseDirection &&
            (string.IsNullOrEmpty(conn.SharedDoorId) || c.SharedDoorId == conn.SharedDoorId));
    }

    /// <summary>
    /// Finds the reverse connection for undo cleanup using doorId.
    /// Only used during undo of door connections where doorId is non-empty.
    /// </summary>
    private ConnectionModel? FindReverseByDoorId(string doorId, string excludeId)
    {
        if (string.IsNullOrEmpty(doorId)) return null;
        return Connections.FirstOrDefault(c => c.SharedDoorId == doorId && c.Id != excludeId);
    }
    private void FireRoomPropsChanged()
    {
        OnPropertyChanged(nameof(IsRoomSelected)); OnPropertyChanged(nameof(IsConnectionSelected));
        OnPropertyChanged(nameof(SelectedRoomTitle)); OnPropertyChanged(nameof(SelectedRoomType));
        OnPropertyChanged(nameof(SelectedRoomDescription)); OnPropertyChanged(nameof(SelectedRoomTags));
        OnPropertyChanged(nameof(SelectedRoomNotes)); OnPropertyChanged(nameof(SelectedRoomX));
        OnPropertyChanged(nameof(SelectedRoomY)); OnPropertyChanged(nameof(SelectedRoomZ));
        OnPropertyChanged(nameof(RoomExits));
        // Evennia-native
        OnPropertyChanged(nameof(SelectedRoomTypeclassPath)); OnPropertyChanged(nameof(SelectedRoomLockString));
        OnPropertyChanged(nameof(RoomAliases)); OnPropertyChanged(nameof(RoomEvenniaTags));
        OnPropertyChanged(nameof(RoomAttributes)); OnPropertyChanged(nameof(RoomPermissions));
    }
    private void FireConnPropsChanged()
    {
        OnPropertyChanged(nameof(SelectedConnectionDirection)); OnPropertyChanged(nameof(SelectedConnectionAliases));
        OnPropertyChanged(nameof(SelectedConnectionExitType)); OnPropertyChanged(nameof(SelectedConnectionIsOneWay));
        OnPropertyChanged(nameof(SelectedConnectionDoorName)); OnPropertyChanged(nameof(SelectedConnectionStartsOpen));
        OnPropertyChanged(nameof(SelectedConnectionStartsClosed)); OnPropertyChanged(nameof(SelectedConnectionStartsLocked));
        OnPropertyChanged(nameof(SelectedConnectionKeyId)); OnPropertyChanged(nameof(IsDoorSelected));
        OnPropertyChanged(nameof(ShowDoorProperties)); OnPropertyChanged(nameof(SelectedConnectionSrcId));
        OnPropertyChanged(nameof(SelectedConnectionDstId));
        // Door expanded
        OnPropertyChanged(nameof(SelectedDoorTypeclass)); OnPropertyChanged(nameof(SelectedDoorLockable));
        OnPropertyChanged(nameof(SelectedDoorSynchronizeOpposite)); OnPropertyChanged(nameof(SelectedDoorTraverseLockString));
        OnPropertyChanged(nameof(SelectedDoorLockedFailure)); OnPropertyChanged(nameof(SelectedDoorClosedFailure));
        OnPropertyChanged(nameof(SelectedDoorDescription)); OnPropertyChanged(nameof(DoorAliases));
        OnPropertyChanged(nameof(DoorTags)); OnPropertyChanged(nameof(DoorAttributes)); OnPropertyChanged(nameof(DoorPermissions));
        // Evennia-native
        OnPropertyChanged(nameof(SelectedConnectionKeyName)); OnPropertyChanged(nameof(SelectedConnectionDescription));
        OnPropertyChanged(nameof(SelectedConnectionTypeclassPath)); OnPropertyChanged(nameof(SelectedConnectionLockString));
        OnPropertyChanged(nameof(ConnectionAliases)); OnPropertyChanged(nameof(ConnectionTags));
        OnPropertyChanged(nameof(ConnectionAttributes)); OnPropertyChanged(nameof(ConnectionPermissions));
    }
    private void RefreshStatus() { OnPropertyChanged(nameof(StatusText)); }
    private void RefreshMap() { MapNeedsRefresh?.Invoke(); }

    // ---- Spawn synchronization ----

    public void SyncSelectedRoomSpawns()
    {
        SelectedRoomItemSpawns.Clear();
        SelectedRoomNpcSpawns.Clear();

        if (_project == null || SelectedRoom == null)
            return;

        foreach (var spawn in _project.Spawns)
        {
            if (spawn.RoomId != SelectedRoom.Id)
                continue;

            var wrapper = new SpawnEntryViewModel(spawn, AvailableItems, AvailableNpcs, MarkDirty);

            if (spawn.EntityType == EntityType.Item)
                SelectedRoomItemSpawns.Add(wrapper);
            else if (spawn.EntityType == EntityType.Npc)
                SelectedRoomNpcSpawns.Add(wrapper);
        }
    }

    private string GenerateSpawnId()
    {
        string candidate;
        do
        {
            candidate = _project.Id + "_spawn_" + _spawnCounter.ToString("D4");
            _spawnCounter++;
        }
        while (_project.Spawns.Any(s => s.Id == candidate));
        return candidate;
    }

    // CENTRAL EXIT CREATION - only CreateConnection creates exits
    public RoomModel? CreateRoomAt(int x, int y, int z, bool refresh = true)
    {
        if (Rooms.Any(r => r.X == x && r.Y == y && r.Z == z)) return null;
        if (!_firstRoomPrompted && Rooms.Count == 0)
        {
            _firstRoomPrompted = true;
            var dlg = new EvenniaAtlas.Dialogs.FirstRoomTitleDialog(DefaultRoomTitle);
            dlg.Owner = Application.Current.MainWindow;
            if (dlg.ShowDialog() == true) DefaultRoomTitle = dlg.RoomTitle;
        }
        _roomCounter++;
        var title = string.IsNullOrWhiteSpace(DefaultRoomTitle) ? "Room" : DefaultRoomTitle;
        var room = new RoomModel { Id = _project.Id + "_room_" + _roomCounter.ToString("D4"), X = x, Y = y, Z = z, Title = title };
        Rooms.Add(room); _project.Rooms = Rooms.ToList(); MarkDirty();
        PushUndo("Add " + room.Id, () => { Rooms.Remove(room); _project.Rooms = Rooms.ToList(); RefreshMap(); }, () => { Rooms.Add(room); _project.Rooms = Rooms.ToList(); RefreshMap(); });
        SelectedRoom = room; RefreshStatus();
        if (refresh) RefreshMap();
        return room;
    }

    /// <summary>
    /// Moves a room to new grid coordinates.
    /// Returns true if the move succeeded, false if rejected (occupied / no-op).
    /// </summary>
    public bool MoveRoom(RoomModel room, int newX, int newY, int newZ)
    {
        // Occupancy check: target cell must be empty (or this room itself)
        if (Rooms.Any(r => r.Id != room.Id && r.X == newX && r.Y == newY && r.Z == newZ))
            return false;

        int oldX = room.X;
        int oldY = room.Y;
        int oldZ = room.Z;

        // No-op: same coordinates
        if (oldX == newX && oldY == newY && oldZ == newZ)
            return false;

        room.X = newX;
        room.Y = newY;
        room.Z = newZ;

        PushUndo($"Move {room.Id} ({oldX},{oldY},{oldZ}) → ({newX},{newY},{newZ})",
            () => { room.X = oldX; room.Y = oldY; room.Z = oldZ; NotifyCoordChanged(); RefreshMap(); if (oldZ != CurrentZ) CurrentZ = oldZ; },
            () => { room.X = newX; room.Y = newY; room.Z = newZ; NotifyCoordChanged(); RefreshMap(); if (newZ != CurrentZ) CurrentZ = newZ; });

        MarkDirty();
        NotifyCoordChanged();
        RefreshStatus();

        // Handle Z change: switch floor so room remains visible
        if (newZ != CurrentZ)
            CurrentZ = newZ;

        // SelectedRoom remains unchanged (stable ID)
        return true;
    }

    private void NotifyCoordChanged()
    {
        OnPropertyChanged(nameof(SelectedRoomX));
        OnPropertyChanged(nameof(SelectedRoomY));
        OnPropertyChanged(nameof(SelectedRoomZ));
    }

    public void NavigateOrBuild(Direction dir)
    {
        var sel = SelectedRoom; if (sel == null) return;
        var (nx, ny, nz) = DirectionHelper.GetNeighborCoordinate(sel, dir);
        var existing = Rooms.FirstOrDefault(r => r.X == nx && r.Y == ny && r.Z == nz);
        if (existing != null)
        {
            if (!Connections.Any(c => c.SourceRoomId == sel.Id && c.DestinationRoomId == existing.Id))
                CreateConnection(sel, existing, dir, ExitType.Normal, false);
            SelectedRoom = existing;
            if (existing.Z != CurrentZ) CurrentZ = existing.Z;
            RefreshMap();
        }
        else if (_buildMode)
        {
            var r = CreateRoomAt(nx, ny, nz, false);
            if (r != null)
            { CreateConnection(sel, r, dir, ExitType.Normal, false); if (r.Z != CurrentZ) CurrentZ = r.Z; RefreshMap(); }
        }
    }

    // CENTRAL EXIT CREATION - this is the ONLY place exits are created
    public void CreateConnection(RoomModel src, RoomModel dst, Direction direction, ExitType exitType = ExitType.Normal, bool refresh = true)
    {
        if (Connections.Any(c => c.SourceRoomId == src.Id && c.DestinationRoomId == dst.Id && c.Direction == direction)) return;
        _connectionCounter++;
        var doorId = exitType == ExitType.Door ? "door_" + _connectionCounter.ToString("D4") : "";
        var conn = new ConnectionModel
        {
            Id = "connection_" + _connectionCounter.ToString("D4"),
            SourceRoomId = src.Id, DestinationRoomId = dst.Id,
            Direction = direction, ReverseDirection = DirectionHelper.GetReverseDirection(direction),
            ExitType = exitType, SharedDoorId = doorId,
            Door = exitType == ExitType.Door ? new DoorModel { StartsClosed = true } : null
        };
        Connections.Add(conn); _project.Connections = Connections.ToList(); MarkDirty();

        // Auto-reverse creation - only when AutoReverse is ON and exit is not one-way
        ConnectionModel? reverseModel = null;
        if (_autoReverse && !conn.IsOneWay)
        {
            _connectionCounter++;
            reverseModel = new ConnectionModel
            {
                Id = "connection_" + _connectionCounter.ToString("D4"),
                SourceRoomId = dst.Id, DestinationRoomId = src.Id,
                Direction = DirectionHelper.GetReverseDirection(direction), ReverseDirection = direction,
                ExitType = exitType, SharedDoorId = doorId,
                Door = exitType == ExitType.Door ? new DoorModel { Name = conn.Door?.Name ?? "", StartsOpen = conn.Door?.StartsOpen ?? false, StartsClosed = conn.Door?.StartsClosed ?? true, StartsLocked = conn.Door?.StartsLocked ?? false, KeyId = conn.Door?.KeyId ?? "" } : null
            };
            Connections.Add(reverseModel);
            _project.Connections = Connections.ToList();
        }

        PushUndo("Connect " + conn.Id, () =>
        {
            Connections.Remove(conn);
            if (reverseModel != null) Connections.Remove(reverseModel);
            _project.Connections = Connections.ToList(); RefreshMap();
        }, () =>
        {
            Connections.Add(conn);
            if (reverseModel != null) Connections.Add(reverseModel);
            _project.Connections = Connections.ToList(); RefreshMap();
        });
        MarkDirty();
        if (refresh) RefreshMap();
    }

    public void DeleteSelected() { if (SelectedRoom != null) DeleteRoom(SelectedRoom); else if (SelectedConnection != null) DeleteConnection(SelectedConnection); }

    public void DeleteRoom(RoomModel room)
    {
        // Hard-block deletion if spawns reference this room
        var referencingSpawns = _project.Spawns.Where(s => s.RoomId == room.Id).ToList();
        if (referencingSpawns.Count > 0)
        {
            MessageBox.Show(
                $"Cannot delete room '{room.Id}' because {referencingSpawns.Count} spawn(s) reference it. Remove the spawns first.",
                "Cannot Delete Room",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var questRefs = _project.Quests
            .Where(q => q.Objectives.Any(o => o.ObjectiveType == QuestObjectiveType.VisitRoom && o.TargetId == room.Id))
            .ToList();
        if (questRefs.Count > 0)
        {
            var questNames = string.Join(", ", questRefs.Select(q => q.Key));
            MessageBox.Show(
                $"Cannot delete room '{room.Id}' because it is used by quest(s): {questNames}. Remove the quest references first.",
                "Cannot Delete Room",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }
        // Hard-block deletion if any NPC patrol waypoint references this room
        var patrolNpcs = _project.Npcs
            .Where(npc => npc.Patrol.Waypoints.Any(w => w.RoomId == room.Id))
            .ToList();
        if (patrolNpcs.Count > 0)
        {
            var npcNames = string.Join(", ", patrolNpcs.Select(n => n.Key));
            MessageBox.Show(
                "Cannot delete room '" + room.Id + "' because it is used by NPC patrol(s): " + npcNames + ". Remove the patrol waypoints first.",
                "Cannot Delete Room",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }
        var cons = Connections.Where(c => c.SourceRoomId == room.Id || c.DestinationRoomId == room.Id).ToList();
        foreach (var c in cons) Connections.Remove(c);
        Rooms.Remove(room); _project.Rooms = Rooms.ToList(); _project.Connections = Connections.ToList();
        PushUndo("Delete " + room.Id, () => { Rooms.Add(room); foreach (var c in cons) Connections.Add(c); _project.Rooms = Rooms.ToList(); _project.Connections = Connections.ToList(); RefreshMap(); }, () => { foreach (var c in cons) Connections.Remove(c); Rooms.Remove(room); _project.Rooms = Rooms.ToList(); _project.Connections = Connections.ToList(); RefreshMap(); });
        SelectedRoom = null; MarkDirty(); RefreshMap();
    }

    private void DeleteConnection(ConnectionModel conn)
    {
        var rev = Connections.FirstOrDefault(c => c.SharedDoorId == conn.SharedDoorId && c.Id != conn.Id);
        Connections.Remove(conn); if (rev != null) Connections.Remove(rev); _project.Connections = Connections.ToList();
        PushUndo("Delete " + conn.Id, () => { Connections.Add(conn); if (rev != null) Connections.Add(rev); _project.Connections = Connections.ToList(); RefreshMap(); }, () => { Connections.Remove(conn); if (rev != null) Connections.Remove(rev); _project.Connections = Connections.ToList(); RefreshMap(); });
        SelectedConnection = null; MarkDirty(); RefreshMap();
    }

    // ---- Spawn Add / Remove (structural) ----

    public void AddItemSpawn()
    {
        if (_project == null || SelectedRoom == null) return;

        var spawnModel = new SpawnModel
        {
            Id = GenerateSpawnId(),
            RoomId = SelectedRoom.Id,
            EntityId = string.Empty,
            EntityType = EntityType.Item,
            Quantity = 1,
            RespawnSeconds = 0,
            Enabled = true
        };

        _project.Spawns.Add(spawnModel);
        var wrapper = new SpawnEntryViewModel(spawnModel, AvailableItems, AvailableNpcs, MarkDirty);
        SelectedRoomItemSpawns.Add(wrapper);
        SelectedItemSpawn = wrapper;

        MarkDirty();
        PushUndo("Add Item Spawn " + spawnModel.Id,
            () => { _project.Spawns.Remove(spawnModel); SyncSelectedRoomSpawns(); },
            () => { _project.Spawns.Add(spawnModel); SyncSelectedRoomSpawns(); });
    }

    public void AddNpcSpawn()
    {
        if (_project == null || SelectedRoom == null) return;

        var spawnModel = new SpawnModel
        {
            Id = GenerateSpawnId(),
            RoomId = SelectedRoom.Id,
            EntityId = string.Empty,
            EntityType = EntityType.Npc,
            Quantity = 1,
            RespawnSeconds = 0,
            Enabled = true
        };

        _project.Spawns.Add(spawnModel);
        var wrapper = new SpawnEntryViewModel(spawnModel, AvailableItems, AvailableNpcs, MarkDirty);
        SelectedRoomNpcSpawns.Add(wrapper);
        SelectedNpcSpawn = wrapper;

        MarkDirty();
        PushUndo("Add NPC Spawn " + spawnModel.Id,
            () => { _project.Spawns.Remove(spawnModel); SyncSelectedRoomSpawns(); },
            () => { _project.Spawns.Add(spawnModel); SyncSelectedRoomSpawns(); });
    }

    public void RemoveItemSpawn()
    {
        if (_project == null || SelectedItemSpawn == null) return;

        var spawnModel = SelectedItemSpawn.Model;
        var index = _project.Spawns.IndexOf(spawnModel);

        _project.Spawns.Remove(spawnModel);
        SelectedRoomItemSpawns.Remove(SelectedItemSpawn);
        SelectedItemSpawn = null;

        MarkDirty();
        PushUndo("Remove Item Spawn " + spawnModel.Id,
            () =>
            {
                if (index >= 0 && index <= _project.Spawns.Count)
                    _project.Spawns.Insert(index, spawnModel);
                else
                    _project.Spawns.Add(spawnModel);
                SyncSelectedRoomSpawns();
            },
            () =>
            {
                _project.Spawns.Remove(spawnModel);
                SyncSelectedRoomSpawns();
            });
    }

    public void RemoveNpcSpawn()
    {
        if (_project == null || SelectedNpcSpawn == null) return;

        var spawnModel = SelectedNpcSpawn.Model;
        var index = _project.Spawns.IndexOf(spawnModel);

        _project.Spawns.Remove(spawnModel);
        SelectedRoomNpcSpawns.Remove(SelectedNpcSpawn);
        SelectedNpcSpawn = null;

        MarkDirty();
        PushUndo("Remove NPC Spawn " + spawnModel.Id,
            () =>
            {
                if (index >= 0 && index <= _project.Spawns.Count)
                    _project.Spawns.Insert(index, spawnModel);
                else
                    _project.Spawns.Add(spawnModel);
                SyncSelectedRoomSpawns();
            },
            () =>
            {
                _project.Spawns.Remove(spawnModel);
                SyncSelectedRoomSpawns();
            });
    }

    // ---- Connector context-menu deletion methods ----

    /// <summary>Deletes a single connection (no reverse cleanup). Used by connector context menu.</summary>
    public void DeleteSingleConnection(ConnectionModel conn)
    {
        var snapshot = CloneConnection(conn);
        Connections.Remove(conn);
        _project.Connections = Connections.ToList();
        PushUndo("Delete " + conn.Id,
            () => { Connections.Add(snapshot); _project.Connections = Connections.ToList(); RefreshMap(); },
            () => { Connections.Remove(conn); _project.Connections = Connections.ToList(); RefreshMap(); });
        SelectedConnection = null; MarkDirty(); RefreshMap();
    }

    /// <summary>Deletes a connection and its structural reverse as one atomic operation. Used by connector context menu.</summary>
    public void DeleteConnectionPair(ConnectionModel conn)
    {
        var rev = FindReverseConnection(conn);
        var snap1 = CloneConnection(conn);
        var snap2 = rev != null ? CloneConnection(rev) : null;
        Connections.Remove(conn);
        if (rev != null) Connections.Remove(rev);
        _project.Connections = Connections.ToList();
        var desc = rev != null ? $"Delete paired {conn.Id} + {rev.Id}" : "Delete " + conn.Id;
        PushUndo(desc,
            () => { Connections.Add(snap1); if (snap2 != null) Connections.Add(snap2); _project.Connections = Connections.ToList(); RefreshMap(); },
            () => { Connections.Remove(conn); if (rev != null) Connections.Remove(rev); _project.Connections = Connections.ToList(); RefreshMap(); });
        SelectedConnection = null; MarkDirty(); RefreshMap();
    }

    private static ConnectionModel CloneConnection(ConnectionModel c) => new()
    {
        Id = c.Id,
        SourceRoomId = c.SourceRoomId,
        DestinationRoomId = c.DestinationRoomId,
        Direction = c.Direction,
        ReverseDirection = c.ReverseDirection,
        IsOneWay = c.IsOneWay,
        ExitType = c.ExitType,
        Aliases = c.Aliases,
        SharedDoorId = c.SharedDoorId,
        Door = c.Door != null ? CloneDoor(c.Door) : null,
        AutoCreateReverse = c.AutoCreateReverse,
        KeyName = c.KeyName,
        Description = c.Description,
        TypeclassPath = c.TypeclassPath,
        AliasesList = new ObservableCollection<AliasModel>(c.AliasesList.Select(a => new AliasModel { Key = a.Key, Category = a.Category })),
        EvenniaTags = new ObservableCollection<TagModel>(c.EvenniaTags.Select(t => new TagModel { Key = t.Key, Category = t.Category, Data = t.Data })),
        Attributes = new ObservableCollection<AttributeModel>(c.Attributes.Select(a => new AttributeModel { Key = a.Key, Value = a.Value, Category = a.Category, LockString = a.LockString })),
        LockString = c.LockString,
        Permissions = new ObservableCollection<string>(c.Permissions),
    };

    public void Undo() { if (_undoStack.Count == 0) return; var a = _undoStack.Pop(); a.Undo(); _redoStack.Push(a); MarkDirty(); SelectedRoom = null; SelectedConnection = null; }
    public void Redo() { if (_redoStack.Count == 0) return; var a = _redoStack.Pop(); a.Rdo(); _undoStack.Push(a); MarkDirty(); SelectedRoom = null; SelectedConnection = null; }
    private void PushUndo(string desc, Action undo, Action redo) { _undoStack.Push(new UndoAction(desc, undo, redo)); _redoStack.Clear(); }

// ---- Property-edit undo helpers (coalesce per edit, not per keystroke) ----
    public void BeginPropertyEdit(string key, object? currentValue)
    {
        _beforeValues[key] = currentValue;
    }

    public void EndPropertyEdit(string key, string description, object? newValue, Action<object?> restore, Action<object?> redo)
    {
        if (!_beforeValues.TryGetValue(key, out var oldValue)) return;
        _beforeValues.Remove(key);
        if (oldValue is string os && newValue is string ns && os == ns) return;
        if (oldValue is bool ob && newValue is bool nb && ob == nb) return;
        if (oldValue == null && newValue == null) return;
        if (oldValue != null && oldValue.Equals(newValue)) return;
        var old = oldValue;
        PushUndo(description, () => restore(old), () => redo(newValue));
    }

    // Deep-copy helpers for collection snapshots
    internal static List<AliasModel> CloneAliases(IEnumerable<AliasModel> src)
        => src.Select(a => new AliasModel { Key = a.Key, Category = a.Category }).ToList();
    internal static List<TagModel> CloneTags(IEnumerable<TagModel> src)
        => src.Select(t => new TagModel { Key = t.Key, Category = t.Category, Data = t.Data }).ToList();
    internal static List<AttributeModel> CloneAttrs(IEnumerable<AttributeModel> src)
        => src.Select(a => new AttributeModel { Key = a.Key, Value = a.Value, Category = a.Category, LockString = a.LockString }).ToList();
    internal static List<string> ClonePerms(IEnumerable<string> src) => src.ToList();
    internal static DoorModel CloneDoor(DoorModel d) => new()
    {
        Name = d.Name, Typeclass = d.Typeclass,
        StartsOpen = d.StartsOpen, StartsClosed = d.StartsClosed, StartsLocked = d.StartsLocked,
        Lockable = d.Lockable, KeyId = d.KeyId, SynchronizeOpposite = d.SynchronizeOpposite,
        TraverseLockString = d.TraverseLockString, LockedFailureMessage = d.LockedFailureMessage,
        ClosedFailureMessage = d.ClosedFailureMessage, Description = d.Description
    };
    // Captures current + reverse door state for paired atomic undo
    private (DoorModel? cur, DoorModel? rev) SnapshotPairedDoor(ConnectionModel conn)
    {
        DoorModel? curClone = conn.Door != null ? CloneDoor(conn.Door) : null;
        DoorModel? revClone = null;
        if (conn.Door?.SynchronizeOpposite == true)
        {
            var revConn = FindReverseConnection(conn);
            if (revConn?.Door != null) revClone = CloneDoor(revConn.Door);
        }
        return (curClone, revClone);
    }
    public void StartNewProject() { _project = new MapProject { Id = "untitled", Name = "Untitled", Version = 2, DefaultRoomTitle = "" }; _currentFilePath = null; _isDirty = false; _roomCounter = 0; _connectionCounter = 0; _spawnCounter = 1; _firstRoomPrompted = false; Rooms.Clear(); Connections.Clear(); SelectedRoomItemSpawns.Clear(); SelectedRoomNpcSpawns.Clear(); _undoStack.Clear(); _redoStack.Clear(); SelectedRoom = null; SelectedConnection = null; CurrentZ = 0; _zoomScale = ZoomDefault; OnPropertyChanged(nameof(ZoomPercentage)); NotifyProjectChanged(); RefreshMap(); }

    public void NewProject()
    {
        if (!ConfirmDiscardChanges()) return;
        var dlg = new EvenniaAtlas.Dialogs.NewProjectDialog();
        if (dlg.ShowDialog() == true)
        {
            _project = new MapProject { Id = dlg.AreaId, Name = dlg.AreaName, Version = 2, DefaultRoomTitle = "" };
            _currentFilePath = null; _isDirty = false; _roomCounter = 0; _connectionCounter = 0; _spawnCounter = 1; _firstRoomPrompted = false;
            Rooms.Clear(); Connections.Clear(); SelectedRoomItemSpawns.Clear(); SelectedRoomNpcSpawns.Clear(); _undoStack.Clear(); _redoStack.Clear();
            SelectedRoom = null; SelectedConnection = null; CurrentZ = 0; _zoomScale = ZoomDefault; OnPropertyChanged(nameof(ZoomPercentage));
            NotifyProjectChanged(); RefreshMap();
        }
    }

    public void OpenProject()
    {
        if (!ConfirmDiscardChanges()) return;
        var dlg = new OpenFileDialog { Filter = "Evennia Map Files (*.evenniamap)|*.evenniamap", Title = "Open Map Project" };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                _project = _fileService.Load(dlg.FileName); _currentFilePath = dlg.FileName; _isDirty = false; _firstRoomPrompted = true;
                Rooms.Clear(); Connections.Clear(); SelectedRoomItemSpawns.Clear(); SelectedRoomNpcSpawns.Clear();
                foreach (var r in _project.Rooms) Rooms.Add(r);
                foreach (var c in _project.Connections) Connections.Add(c);
                _roomCounter = Rooms.Count > 0 ? Rooms.Max(r => int.TryParse(r.Id.Split(Convert.ToChar(95)).Last(), out var n) ? n : 0) : 0;
                _connectionCounter = Connections.Count > 0 ? Connections.Max(c => int.TryParse(c.Id.Split(Convert.ToChar(95)).Last(), out var n) ? n : 0) : 0;
                _spawnCounter = _project.Spawns.Count > 0 ? _project.Spawns.Max(s => int.TryParse(s.Id.Split(Convert.ToChar(95)).Last(), out var n) ? n : 0) : 0;
                _undoStack.Clear(); _redoStack.Clear(); SelectedRoom = null; SelectedConnection = null; CurrentZ = 0; _zoomScale = ZoomDefault; OnPropertyChanged(nameof(ZoomPercentage));
                NotifyProjectChanged(); RefreshMap();
            }
            catch (Exception ex) { MessageBox.Show("Failed to open: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }

    public bool SaveProject() { if (string.IsNullOrEmpty(_currentFilePath)) return SaveProjectAs(); try { SyncToProject(); _fileService.Save(_currentFilePath, _project); _isDirty = false; NotifyProjectChanged(); return true; } catch (Exception ex) { MessageBox.Show("Failed to save: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); return false; } }

    public bool SaveProjectAs() { var dlg = new SaveFileDialog { Filter = "Evennia Map Files (*.evenniamap)|*.evenniamap", Title = "Save Map Project", DefaultExt = ".evenniamap" }; if (dlg.ShowDialog() == true) { _currentFilePath = dlg.FileName; return SaveProject(); } return false; }

    public void ExportEvennia()
    {
        SyncToProject();

        var validator = new MapValidationService();
        var issues = validator.Validate(_project);
        if (issues.Exists(i => i.Severity == ValidationSeverity.Error))
        {
            var dialog = new Dialogs.ValidationResultsDialog(issues) { Owner = Application.Current.MainWindow };
            dialog.ShowDialog();
            return;
        }

        var dlg = new SaveFileDialog { Filter = "Evennia JSON (*.evennia.json)|*.evennia.json", Title = "Export Evennia JSON", DefaultExt = ".evennia.json", FileName = _project.Id + ".evennia.json" };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                _exportService.Export(dlg.FileName, _project);
                MessageBox.Show("Exported to: " + dlg.FileName, "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public void ValidateProject()
    {
        SyncToProject();
        var validator = new MapValidationService();
        var issues = validator.Validate(_project);
        var dialog = new Dialogs.ValidationResultsDialog(issues) { Owner = Application.Current.MainWindow };
        dialog.ShowDialog();
    }

    public bool ConfirmDiscardChanges() { if (!_isDirty) return true; return MessageBox.Show("You have unsaved changes. Discard them?", "Unsaved Changes", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes; }

    public bool ConfirmClose() { if (!_isDirty) return true; var r = MessageBox.Show("You have unsaved changes. Save before closing?", "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning); return r switch { MessageBoxResult.Yes => SaveProject(), MessageBoxResult.No => true, _ => false }; }

    public void NotifyProjectChanged() { OnPropertyChanged(nameof(ProjectName)); OnPropertyChanged(nameof(ProjectId)); OnPropertyChanged(nameof(TitleBarText)); OnPropertyChanged(nameof(IsDirty)); OnPropertyChanged(nameof(StatusText)); }
    private void MarkDirty() { _isDirty = true; NotifyProjectChanged(); }
    public void MarkDirtyPublic() { MarkDirty(); }
    private void SyncToProject() { _project.Rooms = Rooms.ToList(); _project.Connections = Connections.ToList(); }
}

public class UndoAction
{
    public string Description { get; }
    private readonly Action _undo;
    private readonly Action _redo;
    public UndoAction(string d, Action u, Action r) { Description = d; _undo = u; _redo = r; }
    public void Undo() => _undo();
    public void Rdo() => _redo();
}


