using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EvenniaAtlas.Models;

namespace EvenniaAtlas.ViewModels;

public class NpcEditorViewModel : BaseViewModel
{
    private MapProject? _project;
    private int _npcCounter;

    public Action? MarkDirtyCallback { get; set; }
    private void MarkDirty() => MarkDirtyCallback?.Invoke();

    public ObservableCollection<NpcModel> Npcs { get; } = new();
    public ObservableCollection<NpcModel> FilteredNpcs { get; } = new();

    public ICommand NewNpcCommand { get; }
    public ICommand DuplicateNpcCommand { get; }
    public ICommand DeleteNpcCommand { get; }

    public ICommand AddMetadataAliasCommand { get; }
    public ICommand RemoveMetadataAliasCommand { get; }
    public ICommand AddMetadataTagCommand { get; }
    public ICommand RemoveMetadataTagCommand { get; }
    public ICommand AddMetadataAttributeCommand { get; }
    public ICommand RemoveMetadataAttributeCommand { get; }
    public ICommand AddMetadataPermissionCommand { get; }
    public ICommand RemoveMetadataPermissionCommand { get; }

    public ICommand AddAbilityCommand { get; }
    public ICommand RemoveAbilityCommand { get; }

    public ICommand AddResistanceCommand { get; }
    public ICommand RemoveResistanceCommand { get; }

    public ICommand AddEquipmentCommand { get; }
    public ICommand RemoveEquipmentCommand { get; }

    public ICommand AddInventoryCommand { get; }
    public ICommand RemoveInventoryCommand { get; }

    public ICommand AddPatrolWaypointCommand { get; }
    public ICommand RemovePatrolWaypointCommand { get; }
    public ICommand MovePatrolWaypointUpCommand { get; }
    public ICommand MovePatrolWaypointDownCommand { get; }
    public ICommand ClearLootTableCommand { get; }
    public ICommand ClearDialogueCommand { get; }

