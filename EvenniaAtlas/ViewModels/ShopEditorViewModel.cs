using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EvenniaAtlas.Models;

namespace EvenniaAtlas.ViewModels;

public class ShopEditorViewModel : BaseViewModel
{
    private MapProject? _project;
    private int _shopCounter;

    public Action? MarkDirtyCallback { get; set; }
    private void MarkDirty() => MarkDirtyCallback?.Invoke();

    public ObservableCollection<ShopModel> Shops { get; } = new();
    public ObservableCollection<ShopModel> FilteredShops { get; } = new();

    public ICommand NewShopCommand { get; }
    public ICommand DuplicateShopCommand { get; }
    public ICommand DeleteShopCommand { get; }

    public ICommand AddMetadataAliasCommand { get; }
    public ICommand RemoveMetadataAliasCommand { get; }
    public ICommand AddMetadataTagCommand { get; }
    public ICommand RemoveMetadataTagCommand { get; }
    public ICommand AddMetadataAttributeCommand { get; }
    public ICommand RemoveMetadataAttributeCommand { get; }
    public ICommand AddMetadataPermissionCommand { get; }
    public ICommand RemoveMetadataPermissionCommand { get; }

    public ICommand AddInventoryEntryCommand { get; }
    public ICommand RemoveInventoryEntryCommand { get; }

    public ShopEditorViewModel()
    {
        NewShopCommand = new RelayCommand(_ => CreateShop(), _ => _project != null);
        DuplicateShopCommand = new RelayCommand(_ => DuplicateShop(), _ => HasSelectedShop);
        DeleteShopCommand = new RelayCommand(_ => DeleteShop(), _ => HasSelectedShop);
        AddMetadataAliasCommand = new RelayCommand(_ => AddMetadataAlias(), _ => HasSelectedShop);
        RemoveMetadataAliasCommand = new RelayCommand(_ => RemoveMetadataAlias(), _ => HasSelectedMetadataAlias);
        AddMetadataTagCommand = new RelayCommand(_ => AddMetadataTag(), _ => HasSelectedShop);
        RemoveMetadataTagCommand = new RelayCommand(_ => RemoveMetadataTag(), _ => HasSelectedMetadataTag);
        AddMetadataAttributeCommand = new RelayCommand(_ => AddMetadataAttribute(), _ => HasSelectedShop);
        RemoveMetadataAttributeCommand = new RelayCommand(_ => RemoveMetadataAttribute(), _ => HasSelectedMetadataAttribute);
        AddMetadataPermissionCommand = new RelayCommand(_ => AddMetadataPermission(), _ => HasSelectedShop);
        RemoveMetadataPermissionCommand = new RelayCommand(_ => RemoveMetadataPermission(), _ => HasSelectedMetadataPermission);
        AddInventoryEntryCommand = new RelayCommand(_ => AddInventoryEntry(), _ => HasSelectedShop);
        RemoveInventoryEntryCommand = new RelayCommand(_ => RemoveInventoryEntry(), _ => HasSelectedInventoryEntry);
    }

    // ---- Selection ----
    private ShopModel? _selectedShop;
    public ShopModel? SelectedShop
    {
        get => _selectedShop;
        set
        {
            var oldShop = _selectedShop;
            if (SetField(ref _selectedShop, value))
            {
                if (oldShop != null) MarkDirty();
                OnPropertyChanged(nameof(HasSelectedShop));
                SyncInventoryEntries();
                Validate();
            }
        }
    }
    public bool HasSelectedShop => _selectedShop != null;

    // ---- Available NPCs for owner ----
    public ObservableCollection<NpcModel> AvailableNpcs { get; } = new();

    // ---- Available Items for inventory selection ----
    public IEnumerable<ItemModel> AvailableItems => _project?.Items ?? Enumerable.Empty<ItemModel>();

    // ---- Inventory sub-selection ----
    private ShopInventoryEntryViewModel? _selectedInventoryEntry;
    public ShopInventoryEntryViewModel? SelectedInventoryEntry
    {
        get => _selectedInventoryEntry;
        set
        {
            if (SetField(ref _selectedInventoryEntry, value))
                OnPropertyChanged(nameof(HasSelectedInventoryEntry));
        }
    }
    public bool HasSelectedInventoryEntry => _selectedInventoryEntry != null;

