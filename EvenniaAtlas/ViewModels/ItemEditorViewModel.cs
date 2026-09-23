using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EvenniaAtlas.Models;

namespace EvenniaAtlas.ViewModels;

public class ItemEditorViewModel : BaseViewModel
{
    private MapProject? _project;
    private int _itemCounter;

    public Action? MarkDirtyCallback { get; set; }
    private void MarkDirty() => MarkDirtyCallback?.Invoke();

    public ObservableCollection<ItemModel> Items { get; } = new();
    public ObservableCollection<ItemModel> FilteredItems { get; } = new();

    public ObservableCollection<ItemType?> TypeFilterOptions { get; } = new()
    {
        null,
        ItemType.Generic, ItemType.Weapon, ItemType.Armor, ItemType.Shield,
        ItemType.Container, ItemType.Consumable, ItemType.Scroll, ItemType.Wand,
        ItemType.Rod, ItemType.Quest, ItemType.Crafting, ItemType.Key,
        ItemType.Treasure, ItemType.Miscellaneous
    };

    public ICommand NewItemCommand { get; }
    public ICommand DuplicateItemCommand { get; }
    public ICommand DeleteItemCommand { get; }

    public ICommand AddRequirementCommand { get; }
    public ICommand RemoveRequirementCommand { get; }
    public ICommand AddModifierCommand { get; }
    public ICommand RemoveModifierCommand { get; }
    public ICommand AddEffectCommand { get; }
    public ICommand RemoveEffectCommand { get; }
    public ICommand AddSourceCommand { get; }
    public ICommand RemoveSourceCommand { get; }
    public ICommand AddCraftingComponentCommand { get; }
    public ICommand RemoveCraftingComponentCommand { get; }
    public ICommand AddMetadataAliasCommand { get; }
    public ICommand RemoveMetadataAliasCommand { get; }
    public ICommand AddMetadataTagCommand { get; }
    public ICommand RemoveMetadataTagCommand { get; }
    public ICommand AddMetadataAttributeCommand { get; }
    public ICommand RemoveMetadataAttributeCommand { get; }
    public ICommand AddMetadataPermissionCommand { get; }
    public ICommand RemoveMetadataPermissionCommand { get; }

    public ItemEditorViewModel()
    {
        NewItemCommand = new RelayCommand(_ => CreateItem(), _ => _project != null);
        DuplicateItemCommand = new RelayCommand(_ => DuplicateItem(), _ => HasSelectedItem);
        DeleteItemCommand = new RelayCommand(_ => DeleteItem(), _ => HasSelectedItem);
        AddRequirementCommand = new RelayCommand(_ => AddRequirement(), _ => HasSelectedItem);
        RemoveRequirementCommand = new RelayCommand(_ => RemoveRequirement(), _ => HasSelectedRequirement);
        AddModifierCommand = new RelayCommand(_ => AddModifier(), _ => HasSelectedItem);
        RemoveModifierCommand = new RelayCommand(_ => RemoveModifier(), _ => HasSelectedModifier);
        AddEffectCommand = new RelayCommand(_ => AddEffect(), _ => HasSelectedItem);
        RemoveEffectCommand = new RelayCommand(_ => RemoveEffect(), _ => HasSelectedEffect);
        AddSourceCommand = new RelayCommand(_ => AddSource(), _ => HasSelectedItem);
        RemoveSourceCommand = new RelayCommand(_ => RemoveSource(), _ => HasSelectedSource);
        AddCraftingComponentCommand = new RelayCommand(_ => AddCraftingComponent(), _ => HasSelectedItem);
        RemoveCraftingComponentCommand = new RelayCommand(_ => RemoveCraftingComponent(), _ => HasSelectedCraftingComponent);
        AddMetadataAliasCommand = new RelayCommand(_ => AddMetadataAlias(), _ => HasSelectedItem);
        RemoveMetadataAliasCommand = new RelayCommand(_ => RemoveMetadataAlias(), _ => HasSelectedMetadataAlias);
        AddMetadataTagCommand = new RelayCommand(_ => AddMetadataTag(), _ => HasSelectedItem);
        RemoveMetadataTagCommand = new RelayCommand(_ => RemoveMetadataTag(), _ => HasSelectedMetadataTag);
        AddMetadataAttributeCommand = new RelayCommand(_ => AddMetadataAttribute(), _ => HasSelectedItem);
        RemoveMetadataAttributeCommand = new RelayCommand(_ => RemoveMetadataAttribute(), _ => HasSelectedMetadataAttribute);
        AddMetadataPermissionCommand = new RelayCommand(_ => AddMetadataPermission(), _ => HasSelectedItem);
        RemoveMetadataPermissionCommand = new RelayCommand(_ => RemoveMetadataPermission(), _ => HasSelectedMetadataPermission);
    }

