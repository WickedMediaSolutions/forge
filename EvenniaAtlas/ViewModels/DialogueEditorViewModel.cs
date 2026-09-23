using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using EvenniaAtlas.Models;

namespace EvenniaAtlas.ViewModels;

public class DialogueEditorViewModel : BaseViewModel
{
    private MapProject? _project;
    private int _dialogueCounter;
    private string _validationMessage = string.Empty;

    public Action? MarkDirtyCallback { get; set; }
    private void MarkDirty() => MarkDirtyCallback?.Invoke();

    // Sub-selections for metadata editing
    private AliasModel? _selectedMetadataAlias;
    private TagModel? _selectedMetadataTag;
    private AttributeModel? _selectedMetadataAttribute;
    private string? _selectedMetadataPermission;

    // Node sub-selection
    private DialogueNodeViewModel? _selectedDialogueNode;

    public ObservableCollection<DialogueModel> Dialogues { get; } = new();
    public ObservableCollection<DialogueModel> FilteredDialogues { get; } = new();

    // Node UI collection
    public ObservableCollection<DialogueNodeViewModel> DialogueNodes { get; } = new();

    public ICommand NewDialogueCommand { get; }
    public ICommand DuplicateDialogueCommand { get; }
    public ICommand DeleteDialogueCommand { get; }

    public ICommand AddDialogueNodeCommand { get; }
    public ICommand RemoveDialogueNodeCommand { get; }
    public ICommand MoveDialogueNodeUpCommand { get; }
    public ICommand MoveDialogueNodeDownCommand { get; }
    public ICommand ClearStartNodeCommand { get; }
    public ICommand ClearSpeakerNpcCommand { get; }

    public ICommand AddDialogueResponseCommand { get; }
    public ICommand RemoveDialogueResponseCommand { get; }
    public ICommand MoveDialogueResponseUpCommand { get; }
    public ICommand MoveDialogueResponseDownCommand { get; }
    public ICommand ClearResponseNextNodeCommand { get; }
    public ICommand ClearResponseStartsQuestCommand { get; }
    public ICommand ClearResponseCompletesQuestCommand { get; }

    public ICommand AddMetadataAliasCommand { get; }
    public ICommand RemoveMetadataAliasCommand { get; }
    public ICommand AddMetadataTagCommand { get; }
    public ICommand RemoveMetadataTagCommand { get; }
    public ICommand AddMetadataAttributeCommand { get; }
    public ICommand RemoveMetadataAttributeCommand { get; }
    public ICommand AddMetadataPermissionCommand { get; }
    public ICommand RemoveMetadataPermissionCommand { get; }