    public ObservableCollection<ShopInventoryEntryViewModel> InventoryEntries { get; } = new();

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
        _shopCounter = project.Shops.Count > 0
            ? project.Shops.Max(s =>
            {
                var parts = s.Id.Split('_');
                return parts.Length > 0 && int.TryParse(parts.Last(), out var num) ? num : 0;
            })
            : 0;
        Shops.Clear();
        foreach (var shop in project.Shops)
            Shops.Add(shop);
        SelectedShop = null;
        AvailableNpcs.Clear();
        foreach (var npc in project.Npcs)
            AvailableNpcs.Add(npc);
        RefreshFilter();
    }
// ---- CRUD ----
    public void CreateShop()
    {
        if (_project == null) return;
        _shopCounter++;
        var newShop = new ShopModel
        {
            Id = _project.Id + "_shop_" + _shopCounter.ToString("D4"),
            Key = "New Shop",
            Description = string.Empty,
            Notes = string.Empty,
            Metadata = new EvenniaObjectMetadata(),
            NpcId = string.Empty,
            Inventory = new ObservableCollection<ShopInventoryEntryModel>(),
        };
        _project.Shops.Add(newShop);
        Shops.Add(newShop);
        SelectedShop = newShop;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }

    public void DeleteShop()
    {
        if (_project == null || _selectedShop == null) return;
        // Phase B: no reference checks needed for shops
        _project.Shops.Remove(_selectedShop);
        Shops.Remove(_selectedShop);
        SelectedShop = null;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }

    public void DuplicateShop()
    {
        if (_project == null || _selectedShop == null) return;
        _shopCounter++;
        var clone = CloneShop(_selectedShop);
        clone.Id = _project.Id + "_shop_" + _shopCounter.ToString("D4");
        clone.Key = _selectedShop.Key + " Copy";
        _project.Shops.Add(clone);
        Shops.Add(clone);
        SelectedShop = clone;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }

    // ---- Metadata helpers ----
    public void AddMetadataAlias()
    {
        if (_selectedShop == null) return;
        var alias = new AliasModel { Key = string.Empty, Category = string.Empty };
        _selectedShop.Metadata.Aliases.Add(alias);
        SelectedMetadataAlias = alias;
        MarkDirty();
    }

    public void RemoveMetadataAlias()
    {
        if (_selectedShop == null || _selectedMetadataAlias == null) return;
        _selectedShop.Metadata.Aliases.Remove(_selectedMetadataAlias);
        SelectedMetadataAlias = null;
        MarkDirty();
    }

    public void AddMetadataTag()
    {
        if (_selectedShop == null) return;
        var tag = new TagModel { Key = string.Empty, Category = string.Empty, Data = string.Empty };
        _selectedShop.Metadata.Tags.Add(tag);
        SelectedMetadataTag = tag;
        MarkDirty();
    }

    public void RemoveMetadataTag()
    {
        if (_selectedShop == null || _selectedMetadataTag == null) return;
        _selectedShop.Metadata.Tags.Remove(_selectedMetadataTag);
        SelectedMetadataTag = null;
        MarkDirty();
    }

    public void AddMetadataAttribute()
    {
        if (_selectedShop == null) return;
        var attr = new AttributeModel
        {
            Key = string.Empty,
            Value = string.Empty,
            Category = string.Empty,
            LockString = string.Empty,
        };
        _selectedShop.Metadata.Attributes.Add(attr);
        SelectedMetadataAttribute = attr;
        MarkDirty();
    }

    public void RemoveMetadataAttribute()
    {
        if (_selectedShop == null || _selectedMetadataAttribute == null) return;
        _selectedShop.Metadata.Attributes.Remove(_selectedMetadataAttribute);
        SelectedMetadataAttribute = null;
        MarkDirty();
    }

    public void AddMetadataPermission()
    {
        if (_selectedShop == null) return;
        _selectedShop.Metadata.Permissions.Add(string.Empty);
        MarkDirty();
    }

    public void RemoveMetadataPermission()
    {
        if (_selectedShop == null || _selectedMetadataPermission == null) return;
        _selectedShop.Metadata.Permissions.Remove(_selectedMetadataPermission);
        SelectedMetadataPermission = null;
        MarkDirty();
    }

    // ---- Inventory helpers ----
    public void AddInventoryEntry()
    {
        if (_selectedShop == null) return;
        var entry = new ShopInventoryEntryModel
        {
            ItemId = string.Empty,
            ItemName = string.Empty,
            Quantity = 1,
            IsUnlimited = false,
            BuyPrice = 0m,
            SellPrice = 0m,
            IsEnabled = true,
        };
        _selectedShop.Inventory.Add(entry);
        var wrapper = new ShopInventoryEntryViewModel(entry, AvailableItems);
        InventoryEntries.Add(wrapper);
        SelectedInventoryEntry = wrapper;
        MarkDirty();
    }

    public void RemoveInventoryEntry()
    {
        if (_selectedShop == null || _selectedInventoryEntry == null) return;
        _selectedShop.Inventory.Remove(_selectedInventoryEntry.Model);
        InventoryEntries.Remove(_selectedInventoryEntry);
        SelectedInventoryEntry = null;
        MarkDirty();
    }

    private void SyncInventoryEntries()
    {
        InventoryEntries.Clear();
        SelectedInventoryEntry = null;
        if (_selectedShop != null)
        {
            foreach (var inv in _selectedShop.Inventory)
                InventoryEntries.Add(new ShopInventoryEntryViewModel(inv, AvailableItems));
        }
    }

    // ---- Validation ----
    public void Validate()
    {
        if (_selectedShop == null) { ValidationMessage = string.Empty; return; }
        var errors = new System.Collections.Generic.List<string>();
        if (string.IsNullOrWhiteSpace(_selectedShop.Key))
            errors.Add("Key cannot be blank.");
        if (string.IsNullOrWhiteSpace(_selectedShop.Id))
            errors.Add("Id cannot be blank.");
        ValidationMessage = errors.Count > 0 ? string.Join(" | ", errors) : string.Empty;
    }

    // ---- Filter ----
    public void RefreshFilter()
    {
        FilteredShops.Clear();
        if (_project == null) return;
        var query = Shops.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var s = _searchText.Trim();
            query = query.Where(sh =>
                (sh.Key?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (sh.Description?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (sh.Notes?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        foreach (var shop in query)
            FilteredShops.Add(shop);
    }
// ---- Clone support ----
    private static ShopModel CloneShop(ShopModel src) => new()
    {
        Id = src.Id,
        Key = src.Key,
        Description = src.Description,
        Notes = src.Notes,
        NpcId = src.NpcId,
        Metadata = CloneMetadata(src.Metadata),
        Inventory = new ObservableCollection<ShopInventoryEntryModel>(
            src.Inventory.Select(CloneInventoryEntry)),
    };

    private static ShopInventoryEntryModel CloneInventoryEntry(ShopInventoryEntryModel src) => new()
    {
        ItemId = src.ItemId,
        ItemName = src.ItemName,
        Quantity = src.Quantity,
        IsUnlimited = src.IsUnlimited,
        BuyPrice = src.BuyPrice,
        SellPrice = src.SellPrice,
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
/// <summary>ViewModel wrapper for ShopInventoryEntryModel that handles ItemName synchronization.</summary>
public class ShopInventoryEntryViewModel : BaseViewModel
{
    private readonly ShopInventoryEntryModel _model;
    private readonly IEnumerable<ItemModel> _availableItems;

    public ShopInventoryEntryModel Model => _model;

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

    public bool IsUnlimited
    {
        get => _model.IsUnlimited;
        set
        {
            if (_model.IsUnlimited != value)
            {
                _model.IsUnlimited = value;
                OnPropertyChanged();
            }
        }
    }

    public decimal BuyPrice
    {
        get => _model.BuyPrice;
        set
        {
            if (_model.BuyPrice != value)
            {
                _model.BuyPrice = value;
                OnPropertyChanged();
            }
        }
    }

    public decimal SellPrice
    {
        get => _model.SellPrice;
        set
        {
            if (_model.SellPrice != value)
            {
                _model.SellPrice = value;
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

    public ShopInventoryEntryViewModel(ShopInventoryEntryModel model, IEnumerable<ItemModel> availableItems)
    {
        _model = model;
        _availableItems = availableItems;
    }
}