    private ItemModel? _selectedItem;
    public ItemModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            var oldItem = _selectedItem;
            if (SetField(ref _selectedItem, value))
            {
                if (oldItem != null) MarkDirty();
                OnPropertyChanged(nameof(HasSelectedItem));
                SelectedRequirement = null;
                SelectedModifier = null;
                SelectedEffect = null;
                SelectedSource = null;
                SelectedCraftingComponent = null;
                SelectedMetadataAlias = null;
                SelectedMetadataTag = null;
                SelectedMetadataAttribute = null;
                SelectedMetadataPermission = null;
                ValidationMessage = string.Empty;
                OnItemTypeChanged();
            }
        }
    }

    public bool HasSelectedItem => _selectedItem != null;

    private ItemRequirementModel? _selectedRequirement;
    public ItemRequirementModel? SelectedRequirement
    {
        get => _selectedRequirement;
        set
        {
            if (SetField(ref _selectedRequirement, value))
                OnPropertyChanged(nameof(HasSelectedRequirement));
        }
    }

    public bool HasSelectedRequirement => _selectedRequirement != null;

    private ItemModifierModel? _selectedModifier;
    public ItemModifierModel? SelectedModifier
    {
        get => _selectedModifier;
        set
        {
            if (SetField(ref _selectedModifier, value))
                OnPropertyChanged(nameof(HasSelectedModifier));
        }
    }

    public bool HasSelectedModifier => _selectedModifier != null;

    private ItemEffectModel? _selectedEffect;
    public ItemEffectModel? SelectedEffect
    {
        get => _selectedEffect;
        set
        {
            if (SetField(ref _selectedEffect, value))
                OnPropertyChanged(nameof(HasSelectedEffect));
        }
    }

    public bool HasSelectedEffect => _selectedEffect != null;

    private ItemSourceModel? _selectedSource;
    public ItemSourceModel? SelectedSource
    {
        get => _selectedSource;
        set
        {
            if (SetField(ref _selectedSource, value))
                OnPropertyChanged(nameof(HasSelectedSource));
        }
    }

    public bool HasSelectedSource => _selectedSource != null;

    private CraftingComponentModel? _selectedCraftingComponent;
    public CraftingComponentModel? SelectedCraftingComponent
    {
        get => _selectedCraftingComponent;
        set
        {
            if (SetField(ref _selectedCraftingComponent, value))
                OnPropertyChanged(nameof(HasSelectedCraftingComponent));
        }
    }

    public bool HasSelectedCraftingComponent => _selectedCraftingComponent != null;

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

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) RefreshFilter(); }
    }

    private ItemType? _selectedTypeFilter;
    public ItemType? SelectedTypeFilter
    {
        get => _selectedTypeFilter;
        set { if (SetField(ref _selectedTypeFilter, value)) RefreshFilter(); }
    }

    private string _validationMessage = string.Empty;
    public string ValidationMessage
    {
        get => _validationMessage;
        set => SetField(ref _validationMessage, value);
    }

    public void SetProject(MapProject project)
    {
        _project = project;
        _itemCounter = project.Items.Count > 0
            ? project.Items.Max(i =>
            {
                var parts = i.Id.Split('_');
                return parts.Length > 0 && int.TryParse(parts.Last(), out var n) ? n : 0;
            })
            : 0;
        Items.Clear();
        foreach (var item in project.Items)
            Items.Add(item);
        SelectedItem = null;
        RefreshFilter();
    }

    public void CreateItem()
    {
        if (_project == null) return;
        _itemCounter++;
        var newItem = new ItemModel
        {
            Id = _project.Id + "_item_" + _itemCounter.ToString("D4"),
            Key = "New Item",
            Description = string.Empty,
            Notes = string.Empty,
            ItemType = ItemType.Generic,
            EquipmentSlot = EquipmentSlot.None,
            Metadata = new EvenniaObjectMetadata(),
            WeaponData = null,
            ArmorData = null,
            Requirements = new ObservableCollection<ItemRequirementModel>(),
            Modifiers = new ObservableCollection<ItemModifierModel>(),
            Effects = new ObservableCollection<ItemEffectModel>(),
            Sources = new ObservableCollection<ItemSourceModel>(),
            CraftingComponents = new ObservableCollection<CraftingComponentModel>(),
        };
        _project.Items.Add(newItem);
        Items.Add(newItem);
        SelectedItem = newItem;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }

    public void DeleteItem()
    {
        if (_project == null || _selectedItem == null) return;
        var itemId = _selectedItem.Id;
        var itemKey = _selectedItem.Key;

        var spawnRefs = _project.Spawns
            .Where(s => s.EntityType == EntityType.Item && s.EntityId == itemId)
            .ToList();
        if (spawnRefs.Count > 0)
        {
            ValidationMessage = $"Cannot delete \"{itemKey}\": referenced by {spawnRefs.Count} spawn(s).";
            return;
        }

        var craftingRefs = _project.Items
            .Where(i => i.Id != itemId && i.CraftingComponents.Any(cc => cc.ItemId == itemId))
            .ToList();
        if (craftingRefs.Count > 0)
        {
            var names = string.Join(", ", craftingRefs.Select(i => i.Key));
            ValidationMessage = $"Cannot delete \"{itemKey}\": crafting component of: {names}.";
            return;
        }

        var sourceRefs = _project.Items
            .Where(i => i.Id != itemId && i.Sources.Any(s => s.SourceId == itemId))
            .ToList();
        if (sourceRefs.Count > 0)
        {
            var names = string.Join(", ", sourceRefs.Select(i => i.Key));
            ValidationMessage = $"Cannot delete \"{itemKey}\": referenced as source by: {names}.";
            return;
        }

        var npcEquipmentRefs = _project.Npcs
            .Where(n => n.Equipment.Any(e => e.ItemId == itemId))
            .ToList();
        if (npcEquipmentRefs.Count > 0)
        {
            var names = string.Join(", ", npcEquipmentRefs.Select(n => n.Key));
            ValidationMessage = $"Cannot delete \"{itemKey}\": equipped by NPC(s): {names}.";
            return;
        }

        var npcInventoryRefs = _project.Npcs
            .Where(n => n.Inventory.Any(i => i.ItemId == itemId))
            .ToList();
        if (npcInventoryRefs.Count > 0)
        {
            var names = string.Join(", ", npcInventoryRefs.Select(n => n.Key));
            ValidationMessage = $"Cannot delete \"{itemKey}\": carried by NPC(s): {names}.";
            return;
        }

        var shopRefs = _project.Shops
            .Where(s => s.Inventory.Any(e => e.ItemId == itemId))
            .ToList();
        if (shopRefs.Count > 0)
        {
            var names = string.Join(", ", shopRefs.Select(s => s.Key));
            ValidationMessage = $"Cannot delete \"{itemKey}\": sold by shop(s): {names}.";
            return;
        }

        var lootTableRefs = _project.LootTables
            .Where(t => t.Entries.Any(e => e.ItemId == itemId))
            .ToList();
        if (lootTableRefs.Count > 0)
        {
            var names = string.Join(", ", lootTableRefs.Select(t => t.Key));
            ValidationMessage = $"Cannot delete \"{itemKey}\": used by loot table(s): {names}.";
            return;
        }

        var questRefs = _project.Quests
            .Where(q => q.Objectives.Any(o => o.ObjectiveType == QuestObjectiveType.CollectItem && o.TargetId == itemId)
                || q.ItemRewards.Any(r => r.ItemId == itemId))
            .ToList();
        if (questRefs.Count > 0)
        {
            var questNames = string.Join(", ", questRefs.Select(q => q.Key));
            ValidationMessage = $"Cannot delete \"{itemKey}\": used by quest(s): {questNames}.";
            return;
        }
        _project.Items.Remove(_selectedItem);
        Items.Remove(_selectedItem);
        SelectedItem = null;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }

    public void DuplicateItem()
    {
        if (_project == null || _selectedItem == null) return;
        _itemCounter++;
        var clone = CloneItem(_selectedItem);
        clone.Id = _project.Id + "_item_" + _itemCounter.ToString("D4");
        clone.Key = _selectedItem.Key + " Copy";
        _project.Items.Add(clone);
        Items.Add(clone);
        SelectedItem = clone;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }

