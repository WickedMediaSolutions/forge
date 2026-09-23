using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EvenniaAtlas.Models;

namespace EvenniaAtlas.ViewModels;

public class LootTableEditorViewModel : BaseViewModel
{
    private MapProject? _project;
    private int _lootCounter;

    public Action? MarkDirtyCallback { get; set; }
    private void MarkDirty() => MarkDirtyCallback?.Invoke();

    public ObservableCollection<LootTableModel> LootTables { get; } = new();
    public ObservableCollection<LootTableModel> FilteredLootTables { get; } = new();

    public ICommand NewLootTableCommand { get; }
    public ICommand DuplicateLootTableCommand { get; }
    public ICommand DeleteLootTableCommand { get; }

    public ICommand AddMetadataAliasCommand { get; }
    public ICommand RemoveMetadataAliasCommand { get; }
    public ICommand AddMetadataTagCommand { get; }
    public ICommand RemoveMetadataTagCommand { get; }
    public ICommand AddMetadataAttributeCommand { get; }
    public ICommand RemoveMetadataAttributeCommand { get; }
    public ICommand AddMetadataPermissionCommand { get; }
    public ICommand RemoveMetadataPermissionCommand { get; }
    public ICommand AddLootEntryCommand { get; }
    public ICommand RemoveLootEntryCommand { get; }

    public LootTableEditorViewModel()
    {
        NewLootTableCommand = new RelayCommand(_ => CreateLootTable(), _ => _project != null);
        DuplicateLootTableCommand = new RelayCommand(_ => DuplicateLootTable(), _ => HasSelectedLootTable);
        DeleteLootTableCommand = new RelayCommand(_ => DeleteLootTable(), _ => HasSelectedLootTable);
        AddMetadataAliasCommand = new RelayCommand(_ => AddMetadataAlias(), _ => HasSelectedLootTable);
        RemoveMetadataAliasCommand = new RelayCommand(_ => RemoveMetadataAlias(), _ => HasSelectedMetadataAlias);
        AddMetadataTagCommand = new RelayCommand(_ => AddMetadataTag(), _ => HasSelectedLootTable);
        RemoveMetadataTagCommand = new RelayCommand(_ => RemoveMetadataTag(), _ => HasSelectedMetadataTag);
        AddMetadataAttributeCommand = new RelayCommand(_ => AddMetadataAttribute(), _ => HasSelectedLootTable);
        RemoveMetadataAttributeCommand = new RelayCommand(_ => RemoveMetadataAttribute(), _ => HasSelectedMetadataAttribute);
        AddMetadataPermissionCommand = new RelayCommand(_ => AddMetadataPermission(), _ => HasSelectedLootTable);
        RemoveMetadataPermissionCommand = new RelayCommand(_ => RemoveMetadataPermission(), _ => HasSelectedMetadataPermission);
        AddLootEntryCommand = new RelayCommand(_ => AddLootEntry(), _ => HasSelectedLootTable);
        RemoveLootEntryCommand = new RelayCommand(_ => RemoveLootEntry(), _ => HasSelectedLootEntry);
    }
// ---- Selection ----
    private LootTableModel? _selectedLootTable;
    public LootTableModel? SelectedLootTable
    {
        get => _selectedLootTable;
        set
        {
            var oldTable = _selectedLootTable;
            if (SetField(ref _selectedLootTable, value))
            {
                if (oldTable != null) MarkDirty();
                OnPropertyChanged(nameof(HasSelectedLootTable));
                SyncLootEntries();
                Validate();
            }
        }
    }
    public bool HasSelectedLootTable => _selectedLootTable != null;

    // ---- Available Items for entry selection ----
    public IEnumerable<ItemModel> AvailableItems => _project?.Items ?? Enumerable.Empty<ItemModel>();

    // ---- Loot Entry sub-selection ----
    private LootEntryViewModel? _selectedLootEntry;
    public LootEntryViewModel? SelectedLootEntry
    {
        get => _selectedLootEntry;
        set
        {
            if (SetField(ref _selectedLootEntry, value))
                OnPropertyChanged(nameof(HasSelectedLootEntry));
        }
    }
    public bool HasSelectedLootEntry => _selectedLootEntry != null;

    public ObservableCollection<LootEntryViewModel> LootEntries { get; } = new();

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

    // ---- Metadata sub-selections ----
    private AliasModel? _selectedMetadataAlias;
    public AliasModel? SelectedMetadataAlias
    {
        get => _selectedMetadataAlias;
        set { if (SetField(ref _selectedMetadataAlias, value)) OnPropertyChanged(nameof(HasSelectedMetadataAlias)); }
    }
    public bool HasSelectedMetadataAlias => _selectedMetadataAlias != null;

    private TagModel? _selectedMetadataTag;
    public TagModel? SelectedMetadataTag
    {
        get => _selectedMetadataTag;
        set { if (SetField(ref _selectedMetadataTag, value)) OnPropertyChanged(nameof(HasSelectedMetadataTag)); }
    }
    public bool HasSelectedMetadataTag => _selectedMetadataTag != null;

