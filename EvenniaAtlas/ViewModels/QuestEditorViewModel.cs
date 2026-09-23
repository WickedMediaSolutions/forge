using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EvenniaAtlas.Models;

namespace EvenniaAtlas.ViewModels;

public class QuestEditorViewModel : BaseViewModel
{
    private MapProject? _project;
    private int _questCounter;

    public Action? MarkDirtyCallback { get; set; }
    private void MarkDirty() => MarkDirtyCallback?.Invoke();

    // Objective sub-selection
    private QuestObjectiveViewModel? _selectedQuestObjective;

    // Item reward sub-selection
    private QuestItemRewardViewModel? _selectedQuestItemReward;

    public ObservableCollection<QuestModel> Quests { get; } = new();
    public ObservableCollection<QuestModel> FilteredQuests { get; } = new();

    public ICommand NewQuestCommand { get; }
    public ICommand DuplicateQuestCommand { get; }
    public ICommand DeleteQuestCommand { get; }

    public ICommand ClearGiverNpcCommand { get; }
    public ICommand ClearTurnInNpcCommand { get; }

    public ICommand AddMetadataAliasCommand { get; }
    public ICommand RemoveMetadataAliasCommand { get; }
    public ICommand AddMetadataTagCommand { get; }
    public ICommand RemoveMetadataTagCommand { get; }
    public ICommand AddMetadataAttributeCommand { get; }
    public ICommand RemoveMetadataAttributeCommand { get; }
    public ICommand AddMetadataPermissionCommand { get; }
    public ICommand RemoveMetadataPermissionCommand { get; }

    // Objective commands
    public ICommand AddQuestObjectiveCommand { get; }
    public ICommand RemoveQuestObjectiveCommand { get; }
    public ICommand MoveQuestObjectiveUpCommand { get; }
    public ICommand MoveQuestObjectiveDownCommand { get; }

    // Item reward commands
    public ICommand AddQuestItemRewardCommand { get; }
    public ICommand RemoveQuestItemRewardCommand { get; }

    // Loot table reward
    public ICommand ClearRewardLootTableCommand { get; }

    // Objective and reward collections
    public ObservableCollection<QuestObjectiveViewModel> QuestObjectives { get; } = new();
    public ObservableCollection<QuestItemRewardViewModel> QuestItemRewards { get; } = new();

    public QuestEditorViewModel()
    {
        NewQuestCommand = new RelayCommand(_ => CreateQuest(), _ => _project != null);
        DuplicateQuestCommand = new RelayCommand(_ => DuplicateQuest(), _ => HasSelectedQuest);
        DeleteQuestCommand = new RelayCommand(_ => DeleteQuest(), _ => HasSelectedQuest);
        ClearGiverNpcCommand = new RelayCommand(_ => ClearGiverNpc(), _ => HasSelectedQuest);
        ClearTurnInNpcCommand = new RelayCommand(_ => ClearTurnInNpc(), _ => HasSelectedQuest);
        AddMetadataAliasCommand = new RelayCommand(_ => AddMetadataAlias(), _ => HasSelectedQuest);
        RemoveMetadataAliasCommand = new RelayCommand(_ => RemoveMetadataAlias(), _ => HasSelectedMetadataAlias);
        AddMetadataTagCommand = new RelayCommand(_ => AddMetadataTag(), _ => HasSelectedQuest);
        RemoveMetadataTagCommand = new RelayCommand(_ => RemoveMetadataTag(), _ => HasSelectedMetadataTag);
        AddMetadataAttributeCommand = new RelayCommand(_ => AddMetadataAttribute(), _ => HasSelectedQuest);
        RemoveMetadataAttributeCommand = new RelayCommand(_ => RemoveMetadataAttribute(), _ => HasSelectedMetadataAttribute);
        AddMetadataPermissionCommand = new RelayCommand(_ => AddMetadataPermission(), _ => HasSelectedQuest);
        RemoveMetadataPermissionCommand = new RelayCommand(_ => RemoveMetadataPermission(), _ => HasSelectedMetadataPermission);

        AddQuestObjectiveCommand = new RelayCommand(_ => AddQuestObjective(), _ => HasSelectedQuest);
        RemoveQuestObjectiveCommand = new RelayCommand(_ => RemoveQuestObjective(), _ => HasSelectedQuestObjective);
        MoveQuestObjectiveUpCommand = new RelayCommand(_ => MoveQuestObjectiveUp(), _ => HasSelectedQuestObjective);
        MoveQuestObjectiveDownCommand = new RelayCommand(_ => MoveQuestObjectiveDown(), _ => HasSelectedQuestObjective);

        AddQuestItemRewardCommand = new RelayCommand(_ => AddQuestItemReward(), _ => HasSelectedQuest);
        RemoveQuestItemRewardCommand = new RelayCommand(_ => RemoveQuestItemReward(), _ => HasSelectedQuestItemReward);

        ClearRewardLootTableCommand = new RelayCommand(_ => ClearRewardLootTable(), _ => HasSelectedQuest);
    }
// ---- Selection ----
    private QuestModel? _selectedQuest;
    public QuestModel? SelectedQuest
    {
        get => _selectedQuest;
        set
        {
            var oldQuest = _selectedQuest;
            if (SetField(ref _selectedQuest, value))
            {
                if (oldQuest != null) MarkDirty();
                OnPropertyChanged(nameof(HasSelectedQuest));
                SyncObjectives();
                SyncItemRewards();
                Validate();
            }
        }
    }
    public bool HasSelectedQuest => _selectedQuest != null;