    public NpcEditorViewModel()
    {
        NewNpcCommand = new RelayCommand(_ => CreateNpc(), _ => _project != null);
        DuplicateNpcCommand = new RelayCommand(_ => DuplicateNpc(), _ => HasSelectedNpc);
        DeleteNpcCommand = new RelayCommand(_ => DeleteNpc(), _ => HasSelectedNpc);
        AddMetadataAliasCommand = new RelayCommand(_ => AddMetadataAlias(), _ => HasSelectedNpc);
        RemoveMetadataAliasCommand = new RelayCommand(_ => RemoveMetadataAlias(), _ => HasSelectedMetadataAlias);
        AddMetadataTagCommand = new RelayCommand(_ => AddMetadataTag(), _ => HasSelectedNpc);
        RemoveMetadataTagCommand = new RelayCommand(_ => RemoveMetadataTag(), _ => HasSelectedMetadataTag);
        AddMetadataAttributeCommand = new RelayCommand(_ => AddMetadataAttribute(), _ => HasSelectedNpc);
        RemoveMetadataAttributeCommand = new RelayCommand(_ => RemoveMetadataAttribute(), _ => HasSelectedMetadataAttribute);
        AddMetadataPermissionCommand = new RelayCommand(_ => AddMetadataPermission(), _ => HasSelectedNpc);
        RemoveMetadataPermissionCommand = new RelayCommand(_ => RemoveMetadataPermission(), _ => HasSelectedMetadataPermission);
        AddAbilityCommand = new RelayCommand(_ => AddAbility(), _ => HasSelectedNpc);
        RemoveAbilityCommand = new RelayCommand(_ => RemoveAbility(), _ => HasSelectedAbility);
        AddResistanceCommand = new RelayCommand(_ => AddResistance(), _ => HasSelectedNpc);
        RemoveResistanceCommand = new RelayCommand(_ => RemoveResistance(), _ => HasSelectedResistance);
        AddEquipmentCommand = new RelayCommand(_ => AddEquipment(), _ => HasSelectedNpc);
        RemoveEquipmentCommand = new RelayCommand(_ => RemoveEquipment(), _ => HasSelectedEquipment);
        AddInventoryCommand = new RelayCommand(_ => AddInventory(), _ => HasSelectedNpc);
        RemoveInventoryCommand = new RelayCommand(_ => RemoveInventory(), _ => HasSelectedInventory);

        AddPatrolWaypointCommand = new RelayCommand(_ => AddPatrolWaypoint(), _ => HasSelectedNpc);
        RemovePatrolWaypointCommand = new RelayCommand(_ => RemovePatrolWaypoint(), _ => HasSelectedPatrolWaypoint);
        MovePatrolWaypointUpCommand = new RelayCommand(_ => MovePatrolWaypointUp(),
            _ => HasSelectedNpc && _selectedPatrolWaypoint != null && PatrolWaypointEntries.IndexOf(_selectedPatrolWaypoint) > 0);
        MovePatrolWaypointDownCommand = new RelayCommand(_ => MovePatrolWaypointDown(),
            _ => HasSelectedNpc && _selectedPatrolWaypoint != null &&
                 PatrolWaypointEntries.Count > 0 && PatrolWaypointEntries.IndexOf(_selectedPatrolWaypoint) < PatrolWaypointEntries.Count - 1);
        ClearLootTableCommand = new RelayCommand(_ => ClearLootTable(), _ => HasSelectedNpc);
        ClearDialogueCommand = new RelayCommand(_ => ClearDialogue(), _ => HasSelectedNpc);
    }
    // ---- Selected NPC ----
    private NpcModel? _selectedNpc;
    public NpcModel? SelectedNpc
    {
        get => _selectedNpc;
        set
        {
            var oldNpc = _selectedNpc;
            if (SetField(ref _selectedNpc, value))
            {
                if (oldNpc != null) MarkDirty();
                OnPropertyChanged(nameof(HasSelectedNpc));
                SelectedMetadataAlias = null;
                SelectedMetadataTag = null;
                SelectedMetadataAttribute = null;
                SelectedMetadataPermission = null;
                SelectedAbility = null;
                SelectedResistance = null;
                SelectedEquipment = null;
                SelectedInventory = null;
                SyncEquipmentEntries();
                SyncInventoryEntries();
                SelectedPatrolWaypoint = null;
                SyncPatrolWaypointEntries();
                Validate();
            }
        }
    }

    public bool HasSelectedNpc => _selectedNpc != null;

    // ---- Metadata sub-selections ----
    private AliasModel? _selectedMetadataAlias;
    public AliasModel? SelectedMetadataAlias
    {
        get => _selectedMetadataAlias;
        set
        {
            if (SetField(ref _selectedMetadataAlias, value))
                OnPropertyChanged(nameof(HasSelectedMetadataAlias));
        }
    }
    public bool HasSelectedMetadataAlias => _selectedMetadataAlias != null;

    private TagModel? _selectedMetadataTag;
    public TagModel? SelectedMetadataTag
    {
        get => _selectedMetadataTag;
        set
        {
            if (SetField(ref _selectedMetadataTag, value))
                OnPropertyChanged(nameof(HasSelectedMetadataTag));
        }
    }
    public bool HasSelectedMetadataTag => _selectedMetadataTag != null;

    private AttributeModel? _selectedMetadataAttribute;
    public AttributeModel? SelectedMetadataAttribute
    {
        get => _selectedMetadataAttribute;
        set
        {
            if (SetField(ref _selectedMetadataAttribute, value))
                OnPropertyChanged(nameof(HasSelectedMetadataAttribute));
        }
    }
    public bool HasSelectedMetadataAttribute => _selectedMetadataAttribute != null;

    private string? _selectedMetadataPermission;
    public string? SelectedMetadataPermission
    {
        get => _selectedMetadataPermission;
        set
        {
            if (SetField(ref _selectedMetadataPermission, value))
                OnPropertyChanged(nameof(HasSelectedMetadataPermission));
        }
    }
    public bool HasSelectedMetadataPermission => _selectedMetadataPermission != null;

