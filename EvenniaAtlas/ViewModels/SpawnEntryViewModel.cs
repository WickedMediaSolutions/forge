using EvenniaAtlas.Models;

namespace EvenniaAtlas.ViewModels;

public class SpawnEntryViewModel : BaseViewModel
{
    private readonly IEnumerable<ItemModel> _availableItems;
    private readonly IEnumerable<NpcModel> _availableNpcs;
    private readonly Action _markDirty;

    public SpawnModel Model { get; }

    public string Id => Model.Id;
    public string RoomId => Model.RoomId;
    public EntityType EntityType => Model.EntityType;

    public string EntityName
    {
        get
        {
            if (Model.EntityType == EntityType.Item)
            {
                var item = _availableItems.FirstOrDefault(i => i.Id == Model.EntityId);
                return item?.Key ?? string.Empty;
            }
            if (Model.EntityType == EntityType.Npc)
            {
                var npc = _availableNpcs.FirstOrDefault(n => n.Id == Model.EntityId);
                return npc?.Key ?? string.Empty;
            }
            return string.Empty;
        }
    }

    public string EntityId
    {
        get => Model.EntityId;
        set
        {
            if (Model.EntityId == value) return;
            Model.EntityId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EntityName));
            _markDirty();
        }
    }

    public int Quantity
    {
        get => Model.Quantity;
        set
        {
            if (Model.Quantity == value) return;
            Model.Quantity = value;
            OnPropertyChanged();
            _markDirty();
        }
    }

    public double RespawnSeconds
    {
        get => Model.RespawnSeconds;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (Model.RespawnSeconds == value) return;
            Model.RespawnSeconds = value;
            OnPropertyChanged();
            _markDirty();
        }
    }

    public bool Enabled
    {
        get => Model.Enabled;
        set
        {
            if (Model.Enabled == value) return;
            Model.Enabled = value;
            OnPropertyChanged();
            _markDirty();
        }
    }

    public SpawnEntryViewModel(
        SpawnModel model,
        IEnumerable<ItemModel> availableItems,
        IEnumerable<NpcModel> availableNpcs,
        Action? markDirty = null)
    {
        Model = model;
        _availableItems = availableItems;
        _availableNpcs = availableNpcs;
        _markDirty = markDirty ?? (() => { });
    }
}