    public DialogueEditorViewModel()
    {
        NewDialogueCommand = new RelayCommand(_ => CreateDialogue(), _ => _project != null);
        DuplicateDialogueCommand = new RelayCommand(_ => DuplicateDialogue(), _ => HasSelectedDialogue);
        DeleteDialogueCommand = new RelayCommand(_ => DeleteDialogue(), _ => HasSelectedDialogue);
        AddDialogueNodeCommand = new RelayCommand(_ => AddDialogueNode(), _ => HasSelectedDialogue);
        RemoveDialogueNodeCommand = new RelayCommand(_ => RemoveDialogueNode(), _ => HasSelectedDialogueNode);
        MoveDialogueNodeUpCommand = new RelayCommand(_ => MoveDialogueNodeUp(), _ => HasSelectedDialogueNode);
        MoveDialogueNodeDownCommand = new RelayCommand(_ => MoveDialogueNodeDown(), _ => HasSelectedDialogueNode);
        ClearStartNodeCommand = new RelayCommand(_ => ClearStartNode(), _ => HasSelectedDialogue);
        ClearSpeakerNpcCommand = new RelayCommand(_ => ClearSpeakerNpc(), _ => HasSelectedDialogueNode);

        AddDialogueResponseCommand = new RelayCommand(_ => AddDialogueResponse(), _ => HasSelectedDialogueNode);
        RemoveDialogueResponseCommand = new RelayCommand(_ => RemoveDialogueResponse(),
            _ => HasSelectedDialogueNode && SelectedDialogueNode?.HasSelectedResponse == true);
        MoveDialogueResponseUpCommand = new RelayCommand(_ => MoveDialogueResponseUp(),
            _ => HasSelectedDialogueNode && SelectedDialogueNode?.HasSelectedResponse == true);
        MoveDialogueResponseDownCommand = new RelayCommand(_ => MoveDialogueResponseDown(),
            _ => HasSelectedDialogueNode && SelectedDialogueNode?.HasSelectedResponse == true);
        ClearResponseNextNodeCommand = new RelayCommand(_ => ClearResponseNextNode(),
            _ => HasSelectedDialogueNode && SelectedDialogueNode?.HasSelectedResponse == true);
        ClearResponseStartsQuestCommand = new RelayCommand(_ => ClearResponseStartsQuest(),
            _ => HasSelectedDialogueNode && SelectedDialogueNode?.HasSelectedResponse == true);
        ClearResponseCompletesQuestCommand = new RelayCommand(_ => ClearResponseCompletesQuest(),
            _ => HasSelectedDialogueNode && SelectedDialogueNode?.HasSelectedResponse == true);

        AddMetadataAliasCommand = new RelayCommand(_ => AddMetadataAlias(), _ => HasSelectedDialogue);
        RemoveMetadataAliasCommand = new RelayCommand(_ => RemoveMetadataAlias(), _ => HasSelectedMetadataAlias);
        AddMetadataTagCommand = new RelayCommand(_ => AddMetadataTag(), _ => HasSelectedDialogue);
        RemoveMetadataTagCommand = new RelayCommand(_ => RemoveMetadataTag(), _ => HasSelectedMetadataTag);
        AddMetadataAttributeCommand = new RelayCommand(_ => AddMetadataAttribute(), _ => HasSelectedDialogue);
        RemoveMetadataAttributeCommand = new RelayCommand(_ => RemoveMetadataAttribute(), _ => HasSelectedMetadataAttribute);
        AddMetadataPermissionCommand = new RelayCommand(_ => AddMetadataPermission(), _ => HasSelectedDialogue);
        RemoveMetadataPermissionCommand = new RelayCommand(_ => RemoveMetadataPermission(), _ => HasSelectedMetadataPermission);
    }
// ---- Selection ----
    private DialogueModel? _selectedDialogue;
    public DialogueModel? SelectedDialogue
    {
        get => _selectedDialogue;
        set
        {
            var oldDialogue = _selectedDialogue;
            if (SetField(ref _selectedDialogue, value))
            {
                if (oldDialogue != null) MarkDirty();
                OnPropertyChanged(nameof(HasSelectedDialogue));
                SelectedMetadataAlias = null;
                SelectedMetadataTag = null;
                SelectedMetadataAttribute = null;
                SelectedMetadataPermission = null;
                SyncNodes();
                Validate();
            }
        }
    }
    public bool HasSelectedDialogue => _selectedDialogue != null;

    // ---- Node selection ----
    public DialogueNodeViewModel? SelectedDialogueNode
    {
        get => _selectedDialogueNode;
        set
        {
            if (SetField(ref _selectedDialogueNode, value))
                OnPropertyChanged(nameof(HasSelectedDialogueNode));
        }
    }
    public bool HasSelectedDialogueNode => _selectedDialogueNode != null;

    // ---- Available collections ----
    public IEnumerable<NpcModel> AvailableNpcs
        => _project?.Npcs ?? Enumerable.Empty<NpcModel>();

    public IEnumerable<QuestModel> AvailableQuests
        => _project?.Quests ?? Enumerable.Empty<QuestModel>();

    // ---- Metadata sub-selections ----
    public AliasModel? SelectedMetadataAlias
    {
        get => _selectedMetadataAlias;
        set { if (SetField(ref _selectedMetadataAlias, value)) OnPropertyChanged(nameof(HasSelectedMetadataAlias)); }
    }
    public bool HasSelectedMetadataAlias => _selectedMetadataAlias != null;

    public TagModel? SelectedMetadataTag
    {
        get => _selectedMetadataTag;
        set { if (SetField(ref _selectedMetadataTag, value)) OnPropertyChanged(nameof(HasSelectedMetadataTag)); }
    }
    public bool HasSelectedMetadataTag => _selectedMetadataTag != null;

    public AttributeModel? SelectedMetadataAttribute
    {
        get => _selectedMetadataAttribute;
        set { if (SetField(ref _selectedMetadataAttribute, value)) OnPropertyChanged(nameof(HasSelectedMetadataAttribute)); }
    }
    public bool HasSelectedMetadataAttribute => _selectedMetadataAttribute != null;