    // ---- Available NPCs ----
    public IEnumerable<NpcModel> AvailableNpcs => _project?.Npcs ?? Enumerable.Empty<NpcModel>();

    // ---- Available Items / Rooms / Loot Tables ----
    public IEnumerable<ItemModel> AvailableItems => _project?.Items ?? Enumerable.Empty<ItemModel>();
    public IEnumerable<RoomModel> AvailableRooms => _project?.Rooms ?? Enumerable.Empty<RoomModel>();
    public IEnumerable<LootTableModel> AvailableLootTables => _project?.LootTables ?? Enumerable.Empty<LootTableModel>();

    // ---- Search ----
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
                RefreshFilter();
        }
    }

    // ---- Validation ----
    private string _validationMessage = string.Empty;
    public string ValidationMessage
    {
        get => _validationMessage;
        set => SetField(ref _validationMessage, value);
    }

    // ---- Objective sub-selection ----
    public QuestObjectiveViewModel? SelectedQuestObjective
    {
        get => _selectedQuestObjective;
        set
        {
            if (SetField(ref _selectedQuestObjective, value))
                OnPropertyChanged(nameof(HasSelectedQuestObjective));
        }
    }
    public bool HasSelectedQuestObjective => _selectedQuestObjective != null;

    // ---- Item reward sub-selection ----
    public QuestItemRewardViewModel? SelectedQuestItemReward
    {
        get => _selectedQuestItemReward;
        set
        {
            if (SetField(ref _selectedQuestItemReward, value))
                OnPropertyChanged(nameof(HasSelectedQuestItemReward));
        }
    }
    public bool HasSelectedQuestItemReward => _selectedQuestItemReward != null;

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
        _questCounter = project.Quests.Count > 0
            ? project.Quests.Max(q =>
            {
                var parts = q.Id.Split('_');
                return parts.Length > 0 && int.TryParse(parts.Last(), out var num) ? num : 0;
            })
            : 0;
        Quests.Clear();
        foreach (var quest in project.Quests)
            Quests.Add(quest);
        SelectedQuest = null;
        RefreshFilter();
    }

    // ---- CRUD ----
    public void CreateQuest()
    {
        if (_project == null) return;
        _questCounter++;
        var newQuest = new QuestModel
        {
            Id = _project.Id + "_quest_" + _questCounter.ToString("D4"),
            Key = "New Quest",
            Description = string.Empty,
            Notes = string.Empty,
            Metadata = new EvenniaObjectMetadata(),
            GiverNpcId = string.Empty,
            TurnInNpcId = string.Empty,
            Objectives = new ObservableCollection<QuestObjectiveModel>(),
            ExperienceReward = 0,
            CurrencyReward = 0m,
            ItemRewards = new ObservableCollection<QuestItemRewardModel>(),
            RewardLootTableId = string.Empty,
        };
        _project.Quests.Add(newQuest);
        Quests.Add(newQuest);
        SelectedQuest = newQuest;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }

    public void DeleteQuest()
    {
        if (_project == null || _selectedQuest == null) return;
        var questId = _selectedQuest.Id;
        var questKey = _selectedQuest.Key;

        var dialogueRefs = _project.Dialogues
            .Where(d => d.Nodes.Any(n => n.Responses.Any(r => r.StartsQuestId == questId || r.CompletesQuestId == questId)))
            .ToList();
        if (dialogueRefs.Count > 0)
        {
            var dialogueNames = string.Join(", ", dialogueRefs.Select(d => d.Key));
            ValidationMessage = $"Cannot delete \"{questKey}\": used by dialogue(s): {dialogueNames}.";
            return;
        }

        _project.Quests.Remove(_selectedQuest);
        Quests.Remove(_selectedQuest);
        SelectedQuest = null;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }

    public void DuplicateQuest()
    {
        if (_project == null || _selectedQuest == null) return;
        _questCounter++;
        var clone = CloneQuest(_selectedQuest);
        clone.Id = _project.Id + "_quest_" + _questCounter.ToString("D4");
        clone.Key = _selectedQuest.Key + " Copy";
        _project.Quests.Add(clone);
        Quests.Add(clone);
        SelectedQuest = clone;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }

    // ---- NPC helpers ----
    public void ClearGiverNpc()
    {
        if (_selectedQuest == null) return;
        _selectedQuest.GiverNpcId = string.Empty;
        MarkDirty();
    }

    public void ClearTurnInNpc()
    {
        if (_selectedQuest == null) return;
        _selectedQuest.TurnInNpcId = string.Empty;
        MarkDirty();
    }