    private AttributeModel? _selectedMetadataAttribute;
    public AttributeModel? SelectedMetadataAttribute
    {
        get => _selectedMetadataAttribute;
        set { if (SetField(ref _selectedMetadataAttribute, value)) OnPropertyChanged(nameof(HasSelectedMetadataAttribute)); }
    }
    public bool HasSelectedMetadataAttribute => _selectedMetadataAttribute != null;

    private string? _selectedMetadataPermission;
    public string? SelectedMetadataPermission
    {
        get => _selectedMetadataPermission;
        set { if (SetField(ref _selectedMetadataPermission, value)) OnPropertyChanged(nameof(HasSelectedMetadataPermission)); }
    }
    public bool HasSelectedMetadataPermission => _selectedMetadataPermission != null;

    // ---- Project wiring ----
    public void SetProject(MapProject project)
    {
        _project = project;
        _lootCounter = project.LootTables.Count > 0
            ? project.LootTables.Max(lt =>
            {
                var parts = lt.Id.Split('_');
                return parts.Length > 0 && int.TryParse(parts.Last(), out var num) ? num : 0;
            })
            : 0;
        LootTables.Clear();
        foreach (var lootTable in project.LootTables)
            LootTables.Add(lootTable);
        SelectedLootTable = null;
        RefreshFilter();
    }
// ---- CRUD ----
    public void CreateLootTable()
    {
        if (_project == null) return;
        _lootCounter++;
        var newLootTable = new LootTableModel
        {
            Id = _project.Id + "_loot_" + _lootCounter.ToString("D4"),
            Key = "New Loot Table",
            Description = string.Empty,
            Notes = string.Empty,
            Metadata = new EvenniaObjectMetadata(),
            Entries = new ObservableCollection<LootEntryModel>(),
        };
        _project.LootTables.Add(newLootTable);
        LootTables.Add(newLootTable);
        SelectedLootTable = newLootTable;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }

    public void DeleteLootTable()
    {
        if (_project == null || _selectedLootTable == null) return;
        var tableId = _selectedLootTable.Id;
        var tableKey = _selectedLootTable.Key;

        var npcRefs = _project.Npcs
            .Where(n => n.LootTableId == tableId)
            .ToList();
        if (npcRefs.Count > 0)
        {
            var names = string.Join(", ", npcRefs.Select(n => n.Key));
            ValidationMessage = $"Cannot delete \"{tableKey}\": used by NPC(s): {names}.";
            return;
        }

        var questRefs = _project.Quests
            .Where(q => q.RewardLootTableId == tableId)
            .ToList();
        if (questRefs.Count > 0)
        {
            var questNames = string.Join(", ", questRefs.Select(q => q.Key));
            ValidationMessage = $"Cannot delete \"{tableKey}\": used by quest(s): {questNames}.";
            return;
        }
        _project.LootTables.Remove(_selectedLootTable);
        LootTables.Remove(_selectedLootTable);
        SelectedLootTable = null;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }

    public void DuplicateLootTable()
    {
        if (_project == null || _selectedLootTable == null) return;
        _lootCounter++;
        var clone = CloneLootTable(_selectedLootTable);
        clone.Id = _project.Id + "_loot_" + _lootCounter.ToString("D4");
        clone.Key = _selectedLootTable.Key + " Copy";
        _project.LootTables.Add(clone);
        LootTables.Add(clone);
        SelectedLootTable = clone;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }
// ---- Metadata helpers ----
    public void AddMetadataAlias()
    {
        if (_selectedLootTable == null) return;
        var alias = new AliasModel { Key = string.Empty, Category = string.Empty };
        _selectedLootTable.Metadata.Aliases.Add(alias);
        SelectedMetadataAlias = alias;
        MarkDirty();
    }

    public void RemoveMetadataAlias()
    {
        if (_selectedLootTable == null || _selectedMetadataAlias == null) return;
        _selectedLootTable.Metadata.Aliases.Remove(_selectedMetadataAlias);
        SelectedMetadataAlias = null;
        MarkDirty();
    }

    public void AddMetadataTag()
    {
        if (_selectedLootTable == null) return;
        var tag = new TagModel { Key = string.Empty, Category = string.Empty, Data = string.Empty };
        _selectedLootTable.Metadata.Tags.Add(tag);
        SelectedMetadataTag = tag;
        MarkDirty();
    }

    public void RemoveMetadataTag()
    {
        if (_selectedLootTable == null || _selectedMetadataTag == null) return;
        _selectedLootTable.Metadata.Tags.Remove(_selectedMetadataTag);
        SelectedMetadataTag = null;
        MarkDirty();
    }

    public void AddMetadataAttribute()
    {
        if (_selectedLootTable == null) return;
        var attr = new AttributeModel
        {
            Key = string.Empty,
            Value = string.Empty,
            Category = string.Empty,
            LockString = string.Empty,
        };
        _selectedLootTable.Metadata.Attributes.Add(attr);
        SelectedMetadataAttribute = attr;
        MarkDirty();
    }