    public string? SelectedMetadataPermission
    {
        get => _selectedMetadataPermission;
        set { if (SetField(ref _selectedMetadataPermission, value)) OnPropertyChanged(nameof(HasSelectedMetadataPermission)); }
    }
    public bool HasSelectedMetadataPermission => _selectedMetadataPermission != null;

    // ---- Search / Filter ----
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) RefreshFilter(); }
    }

    // ---- Validation ----
    public string ValidationMessage
    {
        get => _validationMessage;
        set => SetField(ref _validationMessage, value);
    }

    // ---- Project wiring ----
    public void SetProject(MapProject project)
    {
        _project = project;
        _dialogueCounter = project.Dialogues.Count > 0
            ? project.Dialogues.Max(d =>
            {
                var parts = d.Id.Split('_');
                return parts.Length > 0 && int.TryParse(parts.Last(), out var num) ? num : 0;
            })
            : 0;
        Dialogues.Clear();
        foreach (var dialogue in project.Dialogues)
            Dialogues.Add(dialogue);
        SelectedDialogue = null;
        RefreshFilter();
    }

    // ---- CRUD ----
    private void CreateDialogue()
    {
        if (_project == null) return;
        _dialogueCounter++;
        var newId = $"{_project.Id}_dialogue_{_dialogueCounter:D4}";
        var newDialogue = new DialogueModel
        {
            Id = newId,
            Key = "New Dialogue",
            Description = string.Empty,
            Notes = string.Empty,
            Metadata = new EvenniaObjectMetadata(),
            StartNodeId = string.Empty,
            Nodes = new ObservableCollection<DialogueNodeModel>(),
        };
        _project.Dialogues.Add(newDialogue);
        Dialogues.Add(newDialogue);
        SelectedDialogue = newDialogue;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }

    private void DeleteDialogue()
    {
        if (_project == null || _selectedDialogue == null) return;
        var dialogueId = _selectedDialogue.Id;
        var dialogueKey = _selectedDialogue.Key;

        var npcRefs = _project.Npcs
            .Where(n => n.DialogueId == dialogueId)
            .ToList();
        if (npcRefs.Count > 0)
        {
            var npcNames = string.Join(", ", npcRefs.Select(n => n.Key));
            ValidationMessage = $"Cannot delete \"{dialogueKey}\": used by NPC(s): {npcNames}.";
            return;
        }

        _project.Dialogues.Remove(_selectedDialogue);
        Dialogues.Remove(_selectedDialogue);
        SelectedDialogue = null;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }

    private void DuplicateDialogue()
    {
        if (_project == null || _selectedDialogue == null) return;
        _dialogueCounter++;
        var newId = $"{_project.Id}_dialogue_{_dialogueCounter:D4}";
        var clone = new DialogueModel
        {
            Id = newId,
            Key = _selectedDialogue.Key + " Copy",
            Description = _selectedDialogue.Description,
            Notes = _selectedDialogue.Notes,
            Metadata = CloneMetadata(_selectedDialogue.Metadata),
            StartNodeId = _selectedDialogue.StartNodeId,
            Nodes = new ObservableCollection<DialogueNodeModel>(
                _selectedDialogue.Nodes.Select(CloneNode)),
        };
        _project.Dialogues.Add(clone);
        Dialogues.Add(clone);
        SelectedDialogue = clone;
        ValidationMessage = string.Empty;
        MarkDirty();
        RefreshFilter();
    }