    // ---- Ability sub-selection ----
    private NpcAbilityModel? _selectedAbility;
    public NpcAbilityModel? SelectedAbility
    {
        get => _selectedAbility;
        set
        {
            if (SetField(ref _selectedAbility, value))
                OnPropertyChanged(nameof(HasSelectedAbility));
        }
    }
    public bool HasSelectedAbility => _selectedAbility != null;

    // ---- Resistance sub-selection ----
    private NpcResistanceModel? _selectedResistance;
    public NpcResistanceModel? SelectedResistance
    {
        get => _selectedResistance;
        set
        {
            if (SetField(ref _selectedResistance, value))
                OnPropertyChanged(nameof(HasSelectedResistance));
        }
    }
    public bool HasSelectedResistance => _selectedResistance != null;

    // ---- Equipment sub-selection ----
    private EquipmentEntryViewModel? _selectedEquipment;
    public EquipmentEntryViewModel? SelectedEquipment
    {
        get => _selectedEquipment;
        set
        {
            if (SetField(ref _selectedEquipment, value))
                OnPropertyChanged(nameof(HasSelectedEquipment));
        }
    }
    public bool HasSelectedEquipment => _selectedEquipment != null;

    // ---- Available Items for equipment selection ----
    public IEnumerable<ItemModel> AvailableItems => _project?.Items ?? Enumerable.Empty<ItemModel>();

    // ---- Available Rooms for patrol waypoint selection ----
    public IEnumerable<RoomModel> AvailableRooms => _project?.Rooms ?? Enumerable.Empty<RoomModel>();

    // ---- Available Loot Tables for NPC loot assignment ----
    public IEnumerable<LootTableModel> AvailableLootTables => _project?.LootTables ?? Enumerable.Empty<LootTableModel>();

    // ---- Available Dialogues for NPC dialogue assignment ----
    public IEnumerable<DialogueModel> AvailableDialogues => _project?.Dialogues ?? Enumerable.Empty<DialogueModel>();

    // ---- Available GameData registries for Classification and Resistances ----
    public IEnumerable<GameDataEntryModel> AvailableDamageTypes => _project?.DamageTypes ?? Enumerable.Empty<GameDataEntryModel>();
    public IEnumerable<GameDataEntryModel> AvailableFactions => _project?.Factions ?? Enumerable.Empty<GameDataEntryModel>();
    public IEnumerable<GameDataEntryModel> AvailableProfessions => _project?.Professions ?? Enumerable.Empty<GameDataEntryModel>();
    public IEnumerable<GameDataEntryModel> AvailableSpecies => _project?.Species ?? Enumerable.Empty<GameDataEntryModel>();
    public IEnumerable<GameDataEntryModel> AvailableAlignments => _project?.Alignments ?? Enumerable.Empty<GameDataEntryModel>();

    public ObservableCollection<EquipmentEntryViewModel> EquipmentEntries { get; } = new();
    // ---- Inventory sub-selection ----
    private InventoryEntryViewModel? _selectedInventory;
    public InventoryEntryViewModel? SelectedInventory
    {
        get => _selectedInventory;
        set
        {
            if (SetField(ref _selectedInventory, value))
                OnPropertyChanged(nameof(HasSelectedInventory));
        }
    }
    public bool HasSelectedInventory => _selectedInventory != null;

    public ObservableCollection<InventoryEntryViewModel> InventoryEntries { get; } = new();

    // ---- Patrol waypoint sub-selection ----
    private PatrolWaypointEntryViewModel? _selectedPatrolWaypoint;
    public PatrolWaypointEntryViewModel? SelectedPatrolWaypoint
    {
        get => _selectedPatrolWaypoint;
        set
        {
            if (SetField(ref _selectedPatrolWaypoint, value))
                OnPropertyChanged(nameof(HasSelectedPatrolWaypoint));
        }
    }
    public bool HasSelectedPatrolWaypoint => _selectedPatrolWaypoint != null;

    public ObservableCollection<PatrolWaypointEntryViewModel> PatrolWaypointEntries { get; } = new();