// ---- Metadata helpers ----
    public void AddMetadataAlias()
    {
        if (_selectedQuest == null) return;
        var alias = new AliasModel { Key = string.Empty, Category = string.Empty };
        _selectedQuest.Metadata.Aliases.Add(alias);
        SelectedMetadataAlias = alias;
        MarkDirty();
    }

    public void RemoveMetadataAlias()
    {
        if (_selectedQuest == null || _selectedMetadataAlias == null) return;
        _selectedQuest.Metadata.Aliases.Remove(_selectedMetadataAlias);
        SelectedMetadataAlias = null;
        MarkDirty();
    }

    public void AddMetadataTag()
    {
        if (_selectedQuest == null) return;
        var tag = new TagModel { Key = string.Empty, Category = string.Empty, Data = string.Empty };
        _selectedQuest.Metadata.Tags.Add(tag);
        SelectedMetadataTag = tag;
        MarkDirty();
    }

    public void RemoveMetadataTag()
    {
        if (_selectedQuest == null || _selectedMetadataTag == null) return;
        _selectedQuest.Metadata.Tags.Remove(_selectedMetadataTag);
        SelectedMetadataTag = null;
        MarkDirty();
    }

    public void AddMetadataAttribute()
    {
        if (_selectedQuest == null) return;
        var attr = new AttributeModel
        {
            Key = string.Empty,
            Value = string.Empty,
            Category = string.Empty,
            LockString = string.Empty,
        };
        _selectedQuest.Metadata.Attributes.Add(attr);
        SelectedMetadataAttribute = attr;
        MarkDirty();
    }

    public void RemoveMetadataAttribute()
    {
        if (_selectedQuest == null || _selectedMetadataAttribute == null) return;
        _selectedQuest.Metadata.Attributes.Remove(_selectedMetadataAttribute);
        SelectedMetadataAttribute = null;
        MarkDirty();
    }

    public void AddMetadataPermission()
    {
        if (_selectedQuest == null) return;
        _selectedQuest.Metadata.Permissions.Add(string.Empty);
        MarkDirty();
    }

    public void RemoveMetadataPermission()
    {
        if (_selectedQuest == null || _selectedMetadataPermission == null) return;
        _selectedQuest.Metadata.Permissions.Remove(_selectedMetadataPermission);
        SelectedMetadataPermission = null;
        MarkDirty();
    }

    // ---- Objective sync & CRUD ----
    private void SyncObjectives()
    {
        QuestObjectives.Clear();
        SelectedQuestObjective = null;
        if (_selectedQuest != null)
        {
            foreach (var obj in _selectedQuest.Objectives)
                QuestObjectives.Add(new QuestObjectiveViewModel(obj, AvailableNpcs, AvailableItems, AvailableRooms));
        }
    }

    public void AddQuestObjective()
    {
        if (_selectedQuest == null) return;
        var obj = new QuestObjectiveModel
        {
            ObjectiveType = QuestObjectiveType.KillNpc,
            TargetId = string.Empty,
            TargetName = string.Empty,
            RequiredCount = 1,
            Description = string.Empty,
        };
        _selectedQuest.Objectives.Add(obj);
        var wrapper = new QuestObjectiveViewModel(obj, AvailableNpcs, AvailableItems, AvailableRooms);
        QuestObjectives.Add(wrapper);
        SelectedQuestObjective = wrapper;
        MarkDirty();
    }

    public void RemoveQuestObjective()
    {
        if (_selectedQuest == null || _selectedQuestObjective == null) return;
        _selectedQuest.Objectives.Remove(_selectedQuestObjective.Model);
        QuestObjectives.Remove(_selectedQuestObjective);
        SelectedQuestObjective = null;
        MarkDirty();
    }

    public void MoveQuestObjectiveUp()
    {
        if (_selectedQuest == null || _selectedQuestObjective == null) return;
        var idx = QuestObjectives.IndexOf(_selectedQuestObjective);
        if (idx <= 0) return;
        QuestObjectives.Move(idx, idx - 1);
        _selectedQuest.Objectives.Move(idx, idx - 1);
        MarkDirty();
    }

    public void MoveQuestObjectiveDown()
    {
        if (_selectedQuest == null || _selectedQuestObjective == null) return;
        var idx = QuestObjectives.IndexOf(_selectedQuestObjective);
        if (idx < 0 || idx >= QuestObjectives.Count - 1) return;
        QuestObjectives.Move(idx, idx + 1);
        _selectedQuest.Objectives.Move(idx, idx + 1);
        MarkDirty();
    }

    // ---- Item reward sync & CRUD ----
    private void SyncItemRewards()
    {
        QuestItemRewards.Clear();
        SelectedQuestItemReward = null;
        if (_selectedQuest != null)
        {
            foreach (var reward in _selectedQuest.ItemRewards)
                QuestItemRewards.Add(new QuestItemRewardViewModel(reward, AvailableItems));
        }
    }

    public void AddQuestItemReward()
    {
        if (_selectedQuest == null) return;
        var reward = new QuestItemRewardModel
        {
            ItemId = string.Empty,
            ItemName = string.Empty,
            Quantity = 1,
        };
        _selectedQuest.ItemRewards.Add(reward);
        var wrapper = new QuestItemRewardViewModel(reward, AvailableItems);
        QuestItemRewards.Add(wrapper);
        SelectedQuestItemReward = wrapper;
        MarkDirty();
    }

    public void RemoveQuestItemReward()
    {
        if (_selectedQuest == null || _selectedQuestItemReward == null) return;
        _selectedQuest.ItemRewards.Remove(_selectedQuestItemReward.Model);
        QuestItemRewards.Remove(_selectedQuestItemReward);
        SelectedQuestItemReward = null;
        MarkDirty();
    }

    // ---- Loot table reward ----
    public void ClearRewardLootTable()
    {
        if (_selectedQuest == null) return;
        _selectedQuest.RewardLootTableId = string.Empty;
        MarkDirty();
    }

    // ---- Validation ----
    public void Validate()
    {
        if (_selectedQuest == null) { ValidationMessage = string.Empty; return; }
        var errors = new System.Collections.Generic.List<string>();
        if (string.IsNullOrWhiteSpace(_selectedQuest.Id))
            errors.Add("Id cannot be blank.");
        if (string.IsNullOrWhiteSpace(_selectedQuest.Key))
            errors.Add("Key cannot be blank.");
        ValidationMessage = errors.Count > 0 ? string.Join(" | ", errors) : string.Empty;
    }

    // ---- Filter ----
    public void RefreshFilter()
    {
        FilteredQuests.Clear();
        if (_project == null) return;
        var query = Quests.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var s = _searchText.Trim();
            query = query.Where(q =>
                (q.Key?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (q.Description?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (q.Notes?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        foreach (var quest in query)
            FilteredQuests.Add(quest);
    }

    // ---- Clone support ----
    private static QuestModel CloneQuest(QuestModel src) => new()
    {
        Id = src.Id,
        Key = src.Key,
        Description = src.Description,
        Notes = src.Notes,
        GiverNpcId = src.GiverNpcId,
        TurnInNpcId = src.TurnInNpcId,
        ExperienceReward = src.ExperienceReward,
        CurrencyReward = src.CurrencyReward,
        RewardLootTableId = src.RewardLootTableId,
        Metadata = CloneMetadata(src.Metadata),
        Objectives = new ObservableCollection<QuestObjectiveModel>(
            src.Objectives.Select(CloneObjective)),
        ItemRewards = new ObservableCollection<QuestItemRewardModel>(
            src.ItemRewards.Select(CloneItemReward)),
    };

    private static QuestObjectiveModel CloneObjective(QuestObjectiveModel src) => new()
    {
        ObjectiveType = src.ObjectiveType,
        TargetId = src.TargetId,
        TargetName = src.TargetName,
        RequiredCount = src.RequiredCount,
        Description = src.Description,
    };

    private static QuestItemRewardModel CloneItemReward(QuestItemRewardModel src) => new()
    {
        ItemId = src.ItemId,
        ItemName = src.ItemName,
        Quantity = src.Quantity,
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
/// <summary>ViewModel wrapper for QuestObjectiveModel.</summary>
public class QuestObjectiveViewModel : BaseViewModel
{
    private readonly QuestObjectiveModel _model;
    private readonly IEnumerable<NpcModel> _availableNpcs;
    private readonly IEnumerable<ItemModel> _availableItems;
    private readonly IEnumerable<RoomModel> _availableRooms;

    public QuestObjectiveModel Model => _model;

    public IEnumerable<QuestObjectiveType> ObjectiveTypes { get; } =
        Enum.GetValues<QuestObjectiveType>();

    public IEnumerable<NpcModel> AvailableNpcs => _availableNpcs;
    public IEnumerable<ItemModel> AvailableItems => _availableItems;
    public IEnumerable<RoomModel> AvailableRooms => _availableRooms;

    public QuestObjectiveType ObjectiveType
    {
        get => _model.ObjectiveType;
        set
        {
            if (_model.ObjectiveType == value) return;
            _model.ObjectiveType = value;
            _model.TargetId = string.Empty;
            _model.TargetName = string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TargetId));
            OnPropertyChanged(nameof(TargetName));
            OnPropertyChanged(nameof(IsKillNpc));
            OnPropertyChanged(nameof(IsCollectItem));
            OnPropertyChanged(nameof(IsVisitRoom));
        }
    }

    public string TargetId
    {
        get => _model.TargetId;
        set
        {
            if (_model.TargetId == value) return;
            _model.TargetId = value;
            _model.TargetName = ResolveTargetName();
            OnPropertyChanged();
            OnPropertyChanged(nameof(TargetName));
        }
    }

    public string TargetName => _model.TargetName;

    public int RequiredCount
    {
        get => _model.RequiredCount;
        set
        {
            if (_model.RequiredCount == value) return;
            _model.RequiredCount = value;
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get => _model.Description;
        set
        {
            if (_model.Description == value) return;
            _model.Description = value;
            OnPropertyChanged();
        }
    }

    public bool IsKillNpc => _model.ObjectiveType == QuestObjectiveType.KillNpc;
    public bool IsCollectItem => _model.ObjectiveType == QuestObjectiveType.CollectItem;
    public bool IsVisitRoom => _model.ObjectiveType == QuestObjectiveType.VisitRoom;

    private string ResolveTargetName()
    {
        return _model.ObjectiveType switch
        {
            QuestObjectiveType.KillNpc =>
                _availableNpcs.FirstOrDefault(n => n.Id == _model.TargetId)?.Key ?? string.Empty,
            QuestObjectiveType.CollectItem =>
                _availableItems.FirstOrDefault(i => i.Id == _model.TargetId)?.Key ?? string.Empty,
            QuestObjectiveType.VisitRoom =>
                _availableRooms.FirstOrDefault(r => r.Id == _model.TargetId)?.Title ?? string.Empty,
            _ => string.Empty,
        };
    }

    public QuestObjectiveViewModel(
        QuestObjectiveModel model,
        IEnumerable<NpcModel> availableNpcs,
        IEnumerable<ItemModel> availableItems,
        IEnumerable<RoomModel> availableRooms)
    {
        _model = model;
        _availableNpcs = availableNpcs;
        _availableItems = availableItems;
        _availableRooms = availableRooms;
    }
}
/// <summary>ViewModel wrapper for QuestItemRewardModel.</summary>
public class QuestItemRewardViewModel : BaseViewModel
{
    private readonly QuestItemRewardModel _model;
    private readonly IEnumerable<ItemModel> _availableItems;

    public QuestItemRewardModel Model => _model;

    public IEnumerable<ItemModel> AvailableItems => _availableItems;

    public string ItemId
    {
        get => _model.ItemId;
        set
        {
            if (_model.ItemId == value) return;
            _model.ItemId = value;
            _model.ItemName = _availableItems.FirstOrDefault(i => i.Id == value)?.Key ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ItemName));
        }
    }

    public string ItemName => _model.ItemName;

    public int Quantity
    {
        get => _model.Quantity;
        set
        {
            if (_model.Quantity == value) return;
            _model.Quantity = value;
            OnPropertyChanged();
        }
    }

    public QuestItemRewardViewModel(QuestItemRewardModel model, IEnumerable<ItemModel> availableItems)
    {
        _model = model;
        _availableItems = availableItems;
    }
}
