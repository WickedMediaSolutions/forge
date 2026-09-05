
using System.Collections.ObjectModel;
using System.Windows;
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
    private readonly Stack<UndoAction> _undoStack = new();
    private readonly Stack<UndoAction> _redoStack = new();
    private bool _firstRoomPrompted;

    public MainViewModel()
    {
        Rooms = new ObservableCollection<RoomModel>();
        Connections = new ObservableCollection<ConnectionModel>();
        StartNewProject();
    }

    public ObservableCollection<RoomModel> Rooms { get; }
    public ObservableCollection<ConnectionModel> Connections { get; }
    public event Action? MapNeedsRefresh;

    private int _currentZ;
    public int CurrentZ { get => _currentZ; set { SetField(ref _currentZ, value); OnPropertyChanged(nameof(FloorLabel)); RefreshMap(); } }
    public string FloorLabel => "Floor " + _currentZ;

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
    public string TitleBarText { get { var n = string.IsNullOrEmpty(_project.Name) ? "Untitled" : _project.Name; return "Evennia Atlas - " + n + (_isDirty ? " *" : ""); } }
    public string StatusText { get { var sel = SelectedRoom; if (sel != null) return "Room: " + sel.Id + "  (" + sel.X + ", " + sel.Y + ", " + sel.Z + ")"; return "Rooms: " + Rooms.Count + "  Exits: " + Connections.Count + "  " + FloorLabel; } }

    private RoomModel? _selectedRoom;
    public RoomModel? SelectedRoom { get => _selectedRoom; set { if (!SetField(ref _selectedRoom, value)) return; SelectedConnection = null; FireRoomPropsChanged(); OnPropertyChanged(nameof(StatusText)); } }

    private ConnectionModel? _selectedConnection;
    public ConnectionModel? SelectedConnection { get => _selectedConnection; set { if (!SetField(ref _selectedConnection, value)) return; if (value != null && _selectedRoom != null) { _selectedRoom = null; FireRoomPropsChanged(); } OnPropertyChanged(nameof(IsRoomSelected)); OnPropertyChanged(nameof(IsConnectionSelected)); OnPropertyChanged(nameof(StatusText)); FireConnPropsChanged(); } }

    public bool IsRoomSelected => _selectedRoom != null;
    public bool IsConnectionSelected => _selectedConnection != null;
    public bool IsDoorSelected => SelectedConnection?.ExitType == ExitType.Door;
    public bool ShowDoorProperties => SelectedConnection?.ExitType == ExitType.Door;

    public string? SelectedRoomTitle { get => _selectedRoom?.Title; set { if (_selectedRoom != null) { _selectedRoom.Title = value ?? ""; MarkDirty(); OnPropertyChanged(); RefreshStatus(); } } }
    public string? SelectedRoomType { get => _selectedRoom?.RoomType; set { if (_selectedRoom != null) { _selectedRoom.RoomType = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }
    public string? SelectedRoomDescription { get => _selectedRoom?.Description; set { if (_selectedRoom != null) { _selectedRoom.Description = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }
    public string? SelectedRoomTags { get => _selectedRoom?.TagsString; set { if (_selectedRoom != null) { _selectedRoom.TagsString = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }
    public string? SelectedRoomNotes { get => _selectedRoom?.Notes; set { if (_selectedRoom != null) { _selectedRoom.Notes = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }
    public int SelectedRoomX => _selectedRoom?.X ?? 0;
    public int SelectedRoomY => _selectedRoom?.Y ?? 0;
    public int SelectedRoomZ => _selectedRoom?.Z ?? 0;

    public ObservableCollection<ConnectionModel> RoomExits
    { get { var list = new ObservableCollection<ConnectionModel>(); if (_selectedRoom != null) foreach (var c in Connections.Where(c => c.SourceRoomId == _selectedRoom.Id || c.DestinationRoomId == _selectedRoom.Id)) list.Add(c); return list; } }

    public Direction SelectedConnectionDirection { get => SelectedConnection?.Direction ?? Direction.North; set { if (SelectedConnection != null) { SelectedConnection.Direction = value; SelectedConnection.ReverseDirection = DirectionHelper.GetReverseDirection(value); MarkDirty(); OnPropertyChanged(); } } }
    public string? SelectedConnectionAliases { get => SelectedConnection?.Aliases; set { if (SelectedConnection != null) { SelectedConnection.Aliases = value ?? ""; MarkDirty(); OnPropertyChanged(); } } }
    public ExitType SelectedConnectionExitType { get => SelectedConnection?.ExitType ?? ExitType.Normal; set { if (SelectedConnection != null) { SelectedConnection.ExitType = value; if (value == ExitType.Door && SelectedConnection.Door == null) SelectedConnection.Door = new DoorModel(); MarkDirty(); OnPropertyChanged(nameof(IsDoorSelected)); OnPropertyChanged(nameof(ShowDoorProperties)); SyncDoorWithReverse(SelectedConnection); } } }
    public bool SelectedConnectionIsOneWay { get => SelectedConnection?.IsOneWay ?? false; set { if (SelectedConnection != null) { SelectedConnection.IsOneWay = value; MarkDirty(); OnPropertyChanged(); } } }
    public string? SelectedConnectionDoorName { get => SelectedConnection?.Door?.Name; set { if (SelectedConnection?.Door != null) { SelectedConnection.Door.Name = value ?? ""; MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); } } }
    public bool SelectedConnectionStartsOpen { get => SelectedConnection?.Door?.StartsOpen ?? false; set { if (SelectedConnection?.Door != null) { SelectedConnection.Door.StartsOpen = value; if (value) SelectedConnection.Door.StartsClosed = false; MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); } } }
    public bool SelectedConnectionStartsClosed { get => SelectedConnection?.Door?.StartsClosed ?? true; set { if (SelectedConnection?.Door != null) { SelectedConnection.Door.StartsClosed = value; if (value) SelectedConnection.Door.StartsOpen = false; MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); } } }
    public bool SelectedConnectionStartsLocked { get => SelectedConnection?.Door?.StartsLocked ?? false; set { if (SelectedConnection?.Door != null) { SelectedConnection.Door.StartsLocked = value; if (value) { SelectedConnection.Door.StartsClosed = true; SelectedConnection.Door.StartsOpen = false; } MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); } } }
    public string? SelectedConnectionKeyId { get => SelectedConnection?.Door?.KeyId; set { if (SelectedConnection?.Door != null) { SelectedConnection.Door.KeyId = value ?? ""; MarkDirty(); OnPropertyChanged(); SyncDoorWithReverse(SelectedConnection); } } }
    public string SelectedConnectionSrcId => SelectedConnection?.SourceRoomId ?? "";
    public string SelectedConnectionDstId => SelectedConnection?.DestinationRoomId ?? "";

    private void SyncDoorWithReverse(ConnectionModel conn)
    {
        if (string.IsNullOrEmpty(conn.SharedDoorId)) return;

        var rev = Connections.FirstOrDefault(c =>
            c.SharedDoorId == conn.SharedDoorId &&
            c.Id != conn.Id);

        if (rev != null && conn.Door != null)
        {
            rev.ExitType = conn.ExitType;
            rev.Door = new DoorModel
            {
                Name = conn.Door.Name,
                StartsOpen = conn.Door.StartsOpen,
                StartsClosed = conn.Door.StartsClosed,
                StartsLocked = conn.Door.StartsLocked,
                KeyId = conn.Door.KeyId
            };
        }
    }
    private void FireRoomPropsChanged()
    {
        OnPropertyChanged(nameof(IsRoomSelected)); OnPropertyChanged(nameof(IsConnectionSelected));
        OnPropertyChanged(nameof(SelectedRoomTitle)); OnPropertyChanged(nameof(SelectedRoomType));
        OnPropertyChanged(nameof(SelectedRoomDescription)); OnPropertyChanged(nameof(SelectedRoomTags));
        OnPropertyChanged(nameof(SelectedRoomNotes)); OnPropertyChanged(nameof(SelectedRoomX));
        OnPropertyChanged(nameof(SelectedRoomY)); OnPropertyChanged(nameof(SelectedRoomZ));
        OnPropertyChanged(nameof(RoomExits));
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
    }
    private void RefreshStatus() { OnPropertyChanged(nameof(StatusText)); }
    private void RefreshMap() { MapNeedsRefresh?.Invoke(); }

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
        PushUndo("Add " + room.Id, () => { Rooms.Remove(room); _project.Rooms = Rooms.ToList(); RefreshMap(); });
        SelectedRoom = room; RefreshStatus();
        if (refresh) RefreshMap();
        return room;
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
        if (_autoReverse && !conn.IsOneWay)
        {
            _connectionCounter++;
            Connections.Add(new ConnectionModel
            {
                Id = "connection_" + _connectionCounter.ToString("D4"),
                SourceRoomId = dst.Id, DestinationRoomId = src.Id,
                Direction = DirectionHelper.GetReverseDirection(direction), ReverseDirection = direction,
                ExitType = exitType, SharedDoorId = doorId,
                Door = exitType == ExitType.Door ? new DoorModel { Name = conn.Door?.Name ?? "", StartsOpen = conn.Door?.StartsOpen ?? false, StartsClosed = conn.Door?.StartsClosed ?? true, StartsLocked = conn.Door?.StartsLocked ?? false, KeyId = conn.Door?.KeyId ?? "" } : null
            });
            _project.Connections = Connections.ToList();
        }

        PushUndo("Connect " + conn.Id, () =>
        {
            Connections.Remove(conn);
            var rev = Connections.FirstOrDefault(c => c.SharedDoorId == doorId && c.Id != conn.Id);
            if (rev != null) Connections.Remove(rev);
            _project.Connections = Connections.ToList(); RefreshMap();
        });
        MarkDirty();
        if (refresh) RefreshMap();
    }

    public void DeleteSelected() { if (SelectedRoom != null) DeleteRoom(SelectedRoom); else if (SelectedConnection != null) DeleteConnection(SelectedConnection); }

    public void DeleteRoom(RoomModel room)
    {
        var cons = Connections.Where(c => c.SourceRoomId == room.Id || c.DestinationRoomId == room.Id).ToList();
        foreach (var c in cons) Connections.Remove(c);
        Rooms.Remove(room); _project.Rooms = Rooms.ToList(); _project.Connections = Connections.ToList();
        PushUndo("Delete " + room.Id, () => { Rooms.Add(room); foreach (var c in cons) Connections.Add(c); _project.Rooms = Rooms.ToList(); _project.Connections = Connections.ToList(); RefreshMap(); });
        SelectedRoom = null; MarkDirty(); RefreshMap();
    }

    private void DeleteConnection(ConnectionModel conn)
    {
        var rev = Connections.FirstOrDefault(c => c.SharedDoorId == conn.SharedDoorId && c.Id != conn.Id);
        Connections.Remove(conn); if (rev != null) Connections.Remove(rev); _project.Connections = Connections.ToList();
        PushUndo("Delete " + conn.Id, () => { Connections.Add(conn); if (rev != null) Connections.Add(rev); _project.Connections = Connections.ToList(); RefreshMap(); });
        SelectedConnection = null; MarkDirty(); RefreshMap();
    }

    public void Undo() { if (_undoStack.Count == 0) return; var a = _undoStack.Pop(); a.Undo(); _redoStack.Push(a); MarkDirty(); SelectedRoom = null; SelectedConnection = null; }
    public void Redo() { if (_redoStack.Count == 0) return; var a = _redoStack.Pop(); a.Undo(); _undoStack.Push(a); MarkDirty(); SelectedRoom = null; SelectedConnection = null; }
    private void PushUndo(string desc, Action undo) { _undoStack.Push(new UndoAction(desc, undo)); _redoStack.Clear(); }

    public void StartNewProject() { _project = new MapProject { Id = "untitled", Name = "Untitled", Version = 1, DefaultRoomTitle = "" }; _currentFilePath = null; _isDirty = false; _roomCounter = 0; _connectionCounter = 0; _firstRoomPrompted = false; Rooms.Clear(); Connections.Clear(); _undoStack.Clear(); _redoStack.Clear(); SelectedRoom = null; SelectedConnection = null; CurrentZ = 0; NotifyProjectChanged(); RefreshMap(); }

    public void NewProject()
    {
        if (!ConfirmDiscardChanges()) return;
        var dlg = new EvenniaAtlas.Dialogs.NewProjectDialog();
        if (dlg.ShowDialog() == true)
        {
            _project = new MapProject { Id = dlg.AreaId, Name = dlg.AreaName, Version = 1, DefaultRoomTitle = "" };
            _currentFilePath = null; _isDirty = false; _roomCounter = 0; _connectionCounter = 0; _firstRoomPrompted = false;
            Rooms.Clear(); Connections.Clear(); _undoStack.Clear(); _redoStack.Clear();
            SelectedRoom = null; SelectedConnection = null; CurrentZ = 0;
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
                Rooms.Clear(); Connections.Clear();
                foreach (var r in _project.Rooms) Rooms.Add(r);
                foreach (var c in _project.Connections) Connections.Add(c);
                _roomCounter = Rooms.Count > 0 ? Rooms.Max(r => int.TryParse(r.Id.Split(Convert.ToChar(95)).Last(), out var n) ? n : 0) : 0;
                _connectionCounter = Connections.Count > 0 ? Connections.Max(c => int.TryParse(c.Id.Split(Convert.ToChar(95)).Last(), out var n) ? n : 0) : 0;
                _undoStack.Clear(); _redoStack.Clear(); SelectedRoom = null; SelectedConnection = null; CurrentZ = 0;
                NotifyProjectChanged(); RefreshMap();
            }
            catch (Exception ex) { MessageBox.Show("Failed to open: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }

    public bool SaveProject() { if (string.IsNullOrEmpty(_currentFilePath)) return SaveProjectAs(); try { SyncToProject(); _fileService.Save(_currentFilePath, _project); _isDirty = false; NotifyProjectChanged(); return true; } catch (Exception ex) { MessageBox.Show("Failed to save: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); return false; } }

    public bool SaveProjectAs() { var dlg = new SaveFileDialog { Filter = "Evennia Map Files (*.evenniamap)|*.evenniamap", Title = "Save Map Project", DefaultExt = ".evenniamap" }; if (dlg.ShowDialog() == true) { _currentFilePath = dlg.FileName; return SaveProject(); } return false; }

    public void ExportEvennia() { SyncToProject(); var dlg = new SaveFileDialog { Filter = "Evennia JSON (*.evennia.json)|*.evennia.json", Title = "Export Evennia JSON", DefaultExt = ".evennia.json", FileName = _project.Id + ".evennia.json" }; if (dlg.ShowDialog() == true) { try { _exportService.Export(dlg.FileName, _project); MessageBox.Show("Exported to: " + dlg.FileName, "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information); } catch (Exception ex) { MessageBox.Show("Export failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); } } }

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
    public UndoAction(string d, Action u) { Description = d; _undo = u; }
    public void Undo() => _undo();
}