    // ---- Search / Filter ----
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) RefreshFilter(); }
    }

    private string _validationMessage = string.Empty;
    public string ValidationMessage
    {
        get => _validationMessage;
        set => SetField(ref _validationMessage, value);
    }

    // ---- Project wiring ----
    public void SetProject(MapProject project)
    {
        _project = project;
        _npcCounter = project.Npcs.Count > 0
            ? project.Npcs.Max(n =>
            {
                var parts = n.Id.Split('_');
                return parts.Length > 0 && int.TryParse(parts.Last(), out var num) ? num : 0;
            })
            : 0;
        Npcs.Clear();
        foreach (var npc in project.Npcs)
            Npcs.Add(npc);
        SelectedNpc = null;
        RefreshFilter();
    }

    // ---- CRUD ----
    public void CreateNpc()
    {
        if (_project == null) return;
        _npcCounter++;
        var newNpc = new NpcModel
        {
            Id = _project.Id + "_npc_" + _npcCounter.ToString("D4"),
            Key = "New NPC",
            Description = string.Empty,
            Notes = string.Empty,
            Metadata = new EvenniaObjectMetadata(),
        };
        _project.Npcs.Add(newNpc);
        Npcs.Add(newNpc);
        SelectedNpc = newNpc;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }

    public void DeleteNpc()
    {
        if (_project == null || _selectedNpc == null) return;
        var npcId = _selectedNpc.Id;
        var npcKey = _selectedNpc.Key;

        var spawnRefs = _project.Spawns
            .Where(s => s.EntityType == EntityType.Npc && s.EntityId == npcId)
            .ToList();
        if (spawnRefs.Count > 0)
        {
            ValidationMessage = $"Cannot delete \"{npcKey}\": referenced by {spawnRefs.Count} spawn(s).";
            return;
        }

        var shopRefs = _project.Shops
            .Where(s => s.NpcId == npcId)
            .ToList();
        if (shopRefs.Count > 0)
        {
            var names = string.Join(", ", shopRefs.Select(s => s.Key));
            ValidationMessage = $"Cannot delete \"{npcKey}\": owns shop(s): {names}.";
            return;
        }


        var questRefs = _project.Quests
            .Where(q => q.GiverNpcId == npcId || q.TurnInNpcId == npcId
                || q.Objectives.Any(o => o.ObjectiveType == QuestObjectiveType.KillNpc && o.TargetId == npcId))
            .ToList();
        if (questRefs.Count > 0)
        {
            var questNames = string.Join(", ", questRefs.Select(q => q.Key));
            ValidationMessage = $"Cannot delete \"{npcKey}\": used by quest(s): {questNames}.";
            return;
        }

        var dialogueSpeakerRefs = _project.Dialogues
            .Where(d => d.Nodes.Any(n => n.SpeakerNpcId == npcId))
            .ToList();
        if (dialogueSpeakerRefs.Count > 0)
        {
            var dialogueNames = string.Join(", ", dialogueSpeakerRefs.Select(d => d.Key));
            ValidationMessage = $"Cannot delete \"{npcKey}\": used as a speaker by dialogue(s): {dialogueNames}.";
            return;
        }
        _project.Npcs.Remove(_selectedNpc);
        Npcs.Remove(_selectedNpc);
        SelectedNpc = null;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }


    public void DuplicateNpc()
    {
        if (_project == null || _selectedNpc == null) return;
        _npcCounter++;
        var clone = CloneNpc(_selectedNpc);
        clone.Id = _project.Id + "_npc_" + _npcCounter.ToString("D4");
        clone.Key = _selectedNpc.Key + " Copy";
        _project.Npcs.Add(clone);
        Npcs.Add(clone);
        SelectedNpc = clone;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }
    // ---- Metadata helpers ----
    public void AddMetadataAlias()
    {
        if (_selectedNpc == null) return;
        var alias = new AliasModel { Key = string.Empty, Category = string.Empty };
        _selectedNpc.Metadata.Aliases.Add(alias);
        SelectedMetadataAlias = alias;
        MarkDirty();
    }

    public void RemoveMetadataAlias()
    {
        if (_selectedNpc == null || _selectedMetadataAlias == null) return;
        _selectedNpc.Metadata.Aliases.Remove(_selectedMetadataAlias);
        SelectedMetadataAlias = null;
        MarkDirty();
    }

    public void AddMetadataTag()
    {
        if (_selectedNpc == null) return;
        var tag = new TagModel { Key = string.Empty, Category = string.Empty, Data = string.Empty };
        _selectedNpc.Metadata.Tags.Add(tag);
        SelectedMetadataTag = tag;
        MarkDirty();
    }

    public void RemoveMetadataTag()
    {
        if (_selectedNpc == null || _selectedMetadataTag == null) return;
        _selectedNpc.Metadata.Tags.Remove(_selectedMetadataTag);
        SelectedMetadataTag = null;
        MarkDirty();
    }

    public void AddMetadataAttribute()
    {
        if (_selectedNpc == null) return;
        var attr = new AttributeModel { Key = string.Empty, Value = string.Empty, Category = string.Empty, LockString = string.Empty };
        _selectedNpc.Metadata.Attributes.Add(attr);
        SelectedMetadataAttribute = attr;
        MarkDirty();
    }

    public void RemoveMetadataAttribute()
    {
        if (_selectedNpc == null || _selectedMetadataAttribute == null) return;
        _selectedNpc.Metadata.Attributes.Remove(_selectedMetadataAttribute);
        SelectedMetadataAttribute = null;
        MarkDirty();
    }

    public void AddMetadataPermission()
    {
        if (_selectedNpc == null) return;
        _selectedNpc.Metadata.Permissions.Add(string.Empty);
        MarkDirty();
    }

    public void RemoveMetadataPermission()
    {
        if (_selectedNpc == null || _selectedMetadataPermission == null) return;
        _selectedNpc.Metadata.Permissions.Remove(_selectedMetadataPermission);
        SelectedMetadataPermission = null;
        MarkDirty();
    }

    // ---- Ability helpers ----
    public void AddAbility()
    {
        if (_selectedNpc == null) return;
        var ability = new NpcAbilityModel();
        _selectedNpc.Abilities.Add(ability);
        SelectedAbility = ability;
        MarkDirty();
    }

    public void RemoveAbility()
    {
        if (_selectedNpc == null || _selectedAbility == null) return;
        _selectedNpc.Abilities.Remove(_selectedAbility);
        SelectedAbility = null;
        MarkDirty();
    }

    // ---- Resistance helpers ----
    public void AddResistance()
    {
        if (_selectedNpc == null) return;
        var resistance = new NpcResistanceModel();
        _selectedNpc.Resistances.Add(resistance);
        SelectedResistance = resistance;
        MarkDirty();
    }

    public void RemoveResistance()
    {
        if (_selectedNpc == null || _selectedResistance == null) return;
        _selectedNpc.Resistances.Remove(_selectedResistance);
        SelectedResistance = null;
        MarkDirty();
    }

    // ---- Loot Table helpers ----
    public void ClearLootTable()
    {
        if (_selectedNpc == null) return;
        _selectedNpc.LootTableId = string.Empty;
        MarkDirty();
    }
    public void ClearDialogue()
    {
        if (_selectedNpc == null) return;
        _selectedNpc.DialogueId = string.Empty;
        MarkDirty();
    }


    // ---- Equipment helpers ----
    public void AddEquipment()
    {
        if (_selectedNpc == null) return;
        var equipment = new NpcEquipmentModel();
        _selectedNpc.Equipment.Add(equipment);
        var wrapper = new EquipmentEntryViewModel(equipment, AvailableItems);
        EquipmentEntries.Add(wrapper);
        SelectedEquipment = wrapper;
        MarkDirty();
    }

    public void RemoveEquipment()
    {
        if (_selectedNpc == null || _selectedEquipment == null) return;
        _selectedNpc.Equipment.Remove(_selectedEquipment.Model);
        EquipmentEntries.Remove(_selectedEquipment);
        SelectedEquipment = null;
        MarkDirty();
    }

    private void SyncEquipmentEntries()
    {
        EquipmentEntries.Clear();
        if (_selectedNpc != null)
        {
            foreach (var eq in _selectedNpc.Equipment)
                EquipmentEntries.Add(new EquipmentEntryViewModel(eq, AvailableItems));
        }
    }

    // ---- Inventory helpers ----
    public void AddInventory()
    {
        if (_selectedNpc == null) return;
        var inventory = new NpcInventoryItemModel();
        _selectedNpc.Inventory.Add(inventory);
        var wrapper = new InventoryEntryViewModel(inventory, AvailableItems);
        InventoryEntries.Add(wrapper);
        SelectedInventory = wrapper;
        MarkDirty();
    }

    public void RemoveInventory()
    {
        if (_selectedNpc == null || _selectedInventory == null) return;
        _selectedNpc.Inventory.Remove(_selectedInventory.Model);
        InventoryEntries.Remove(_selectedInventory);
        SelectedInventory = null;
        MarkDirty();
    }

    private void SyncInventoryEntries()
    {
        InventoryEntries.Clear();
        if (_selectedNpc != null)
        {
            foreach (var inv in _selectedNpc.Inventory)
                InventoryEntries.Add(new InventoryEntryViewModel(inv, AvailableItems));
        }
    }

    // ---- Patrol helpers ----
    public void AddPatrolWaypoint()
    {
        if (_selectedNpc == null) return;
        var waypoint = new NpcPatrolWaypointModel();
        _selectedNpc.Patrol.Waypoints.Add(waypoint);
        var wrapper = new PatrolWaypointEntryViewModel(waypoint, AvailableRooms);
        PatrolWaypointEntries.Add(wrapper);
        SelectedPatrolWaypoint = wrapper;
        MarkDirty();
    }

    public void RemovePatrolWaypoint()
    {
        if (_selectedNpc == null || _selectedPatrolWaypoint == null) return;
        _selectedNpc.Patrol.Waypoints.Remove(_selectedPatrolWaypoint.Model);
        PatrolWaypointEntries.Remove(_selectedPatrolWaypoint);
        SelectedPatrolWaypoint = null;
        MarkDirty();
    }

    public void MovePatrolWaypointUp()
    {
        if (_selectedNpc == null || _selectedPatrolWaypoint == null) return;
        var index = PatrolWaypointEntries.IndexOf(_selectedPatrolWaypoint);
        if (index <= 0) return;
        PatrolWaypointEntries.Move(index, index - 1);
        _selectedNpc.Patrol.Waypoints.Move(index, index - 1);
        MarkDirty();
    }

    public void MovePatrolWaypointDown()
    {
        if (_selectedNpc == null || _selectedPatrolWaypoint == null) return;
        var index = PatrolWaypointEntries.IndexOf(_selectedPatrolWaypoint);
        if (index < 0 || index >= PatrolWaypointEntries.Count - 1) return;
        PatrolWaypointEntries.Move(index, index + 1);
        _selectedNpc.Patrol.Waypoints.Move(index, index + 1);
        MarkDirty();
    }

    private void SyncPatrolWaypointEntries()
    {
        PatrolWaypointEntries.Clear();
        if (_selectedNpc != null)
        {
            foreach (var wp in _selectedNpc.Patrol.Waypoints)
                PatrolWaypointEntries.Add(new PatrolWaypointEntryViewModel(wp, AvailableRooms));
        }
    }