public void AddRequirement()
    {
        if (_selectedItem == null) return;
        var req = new ItemRequirementModel
        {
            RequirementType = RequirementType.Level,
            Operator = ComparisonOperator.GreaterThanOrEqual,
            Value = string.Empty,
        };
        _selectedItem.Requirements.Add(req);
        SelectedRequirement = req;
        MarkDirty();
    }

    public void RemoveRequirement()
    {
        if (_selectedItem == null || _selectedRequirement == null) return;
        _selectedItem.Requirements.Remove(_selectedRequirement);
        SelectedRequirement = null;
        MarkDirty();
    }

    public void AddModifier()
    {
        if (_selectedItem == null) return;
        var mod = new ItemModifierModel
        {
            ModifierType = string.Empty,
            Value = 0,
            ValueType = ModifierValueType.Flat,
            Notes = string.Empty,
        };
        _selectedItem.Modifiers.Add(mod);
        SelectedModifier = mod;
        MarkDirty();
    }

    public void RemoveModifier()
    {
        if (_selectedItem == null || _selectedModifier == null) return;
        _selectedItem.Modifiers.Remove(_selectedModifier);
        SelectedModifier = null;
        MarkDirty();
    }

    public void AddEffect()
    {
        if (_selectedItem == null) return;
        var effect = new ItemEffectModel
        {
            EffectType = string.Empty,
            EffectId = string.Empty,
            Trigger = ItemEffectTrigger.Passive,
            Charges = 0,
            UsesPerDay = 0,
            ChancePercent = 0,
            Notes = string.Empty,
        };
        _selectedItem.Effects.Add(effect);
        SelectedEffect = effect;
        MarkDirty();
    }

    public void RemoveEffect()
    {
        if (_selectedItem == null || _selectedEffect == null) return;
        _selectedItem.Effects.Remove(_selectedEffect);
        SelectedEffect = null;
        MarkDirty();
    }

    public void AddSource()
    {
        if (_selectedItem == null) return;
        var source = new ItemSourceModel
        {
            SourceType = ItemSourceType.Other,
            SourceId = string.Empty,
            SourceName = string.Empty,
            ChancePercent = 0,
            Quantity = 1,
            Cost = 0,
            Notes = string.Empty,
        };
        _selectedItem.Sources.Add(source);
        SelectedSource = source;
        MarkDirty();
    }

    public void RemoveSource()
    {
        if (_selectedItem == null || _selectedSource == null) return;
        _selectedItem.Sources.Remove(_selectedSource);
        SelectedSource = null;
        MarkDirty();
    }

    public void AddCraftingComponent()
    {
        if (_selectedItem == null) return;
        var comp = new CraftingComponentModel
        {
            ItemId = string.Empty,
            ItemName = string.Empty,
            Quantity = 1,
        };
        _selectedItem.CraftingComponents.Add(comp);
        SelectedCraftingComponent = comp;
        MarkDirty();
    }

    public void RemoveCraftingComponent()
    {
        if (_selectedItem == null || _selectedCraftingComponent == null) return;
        _selectedItem.CraftingComponents.Remove(_selectedCraftingComponent);
        SelectedCraftingComponent = null;
        MarkDirty();
    }

    public void AddMetadataAlias()
    {
        if (_selectedItem == null) return;
        var alias = new AliasModel { Key = string.Empty, Category = string.Empty };
        _selectedItem.Metadata.Aliases.Add(alias);
        SelectedMetadataAlias = alias;
        MarkDirty();
    }

    public void RemoveMetadataAlias()
    {
        if (_selectedItem == null || _selectedMetadataAlias == null) return;
        _selectedItem.Metadata.Aliases.Remove(_selectedMetadataAlias);
        SelectedMetadataAlias = null;
        MarkDirty();
    }

    public void AddMetadataTag()
    {
        if (_selectedItem == null) return;
        var tag = new TagModel { Key = string.Empty, Category = string.Empty, Data = string.Empty };
        _selectedItem.Metadata.Tags.Add(tag);
        SelectedMetadataTag = tag;
        MarkDirty();
    }

    public void RemoveMetadataTag()
    {
        if (_selectedItem == null || _selectedMetadataTag == null) return;
        _selectedItem.Metadata.Tags.Remove(_selectedMetadataTag);
        SelectedMetadataTag = null;
        MarkDirty();
    }

    public void AddMetadataAttribute()
    {
        if (_selectedItem == null) return;
        var attr = new AttributeModel { Key = string.Empty, Value = string.Empty, Category = string.Empty, LockString = string.Empty };
        _selectedItem.Metadata.Attributes.Add(attr);
        SelectedMetadataAttribute = attr;
        MarkDirty();
    }

    public void RemoveMetadataAttribute()
    {
        if (_selectedItem == null || _selectedMetadataAttribute == null) return;
        _selectedItem.Metadata.Attributes.Remove(_selectedMetadataAttribute);
        SelectedMetadataAttribute = null;
        MarkDirty();
    }

    public void AddMetadataPermission()
    {
        if (_selectedItem == null) return;
        _selectedItem.Metadata.Permissions.Add(string.Empty);
        MarkDirty();
    }

    public void RemoveMetadataPermission()
    {
        if (_selectedItem == null || _selectedMetadataPermission == null) return;
        _selectedItem.Metadata.Permissions.Remove(_selectedMetadataPermission);
        SelectedMetadataPermission = null;
        MarkDirty();
    }

    public void OnItemTypeChanged()
    {
        if (_selectedItem == null) return;
        switch (_selectedItem.ItemType)
        {
            case ItemType.Weapon:
                _selectedItem.WeaponData ??= new WeaponDataModel();
                break;
            case ItemType.Armor:
            case ItemType.Shield:
                _selectedItem.ArmorData ??= new ArmorDataModel();
                break;
        }
        MarkDirty();
    }

    public void Validate()
    {
        if (_selectedItem == null) { ValidationMessage = string.Empty; return; }
        var errors = new System.Collections.Generic.List<string>();
        if (string.IsNullOrWhiteSpace(_selectedItem.Key))
            errors.Add("Key cannot be blank.");
        if (string.IsNullOrWhiteSpace(_selectedItem.Id))
            errors.Add("Id cannot be blank.");
        if (_selectedItem.WeaponData is { } wd)
        {
            if (wd.DamageMin > wd.DamageMax)
                errors.Add("DamageMin cannot be greater than DamageMax.");
            if (wd.HandsRequired < 1)
                errors.Add("HandsRequired cannot be less than 1.");
        }
        for (int i = 0; i < _selectedItem.Sources.Count; i++)
            if (_selectedItem.Sources[i].Quantity < 1)
                errors.Add($"Source [{i}]: Quantity cannot be less than 1.");
        for (int i = 0; i < _selectedItem.CraftingComponents.Count; i++)
            if (_selectedItem.CraftingComponents[i].Quantity < 1)
                errors.Add($"CraftingComponent [{i}]: Quantity cannot be less than 1.");
        ValidationMessage = errors.Count > 0 ? string.Join(" | ", errors) : string.Empty;
    }

    public void RefreshFilter()
    {
        FilteredItems.Clear();
        if (_project == null) return;
        var query = Items.AsEnumerable();
        if (_selectedTypeFilter.HasValue)
            query = query.Where(i => i.ItemType == _selectedTypeFilter.Value);
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var s = _searchText.Trim();
            query = query.Where(i =>
                (i.Key?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (i.Description?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (i.Notes?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        foreach (var item in query)
            FilteredItems.Add(item);
    }

    private static ItemModel CloneItem(ItemModel src) => new()
    {
        Id = src.Id,
        Key = src.Key,
        Description = src.Description,
        Notes = src.Notes,
        ItemType = src.ItemType,
        EquipmentSlot = src.EquipmentSlot,
        LevelRequirement = src.LevelRequirement,
        Encumbrance = src.Encumbrance,
        ItemLimit = src.ItemLimit,
        BaseValue = src.BaseValue,
        IsGettable = src.IsGettable,
        IsDroppable = src.IsDroppable,
        IsSellable = src.IsSellable,
        IsTradeable = src.IsTradeable,
        IsUnique = src.IsUnique,
        IsQuestItem = src.IsQuestItem,
        IsMagical = src.IsMagical,
        MagicLevel = src.MagicLevel,
        WeaponData = src.WeaponData != null ? CloneWeaponData(src.WeaponData) : null,
        ArmorData = src.ArmorData != null ? CloneArmorData(src.ArmorData) : null,
        Metadata = CloneMetadata(src.Metadata),
        Requirements = new ObservableCollection<ItemRequirementModel>(
            src.Requirements.Select(CloneRequirement)),
        Modifiers = new ObservableCollection<ItemModifierModel>(
            src.Modifiers.Select(CloneModifier)),
        Effects = new ObservableCollection<ItemEffectModel>(
            src.Effects.Select(CloneEffect)),
        Sources = new ObservableCollection<ItemSourceModel>(
            src.Sources.Select(CloneSource)),
        CraftingComponents = new ObservableCollection<CraftingComponentModel>(
            src.CraftingComponents.Select(CloneCraftingComponent)),
    };

    private static WeaponDataModel CloneWeaponData(WeaponDataModel src) => new()
    {
        WeaponType = src.WeaponType,
        DamageMin = src.DamageMin,
        DamageMax = src.DamageMax,
        StrengthRequirement = src.StrengthRequirement,
        AccuracyModifier = src.AccuracyModifier,
        BackstabAccuracyModifier = src.BackstabAccuracyModifier,
        Speed = src.Speed,
        Range = src.Range,
        HandsRequired = src.HandsRequired,
    };

    private static ArmorDataModel CloneArmorData(ArmorDataModel src) => new()
    {
        ArmorType = src.ArmorType,
        ArmorClass = src.ArmorClass,
        DamageReduction = src.DamageReduction,
        AccuracyModifier = src.AccuracyModifier,
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

    private static ItemRequirementModel CloneRequirement(ItemRequirementModel src) => new()
    {
        RequirementType = src.RequirementType,
        Operator = src.Operator,
        Value = src.Value,
    };

    private static ItemModifierModel CloneModifier(ItemModifierModel src) => new()
    {
        ModifierType = src.ModifierType,
        Value = src.Value,
        ValueType = src.ValueType,
        Notes = src.Notes,
    };

    private static ItemEffectModel CloneEffect(ItemEffectModel src) => new()
    {
        EffectType = src.EffectType,
        EffectId = src.EffectId,
        Trigger = src.Trigger,
        Charges = src.Charges,
        UsesPerDay = src.UsesPerDay,
        ChancePercent = src.ChancePercent,
        Notes = src.Notes,
    };

    private static ItemSourceModel CloneSource(ItemSourceModel src) => new()
    {
        SourceType = src.SourceType,
        SourceId = src.SourceId,
        SourceName = src.SourceName,
        ChancePercent = src.ChancePercent,
        Quantity = src.Quantity,
        Cost = src.Cost,
        Notes = src.Notes,
    };

    private static CraftingComponentModel CloneCraftingComponent(CraftingComponentModel src) => new()
    {
        ItemId = src.ItemId,
        ItemName = src.ItemName,
        Quantity = src.Quantity,
    };
}

internal class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool> _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute ?? (_ => true);
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute(parameter);
    public void Execute(object? parameter) => _execute(parameter);
}