// ---- Cloning helpers ----
    private static DialogueNodeModel CloneNode(DialogueNodeModel src) => new()
    {
        Id = src.Id,
        Text = src.Text,
        SpeakerNpcId = src.SpeakerNpcId,
        SpeakerName = src.SpeakerName,
        Responses = new ObservableCollection<DialogueResponseModel>(
            src.Responses.Select(CloneResponse)),
    };

    private static DialogueResponseModel CloneResponse(DialogueResponseModel src) => new()
    {
        Text = src.Text,
        NextNodeId = src.NextNodeId,
        StartsQuestId = src.StartsQuestId,
        CompletesQuestId = src.CompletesQuestId,
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

    // ---- Node sync & CRUD ----
    private void SyncNodes()
    {
        DialogueNodes.Clear();
        SelectedDialogueNode = null;
        if (_selectedDialogue != null)
        {
            foreach (var node in _selectedDialogue.Nodes)
                DialogueNodes.Add(new DialogueNodeViewModel(
                    node, AvailableNpcs, _selectedDialogue.Nodes, AvailableQuests));
        }
    }

    private string GenerateLocalNodeId()
    {
        if (_selectedDialogue == null) return "node_0001";
        int maxNum = 0;
        foreach (var node in _selectedDialogue.Nodes)
        {
            if (node.Id.StartsWith("node_") &&
                int.TryParse(node.Id.Substring(5), out var num) && num > maxNum)
                maxNum = num;
        }
        return $"node_{(maxNum + 1):D4}";
    }

    private void AddDialogueNode()
    {
        if (_selectedDialogue == null) return;
        var newId = GenerateLocalNodeId();
        var node = new DialogueNodeModel
        {
            Id = newId,
            Text = string.Empty,
            SpeakerNpcId = string.Empty,
            SpeakerName = string.Empty,
            Responses = new ObservableCollection<DialogueResponseModel>(),
        };
        _selectedDialogue.Nodes.Add(node);
        var wrapper = new DialogueNodeViewModel(
            node, AvailableNpcs, _selectedDialogue.Nodes, AvailableQuests);
        DialogueNodes.Add(wrapper);
        SelectedDialogueNode = wrapper;
        if (string.IsNullOrEmpty(_selectedDialogue.StartNodeId))
            _selectedDialogue.StartNodeId = newId;
        MarkDirty();
    }

    private void RemoveDialogueNode()
    {
        if (_selectedDialogue == null || _selectedDialogueNode == null) return;
        var nodeId = _selectedDialogueNode.Id;

        if (_selectedDialogue.StartNodeId == nodeId)
        {
            ValidationMessage = $"Cannot delete node \"{nodeId}\": it is the dialogue Start Node.";
            return;
        }

        foreach (var n in _selectedDialogue.Nodes)
        {
            foreach (var r in n.Responses)
            {
                if (r.NextNodeId == nodeId)
                {
                    ValidationMessage =
                        $"Cannot delete node \"{nodeId}\": referenced by one or more responses.";
                    return;
                }
            }
        }

        _selectedDialogue.Nodes.Remove(_selectedDialogueNode.Model);
        DialogueNodes.Remove(_selectedDialogueNode);
        SelectedDialogueNode = DialogueNodes.FirstOrDefault();
        MarkDirty();
    }

    private void MoveDialogueNodeUp()
    {
        if (_selectedDialogue == null || _selectedDialogueNode == null) return;
        var idx = DialogueNodes.IndexOf(_selectedDialogueNode);
        if (idx <= 0) return;
        _selectedDialogue.Nodes.Move(idx, idx - 1);
        DialogueNodes.Move(idx, idx - 1);
        MarkDirty();
    }

    private void MoveDialogueNodeDown()
    {
        if (_selectedDialogue == null || _selectedDialogueNode == null) return;
        var idx = DialogueNodes.IndexOf(_selectedDialogueNode);
        if (idx < 0 || idx >= DialogueNodes.Count - 1) return;
        _selectedDialogue.Nodes.Move(idx, idx + 1);
        DialogueNodes.Move(idx, idx + 1);
        MarkDirty();
    }

    private void ClearStartNode()
    {
        if (_selectedDialogue == null) return;
        _selectedDialogue.StartNodeId = string.Empty;
        MarkDirty();
    }

    private void ClearSpeakerNpc()
    {
        if (_selectedDialogueNode == null) return;
        _selectedDialogueNode.SpeakerNpcId = string.Empty;
        MarkDirty();
    }

    // ---- Response CRUD ----
    private void AddDialogueResponse()
    {
        if (_selectedDialogueNode == null) return;
        var resp = new DialogueResponseModel
        {
            Text = string.Empty,
            NextNodeId = string.Empty,
            StartsQuestId = string.Empty,
            CompletesQuestId = string.Empty,
        };
        _selectedDialogueNode.Model.Responses.Add(resp);
        var wrappedResp = _selectedDialogueNode.WrapResponse(resp);
        _selectedDialogueNode.SelectedResponse = wrappedResp;
        MarkDirty();
    }

    private void RemoveDialogueResponse()
    {
        if (_selectedDialogueNode?.SelectedResponse == null) return;
        var selResp = _selectedDialogueNode.SelectedResponse;
        _selectedDialogueNode.Model.Responses.Remove(selResp.Model);
        _selectedDialogueNode.Responses.Remove(selResp);
        _selectedDialogueNode.SelectedResponse = _selectedDialogueNode.Responses.FirstOrDefault();
        MarkDirty();
    }

    private void MoveDialogueResponseUp()
    {
        if (_selectedDialogueNode?.SelectedResponse == null) return;
        var idx = _selectedDialogueNode.Responses.IndexOf(_selectedDialogueNode.SelectedResponse);
        if (idx <= 0) return;
        _selectedDialogueNode.Model.Responses.Move(idx, idx - 1);
        _selectedDialogueNode.Responses.Move(idx, idx - 1);
        MarkDirty();
    }

    private void MoveDialogueResponseDown()
    {
        if (_selectedDialogueNode?.SelectedResponse == null) return;
        var idx = _selectedDialogueNode.Responses.IndexOf(_selectedDialogueNode.SelectedResponse);
        if (idx < 0 || idx >= _selectedDialogueNode.Responses.Count - 1) return;
        _selectedDialogueNode.Model.Responses.Move(idx, idx + 1);
        _selectedDialogueNode.Responses.Move(idx, idx + 1);
        MarkDirty();
    }

    private void ClearResponseNextNode()
    {
        if (_selectedDialogueNode?.SelectedResponse == null) return;
        _selectedDialogueNode.SelectedResponse.NextNodeId = string.Empty;
        MarkDirty();
    }

    private void ClearResponseStartsQuest()
    {
        if (_selectedDialogueNode?.SelectedResponse == null) return;
        _selectedDialogueNode.SelectedResponse.StartsQuestId = string.Empty;
        MarkDirty();
    }

    private void ClearResponseCompletesQuest()
    {
        if (_selectedDialogueNode?.SelectedResponse == null) return;
        _selectedDialogueNode.SelectedResponse.CompletesQuestId = string.Empty;
        MarkDirty();
    }

    // ---- Metadata commands ----
    private void AddMetadataAlias()
    {
        if (_selectedDialogue == null) return;
        _selectedDialogue.Metadata.Aliases.Add(new AliasModel());
        SelectedMetadataAlias = _selectedDialogue.Metadata.Aliases.Last();
        MarkDirty();
    }

    private void RemoveMetadataAlias()
    {
        if (_selectedDialogue == null || _selectedMetadataAlias == null) return;
        _selectedDialogue.Metadata.Aliases.Remove(_selectedMetadataAlias);
        SelectedMetadataAlias = null;
        MarkDirty();
    }

    private void AddMetadataTag()
    {
        if (_selectedDialogue == null) return;
        _selectedDialogue.Metadata.Tags.Add(new TagModel());
        SelectedMetadataTag = _selectedDialogue.Metadata.Tags.Last();
        MarkDirty();
    }

    private void RemoveMetadataTag()
    {
        if (_selectedDialogue == null || _selectedMetadataTag == null) return;
        _selectedDialogue.Metadata.Tags.Remove(_selectedMetadataTag);
        SelectedMetadataTag = null;
        MarkDirty();
    }

    private void AddMetadataAttribute()
    {
        if (_selectedDialogue == null) return;
        _selectedDialogue.Metadata.Attributes.Add(new AttributeModel());
        SelectedMetadataAttribute = _selectedDialogue.Metadata.Attributes.Last();
        MarkDirty();
    }

    private void RemoveMetadataAttribute()
    {
        if (_selectedDialogue == null || _selectedMetadataAttribute == null) return;
        _selectedDialogue.Metadata.Attributes.Remove(_selectedMetadataAttribute);
        SelectedMetadataAttribute = null;
        MarkDirty();
    }

    private void AddMetadataPermission()
    {
        if (_selectedDialogue == null) return;
        _selectedDialogue.Metadata.Permissions.Add(string.Empty);
        SelectedMetadataPermission = _selectedDialogue.Metadata.Permissions.Last();
        MarkDirty();
    }

    private void RemoveMetadataPermission()
    {
        if (_selectedDialogue == null || _selectedMetadataPermission == null) return;
        _selectedDialogue.Metadata.Permissions.Remove(_selectedMetadataPermission);
        SelectedMetadataPermission = null;
        MarkDirty();
    }

    // ---- Validation ----
    private void Validate()
    {
        if (_selectedDialogue == null) { ValidationMessage = string.Empty; return; }
        var errors = new System.Collections.Generic.List<string>();
        if (string.IsNullOrWhiteSpace(_selectedDialogue.Id))
            errors.Add("Id cannot be blank.");
        if (string.IsNullOrWhiteSpace(_selectedDialogue.Key))
            errors.Add("Key cannot be blank.");
        ValidationMessage = errors.Count > 0 ? string.Join(" | ", errors) : string.Empty;
    }

    // ---- Filter ----
    private void RefreshFilter()
    {
        FilteredDialogues.Clear();
        if (_project == null) return;
        var query = Dialogues.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var term = _searchText.Trim();
            query = query.Where(d =>
                (d.Key?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (d.Notes?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        foreach (var dialogue in query)
            FilteredDialogues.Add(dialogue);
    }
}
/// <summary>ViewModel wrapper for DialogueResponseModel.</summary>
public class DialogueResponseViewModel : BaseViewModel
{
    private readonly DialogueResponseModel _model;
    private readonly IEnumerable<DialogueNodeModel> _availableNodes;
    private readonly IEnumerable<QuestModel> _availableQuests;

    public DialogueResponseModel Model => _model;
    public IEnumerable<DialogueNodeModel> AvailableNodes => _availableNodes;
    public IEnumerable<QuestModel> AvailableQuests => _availableQuests;

    public string Text
    {
        get => _model.Text;
        set { _model.Text = value; OnPropertyChanged(); }
    }

    public string NextNodeId
    {
        get => _model.NextNodeId;
        set { _model.NextNodeId = value; OnPropertyChanged(); }
    }

    public string StartsQuestId
    {
        get => _model.StartsQuestId;
        set { _model.StartsQuestId = value; OnPropertyChanged(); }
    }

    public string CompletesQuestId
    {
        get => _model.CompletesQuestId;
        set { _model.CompletesQuestId = value; OnPropertyChanged(); }
    }

    public DialogueResponseViewModel(
        DialogueResponseModel model,
        IEnumerable<DialogueNodeModel> availableNodes,
        IEnumerable<QuestModel> availableQuests)
    {
        _model = model;
        _availableNodes = availableNodes;
        _availableQuests = availableQuests;
    }
}

/// <summary>ViewModel wrapper for DialogueNodeModel.</summary>
public class DialogueNodeViewModel : BaseViewModel
{
    private readonly DialogueNodeModel _model;
    private readonly IEnumerable<NpcModel> _availableNpcs;
    private readonly IEnumerable<DialogueNodeModel> _dialogueNodes;
    private readonly IEnumerable<QuestModel> _availableQuests;

    public DialogueNodeModel Model => _model;
    public IEnumerable<NpcModel> AvailableNpcs => _availableNpcs;

    public string Id
    {
        get => _model.Id;
        set { _model.Id = value; OnPropertyChanged(); }
    }

    public string Text
    {
        get => _model.Text;
        set { _model.Text = value; OnPropertyChanged(); }
    }

    public string SpeakerNpcId
    {
        get => _model.SpeakerNpcId;
        set
        {
            if (_model.SpeakerNpcId == value) return;
            _model.SpeakerNpcId = value;
            _model.SpeakerName = string.IsNullOrEmpty(value)
                ? string.Empty
                : _availableNpcs.FirstOrDefault(n => n.Id == value)?.Key ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SpeakerName));
        }
    }

    public string SpeakerName => _model.SpeakerName;

    public ObservableCollection<DialogueResponseViewModel> Responses { get; } = new();

    private DialogueResponseViewModel? _selectedResponse;
    public DialogueResponseViewModel? SelectedResponse
    {
        get => _selectedResponse;
        set
        {
            if (SetField(ref _selectedResponse, value))
                OnPropertyChanged(nameof(HasSelectedResponse));
        }
    }
    public bool HasSelectedResponse => _selectedResponse != null;

    public void WrapResponses()
    {
        Responses.Clear();
        foreach (var r in _model.Responses)
            Responses.Add(new DialogueResponseViewModel(r, _dialogueNodes, _availableQuests));
    }

    public DialogueResponseViewModel WrapResponse(DialogueResponseModel responseModel)
    {
        var vm = new DialogueResponseViewModel(responseModel, _dialogueNodes, _availableQuests);
        Responses.Add(vm);
        return vm;
    }

    public DialogueNodeViewModel(
        DialogueNodeModel model,
        IEnumerable<NpcModel> availableNpcs,
        IEnumerable<DialogueNodeModel> dialogueNodes,
        IEnumerable<QuestModel> availableQuests)
    {
        _model = model;
        _availableNpcs = availableNpcs;
        _dialogueNodes = dialogueNodes;
        _availableQuests = availableQuests;
        WrapResponses();
    }
}