// ---- Validation ----
    public void Validate()
    {
        if (_selectedNpc == null) { ValidationMessage = string.Empty; return; }
        var errors = new System.Collections.Generic.List<string>();
        if (string.IsNullOrWhiteSpace(_selectedNpc.Key))
            errors.Add("Key cannot be blank.");
        if (string.IsNullOrWhiteSpace(_selectedNpc.Id))
            errors.Add("Id cannot be blank.");
        ValidationMessage = errors.Count > 0 ? string.Join(" | ", errors) : string.Empty;
    }

    // ---- Filter ----
    public void RefreshFilter()
    {
        FilteredNpcs.Clear();
        if (_project == null) return;
        var query = Npcs.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var s = _searchText.Trim();
            query = query.Where(n =>
                (n.Key?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (n.Description?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (n.Notes?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        foreach (var npc in query)
            FilteredNpcs.Add(npc);
    }

    // ---- Clone support ----
    private static NpcModel CloneNpc(NpcModel src) => new()
    {
        Id = src.Id,
        Key = src.Key,
        Description = src.Description,
        Notes = src.Notes,
        LootTableId = src.LootTableId,
        Metadata = CloneMetadata(src.Metadata),
        DialogueId = src.DialogueId,
        Stats = CloneStats(src.Stats),
        Combat = CloneCombat(src.Combat),
        Classification = CloneClassification(src.Classification),
        Behavior = CloneBehavior(src.Behavior),
        Patrol = ClonePatrol(src.Patrol),
        Abilities = new ObservableCollection<NpcAbilityModel>(
            src.Abilities.Select(CloneAbility)),
        Resistances = new ObservableCollection<NpcResistanceModel>(
            src.Resistances.Select(CloneResistance)),
        Equipment = new ObservableCollection<NpcEquipmentModel>(
            src.Equipment.Select(CloneEquipment)),
        Inventory = new ObservableCollection<NpcInventoryItemModel>(
            src.Inventory.Select(CloneInventoryItem)),
    };

    private static NpcAbilityModel CloneAbility(NpcAbilityModel src) => new()
    {
        AbilityId = src.AbilityId,
        AbilityName = src.AbilityName,
        ChancePercent = src.ChancePercent,
        CooldownSeconds = src.CooldownSeconds,
        Priority = src.Priority,
        Notes = src.Notes,
    };

    private static NpcResistanceModel CloneResistance(NpcResistanceModel src) => new()
    {
        DamageType = src.DamageType,
        ResistancePercent = src.ResistancePercent,
        IsImmune = src.IsImmune,
        Notes = src.Notes,
    };

    private static NpcEquipmentModel CloneEquipment(NpcEquipmentModel src) => new()
    {
        ItemId = src.ItemId,
        ItemName = src.ItemName,
        Slot = src.Slot,
    };

    private static NpcInventoryItemModel CloneInventoryItem(NpcInventoryItemModel src) => new()
    {
        ItemId = src.ItemId,
        ItemName = src.ItemName,
        Quantity = src.Quantity,
    };

    private static NpcClassificationModel CloneClassification(NpcClassificationModel src) => new()
    {
        Species = src.Species,
        Profession = src.Profession,
        Faction = src.Faction,
        Alignment = src.Alignment,
    };

    private static NpcBehaviorModel CloneBehavior(NpcBehaviorModel src) => new()
    {
        AggressionMode = src.AggressionMode,
        AggroRange = src.AggroRange,
        CanWander = src.CanWander,
        WanderIntervalSeconds = src.WanderIntervalSeconds,
        CanFlee = src.CanFlee,
        FleeHealthPercent = src.FleeHealthPercent,
    };

    private static NpcPatrolModel ClonePatrol(NpcPatrolModel src) => new()
    {
        Enabled = src.Enabled,
        Loop = src.Loop,
        Waypoints = new ObservableCollection<NpcPatrolWaypointModel>(
            src.Waypoints.Select(ClonePatrolWaypoint)),
    };

    private static NpcPatrolWaypointModel ClonePatrolWaypoint(NpcPatrolWaypointModel src) => new()
    {
        RoomId = src.RoomId,
        RoomName = src.RoomName,
        WaitSeconds = src.WaitSeconds,
    };

    private static NpcCombatModel CloneCombat(NpcCombatModel src) => new()
    {
        DamageMin = src.DamageMin,
        DamageMax = src.DamageMax,
        AccuracyModifier = src.AccuracyModifier,
        AttackSpeed = src.AttackSpeed,
        ArmorClass = src.ArmorClass,
        DamageReduction = src.DamageReduction,
    };

    private static NpcStatsModel CloneStats(NpcStatsModel src) => new()
    {
        Level = src.Level,
        MaxHealth = src.MaxHealth,
        MaxMana = src.MaxMana,
        MaxStamina = src.MaxStamina,
        Strength = src.Strength,
        Agility = src.Agility,
        Intellect = src.Intellect,
        Wisdom = src.Wisdom,
        Charm = src.Charm,
        ExperienceReward = src.ExperienceReward,
        CurrencyReward = src.CurrencyReward,
    };

    private static EvenniaObjectMetadata CloneMetadata(EvenniaObjectMetadata src) => new()
    {
        TypeclassPath = src.TypeclassPath,
        LockString = src.LockString,
        Aliases = new ObservableCollection<AliasModel>(
            src.Aliases.Select(a => new AliasModel { Key = a.Key, Category = a.Category })),
        Tags = new ObservableCollection<TagModel>(
            src.Tags.Select(t => new TagModel { Key = t.Key, Category = t.Category, Data = t.Data })),
        Attributes = new ObservableCollection<AttributeModel>(
            src.Attributes.Select(a => new AttributeModel
            {
                Key = a.Key,
                Value = a.Value,
                Category = a.Category,
                LockString = a.LockString,
            })),
        Permissions = new ObservableCollection<string>(src.Permissions),
    };
}

/// <summary>ViewModel wrapper for NpcEquipmentModel that handles ItemName synchronization.</summary>
public class EquipmentEntryViewModel : BaseViewModel
{
    private readonly NpcEquipmentModel _model;
    private readonly IEnumerable<ItemModel> _availableItems;

    public NpcEquipmentModel Model => _model;

    public string ItemId
    {
        get => _model.ItemId;
        set
        {
            if (_model.ItemId != value)
            {
                _model.ItemId = value;
                // Synchronize ItemName from the selected ItemModel.Key
                _model.ItemName = _availableItems.FirstOrDefault(i => i.Id == value)?.Key ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ItemName));
            }
        }
    }

    public string ItemName => _model.ItemName;

    public EquipmentSlot Slot
    {
        get => _model.Slot;
        set
        {
            if (_model.Slot != value)
            {
                _model.Slot = value;
                OnPropertyChanged();
            }
        }
    }

    public EquipmentEntryViewModel(NpcEquipmentModel model, IEnumerable<ItemModel> availableItems)
    {
        _model = model;
        _availableItems = availableItems;
    }
}

/// <summary>ViewModel wrapper for NpcInventoryItemModel that handles ItemName synchronization.</summary>
public class InventoryEntryViewModel : BaseViewModel
{
    private readonly NpcInventoryItemModel _model;
    private readonly IEnumerable<ItemModel> _availableItems;

    public NpcInventoryItemModel Model => _model;

    public string ItemId
    {
        get => _model.ItemId;
        set
        {
            if (_model.ItemId != value)
            {
                _model.ItemId = value;
                // Synchronize ItemName from the selected ItemModel.Key
                _model.ItemName = _availableItems.FirstOrDefault(i => i.Id == value)?.Key ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ItemName));
            }
        }
    }

    public string ItemName => _model.ItemName;

    public int Quantity
    {
        get => _model.Quantity;
        set
        {
            if (_model.Quantity != value)
            {
                _model.Quantity = value;
                OnPropertyChanged();
            }
        }
    }

    public InventoryEntryViewModel(NpcInventoryItemModel model, IEnumerable<ItemModel> availableItems)
    {
        _model = model;
        _availableItems = availableItems;
    }
}

/// <summary>ViewModel wrapper for NpcPatrolWaypointModel that handles RoomName synchronization.</summary>
public class PatrolWaypointEntryViewModel : BaseViewModel
{
    private readonly NpcPatrolWaypointModel _model;
    private readonly IEnumerable<RoomModel> _availableRooms;

    public NpcPatrolWaypointModel Model => _model;

    public string RoomId
    {
        get => _model.RoomId;
        set
        {
            if (_model.RoomId != value)
            {
                _model.RoomId = value;
                _model.RoomName = _availableRooms.FirstOrDefault(r => r.Id == value)?.Title ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RoomName));
            }
        }
    }

    public string RoomName => _model.RoomName;

    public double WaitSeconds
    {
        get => _model.WaitSeconds;
        set
        {
            if (_model.WaitSeconds != value)
            {
                _model.WaitSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    public PatrolWaypointEntryViewModel(NpcPatrolWaypointModel model, IEnumerable<RoomModel> availableRooms)
    {
        _model = model;
        _availableRooms = availableRooms;
    }
}
