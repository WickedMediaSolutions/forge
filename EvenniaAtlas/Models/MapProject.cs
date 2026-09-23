namespace EvenniaAtlas.Models;

public class MapProject
{
    public int Version { get; set; } = 2;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DefaultRoomTitle { get; set; } = string.Empty;
    public List<RoomModel> Rooms { get; set; } = new();
    public List<ConnectionModel> Connections { get; set; } = new();
    public List<ItemModel> Items { get; set; } = new();
    public List<NpcModel> Npcs { get; set; } = new();
    public List<SpawnModel> Spawns { get; set; } = new();
    public List<ShopModel> Shops { get; set; } = new();
    public List<LootTableModel> LootTables { get; set; } = new();
    public List<QuestModel> Quests { get; set; } = new();
    public List<DialogueModel> Dialogues { get; set; } = new();
    public List<GameDataEntryModel> DamageTypes { get; set; } = new();
    public List<GameDataEntryModel> Factions { get; set; } = new();
    public List<GameDataEntryModel> Professions { get; set; } = new();
    public List<GameDataEntryModel> Species { get; set; } = new();
    public List<GameDataEntryModel> Alignments { get; set; } = new();
}