    public void RemoveMetadataAttribute()
    {
        if (_selectedLootTable == null || _selectedMetadataAttribute == null) return;
        _selectedLootTable.Metadata.Attributes.Remove(_selectedMetadataAttribute);
        SelectedMetadataAttribute = null;
        MarkDirty();
    }

    public void AddMetadataPermission()
    {
        if (_selectedLootTable == null) return;
        _selectedLootTable.Metadata.Permissions.Add(string.Empty);
        MarkDirty();
    }

    public void RemoveMetadataPermission()
    {
        if (_selectedLootTable == null || _selectedMetadataPermission == null) return;
        _selectedLootTable.Metadata.Permissions.Remove(_selectedMetadataPermission);
        SelectedMetadataPermission = null;
        MarkDirty();
    }
// ---- Loot Entry helpers ----
    public void AddLootEntry()
    {
        if (_selectedLootTable == null) return;
        var entry = new LootEntryModel
        {
            ItemId = string.Empty,
            ItemName = string.Empty,
            ChancePercent = 0,
            QuantityMin = 1,
            QuantityMax = 1,
            IsEnabled = true,
        };
        _selectedLootTable.Entries.Add(entry);
        var wrapper = new LootEntryViewModel(entry, AvailableItems);
        LootEntries.Add(wrapper);
        SelectedLootEntry = wrapper;
        MarkDirty();
    }

    public void RemoveLootEntry()
    {
        if (_selectedLootTable == null || _selectedLootEntry == null) return;
        _selectedLootTable.Entries.Remove(_selectedLootEntry.Model);
        LootEntries.Remove(_selectedLootEntry);
        SelectedLootEntry = null;
        MarkDirty();
    }

    private void SyncLootEntries()
    {
        LootEntries.Clear();
        SelectedLootEntry = null;
        if (_selectedLootTable != null)
        {
            foreach (var entry in _selectedLootTable.Entries)
                LootEntries.Add(new LootEntryViewModel(entry, AvailableItems));
        }
    }

    // ---- Validation ----
    public void Validate()
    {
        if (_selectedLootTable == null) { ValidationMessage = string.Empty; return; }
        var errors = new System.Collections.Generic.List<string>();
        if (string.IsNullOrWhiteSpace(_selectedLootTable.Id))
            errors.Add("Id cannot be blank.");
        if (string.IsNullOrWhiteSpace(_selectedLootTable.Key))
            errors.Add("Key cannot be blank.");
        ValidationMessage = errors.Count > 0 ? string.Join(" | ", errors) : string.Empty;
    }

    // ---- Filter ----
    public void RefreshFilter()
    {
        FilteredLootTables.Clear();
        if (_project == null) return;
        var query = LootTables.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var s = _searchText.Trim();
            query = query.Where(lt =>
                (lt.Key?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (lt.Description?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (lt.Notes?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        foreach (var lootTable in query)
            FilteredLootTables.Add(lootTable);
    }

    // ---- Clone support ----
    private static LootTableModel CloneLootTable(LootTableModel src) => new()
    {
        Id = src.Id,
        Key = src.Key,
        Description = src.Description,
        Notes = src.Notes,
        Metadata = CloneMetadata(src.Metadata),
        Entries = new ObservableCollection<LootEntryModel>(
            src.Entries.Select(CloneLootEntry)),
    };

    private static LootEntryModel CloneLootEntry(LootEntryModel src) => new()
    {
        ItemId = src.ItemId,
        ItemName = src.ItemName,
        ChancePercent = src.ChancePercent,
        QuantityMin = src.QuantityMin,
        QuantityMax = src.QuantityMax,
        IsEnabled = src.IsEnabled,
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
/// <summary>ViewModel wrapper for LootEntryModel that handles ItemName synchronization.</summary>
public class LootEntryViewModel : BaseViewModel
{
    private readonly LootEntryModel _model;
    private readonly IEnumerable<ItemModel> _availableItems;

    public LootEntryModel Model => _model;

    public string ItemId
    {
        get => _model.ItemId;
        set
        {
            if (_model.ItemId != value)
            {
                _model.ItemId = value;
                _model.ItemName = _availableItems.FirstOrDefault(i => i.Id == value)?.Key ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ItemName));
            }
        }
    }

    public string ItemName => _model.ItemName;

    public double ChancePercent
    {
        get => _model.ChancePercent;
        set
        {
            if (_model.ChancePercent != value)
            {
                _model.ChancePercent = value;
                OnPropertyChanged();
            }
        }
    }

    public int QuantityMin
    {
        get => _model.QuantityMin;
        set
        {
            if (_model.QuantityMin != value)
            {
                _model.QuantityMin = value;
                OnPropertyChanged();
            }
        }
    }

    public int QuantityMax
    {
        get => _model.QuantityMax;
        set
        {
            if (_model.QuantityMax != value)
            {
                _model.QuantityMax = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsEnabled
    {
        get => _model.IsEnabled;
        set
        {
            if (_model.IsEnabled != value)
            {
                _model.IsEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public LootEntryViewModel(LootEntryModel model, IEnumerable<ItemModel> availableItems)
    {
        _model = model;
        _availableItems = availableItems;
    }
}
