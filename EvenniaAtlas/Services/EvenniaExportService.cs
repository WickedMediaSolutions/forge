using System.Text.Json;
using System.IO;
using EvenniaAtlas.Models;

namespace EvenniaAtlas.Services;

public class EvenniaExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Export(string filePath, MapProject project)
    {
        var exportData = new EvenniaExportData
        {
            AreaId = project.Id,
            AreaName = project.Name,
            Version = project.Version,
            Rooms = project.Rooms.Select(r => new EvenniaRoom
            {
                // Atlas fields
                Id = r.Id,
                Title = r.Title,
                RoomType = r.RoomType,
                Description = r.Description,
                X = r.X,
                Y = r.Y,
                Z = r.Z,
                Tags = r.Tags,
                Notes = r.Notes,

                // Evennia-native fields
                TypeclassPath = r.TypeclassPath,
                Aliases = r.Aliases.Select(a => new EvenniaAlias { Key = a.Key, Category = a.Category }).ToList(),
                EvenniaTags = r.EvenniaTags.Select(t => new EvenniaTag { Key = t.Key, Category = t.Category, Data = t.Data }).ToList(),
                Attributes = r.Attributes.Select(a => new EvenniaAttribute { Key = a.Key, Value = a.Value, Category = a.Category, LockString = a.LockString }).ToList(),
                Permissions = r.Permissions.ToList(),
                LockString = r.LockString
            }).ToList(),
            Exits = project.Connections.Select(c => new EvenniaExit
            {
                // Atlas structural fields
                SourceRoomId = c.SourceRoomId,
                DestinationRoomId = c.DestinationRoomId,
                Direction = c.Direction.ToString().ToLower(),
                ReverseDirection = c.ReverseDirection.ToString().ToLower(),
                IsOneWay = c.IsOneWay,
                ExitType = c.ExitType.ToString().ToLower(),
                // Legacy Atlas aliases (comma-separated)
                Aliases = string.IsNullOrWhiteSpace(c.Aliases) ? new List<string>() :
                    c.Aliases.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),

                // Evennia-native fields
                Key = string.IsNullOrWhiteSpace(c.KeyName) ? c.Direction.ToString().ToLower() : c.KeyName,
                Description = c.Description,
                TypeclassPath = c.TypeclassPath,
                AliasesList = c.AliasesList.Select(a => new EvenniaAlias { Key = a.Key, Category = a.Category }).ToList(),
                EvenniaTags = c.EvenniaTags.Select(t => new EvenniaTag { Key = t.Key, Category = t.Category, Data = t.Data }).ToList(),
                Attributes = c.Attributes.Select(a => new EvenniaAttribute { Key = a.Key, Value = a.Value, Category = a.Category, LockString = a.LockString }).ToList(),
                Permissions = c.Permissions.ToList(),
                LockString = c.LockString,

                // Door sub-object when applicable
                Door = c.Door == null ? null : new EvenniaDoor
                {
                    // Atlas door convenience fields
                    Name = c.Door.Name,
                    StartsOpen = c.Door.StartsOpen,
                    StartsClosed = c.Door.StartsClosed,
                    StartsLocked = c.Door.StartsLocked,
                    KeyId = c.Door.KeyId,
                    SharedDoorId = c.SharedDoorId,

                    // Door-specific Evennia configuration
                    Typeclass = c.Door.Typeclass,
                    Lockable = c.Door.Lockable,
                    SynchronizeOpposite = c.Door.SynchronizeOpposite,
                    TraverseLockString = c.Door.TraverseLockString,
                    LockedFailureMessage = c.Door.LockedFailureMessage,
                    ClosedFailureMessage = c.Door.ClosedFailureMessage,

                    // Door-specific metadata
                    Description = c.Door.Description,
                    DoorAliases = c.Door.DoorAliases.Select(a => new EvenniaAlias { Key = a.Key, Category = a.Category }).ToList(),
                    DoorTags = c.Door.DoorTags.Select(t => new EvenniaTag { Key = t.Key, Category = t.Category, Data = t.Data }).ToList(),
                    DoorAttributes = c.Door.DoorAttributes.Select(a => new EvenniaAttribute { Key = a.Key, Value = a.Value, Category = a.Category, LockString = a.LockString }).ToList(),
                    DoorPermissions = c.Door.DoorPermissions.ToList()
                }
            }).ToList(),
Items = project.Items.Select(i => new EvenniaItem
            {
                // Identity
                Id = i.Id,
                Key = i.Key,
                Description = i.Description,
                Notes = i.Notes,

                // Evennia metadata (inlined from EvenniaObjectMetadata)
                TypeclassPath = i.Metadata.TypeclassPath,
                Aliases = i.Metadata.Aliases.Select(a => new EvenniaAlias { Key = a.Key, Category = a.Category }).ToList(),
                EvenniaTags = i.Metadata.Tags.Select(t => new EvenniaTag { Key = t.Key, Category = t.Category, Data = t.Data }).ToList(),
                Attributes = i.Metadata.Attributes.Select(a => new EvenniaAttribute { Key = a.Key, Value = a.Value, Category = a.Category, LockString = a.LockString }).ToList(),
                Permissions = i.Metadata.Permissions.ToList(),
                LockString = i.Metadata.LockString,

                // Classification
                ItemType = i.ItemType.ToString().ToLower(),
                EquipmentSlot = i.EquipmentSlot.ToString().ToLower(),

                // Numeric attributes
                LevelRequirement = i.LevelRequirement,
                Encumbrance = i.Encumbrance,
                ItemLimit = i.ItemLimit,
                BaseValue = i.BaseValue,

                // Flags
                IsGettable = i.IsGettable,
                IsDroppable = i.IsDroppable,
                IsSellable = i.IsSellable,
                IsTradeable = i.IsTradeable,
                IsUnique = i.IsUnique,
                IsQuestItem = i.IsQuestItem,
                IsMagical = i.IsMagical,
                MagicLevel = i.MagicLevel,

                // Type-specific data (nullable)
                WeaponData = i.WeaponData == null ? null : new EvenniaWeaponData
                {
                    WeaponType = i.WeaponData.WeaponType.ToString().ToLower(),
                    DamageMin = i.WeaponData.DamageMin,
                    DamageMax = i.WeaponData.DamageMax,
                    StrengthRequirement = i.WeaponData.StrengthRequirement,
                    AccuracyModifier = i.WeaponData.AccuracyModifier,
                    BackstabAccuracyModifier = i.WeaponData.BackstabAccuracyModifier,
                    Speed = i.WeaponData.Speed,
                    Range = i.WeaponData.Range,
                    HandsRequired = i.WeaponData.HandsRequired
                },
                ArmorData = i.ArmorData == null ? null : new EvenniaArmorData
                {
                    ArmorType = i.ArmorData.ArmorType.ToString().ToLower(),
                    ArmorClass = i.ArmorData.ArmorClass,
                    DamageReduction = i.ArmorData.DamageReduction,
                    AccuracyModifier = i.ArmorData.AccuracyModifier
                },

                // Collections
                Requirements = i.Requirements.Select(r => new EvenniaItemRequirement
                {
                    RequirementType = r.RequirementType.ToString().ToLower(),
                    Operator = r.Operator.ToString().ToLower(),
                    Value = r.Value
                }).ToList(),
                Modifiers = i.Modifiers.Select(m => new EvenniaItemModifier
                {
                    ModifierType = m.ModifierType,
                    Value = m.Value,
                    ValueType = m.ValueType.ToString().ToLower(),
                    Notes = m.Notes
                }).ToList(),
                Effects = i.Effects.Select(e => new EvenniaItemEffect
                {
                    EffectType = e.EffectType,
                    EffectId = e.EffectId,
                    Trigger = e.Trigger.ToString().ToLower(),
                    Charges = e.Charges,
                    UsesPerDay = e.UsesPerDay,
                    ChancePercent = e.ChancePercent,
                    Notes = e.Notes
                }).ToList(),
                Sources = i.Sources.Select(s => new EvenniaItemSource
                {
                    SourceType = s.SourceType.ToString().ToLower(),
                    SourceId = s.SourceId,
                    SourceName = s.SourceName,
                    ChancePercent = s.ChancePercent,
                    Quantity = s.Quantity,
                    Cost = s.Cost,
                    Notes = s.Notes
                }).ToList(),
                CraftingComponents = i.CraftingComponents.Select(c => new EvenniaCraftingComponent
                {
                    ItemId = c.ItemId,
                    ItemName = c.ItemName,
                    Quantity = c.Quantity
                }).ToList()
            }).ToList(),
            Npcs = project.Npcs.Select(n => new EvenniaNpc
            {
                // Identity
                Id = n.Id,
                Key = n.Key,
                Description = n.Description,
                Notes = n.Notes,

                // Evennia metadata (inlined from EvenniaObjectMetadata)
                TypeclassPath = n.Metadata.TypeclassPath,
                Aliases = n.Metadata.Aliases.Select(a => new EvenniaAlias { Key = a.Key, Category = a.Category }).ToList(),
                EvenniaTags = n.Metadata.Tags.Select(t => new EvenniaTag { Key = t.Key, Category = t.Category, Data = t.Data }).ToList(),
                Attributes = n.Metadata.Attributes.Select(a => new EvenniaAttribute { Key = a.Key, Value = a.Value, Category = a.Category, LockString = a.LockString }).ToList(),
                Permissions = n.Metadata.Permissions.ToList(),
                LockString = n.Metadata.LockString,

                // Reference IDs
                LootTableId = n.LootTableId,
                DialogueId = n.DialogueId,

                // Stats
                Stats = new EvenniaNpcStats
                {
                    Level = n.Stats.Level,
                    MaxHealth = n.Stats.MaxHealth,
                    MaxMana = n.Stats.MaxMana,
                    MaxStamina = n.Stats.MaxStamina,
                    Strength = n.Stats.Strength,
                    Agility = n.Stats.Agility,
                    Intellect = n.Stats.Intellect,
                    Wisdom = n.Stats.Wisdom,
                    Charm = n.Stats.Charm,
                    ExperienceReward = n.Stats.ExperienceReward,
                    CurrencyReward = n.Stats.CurrencyReward
                },

                // Combat
                Combat = new EvenniaNpcCombat
                {
                    DamageMin = n.Combat.DamageMin,
                    DamageMax = n.Combat.DamageMax,
                    AccuracyModifier = n.Combat.AccuracyModifier,
                    AttackSpeed = n.Combat.AttackSpeed,
                    ArmorClass = n.Combat.ArmorClass,
                    DamageReduction = n.Combat.DamageReduction
                },

                // Classification
                Classification = new EvenniaNpcClassification
                {
                    Species = n.Classification.Species,
                    Profession = n.Classification.Profession,
                    Faction = n.Classification.Faction,
                    Alignment = n.Classification.Alignment
                },

                // Abilities
                Abilities = n.Abilities.Select(a => new EvenniaNpcAbility
                {
                    AbilityId = a.AbilityId,
                    AbilityName = a.AbilityName,
                    ChancePercent = a.ChancePercent,
                    CooldownSeconds = a.CooldownSeconds,
                    Priority = a.Priority,
                    Notes = a.Notes
                }).ToList(),

                // Resistances
                Resistances = n.Resistances.Select(r => new EvenniaNpcResistance
                {
                    DamageType = r.DamageType,
                    ResistancePercent = r.ResistancePercent,
                    IsImmune = r.IsImmune,
                    Notes = r.Notes
                }).ToList(),

                // Behavior
                Behavior = new EvenniaNpcBehavior
                {
                    AggressionMode = n.Behavior.AggressionMode.ToString().ToLower(),
                    AggroRange = n.Behavior.AggroRange,
                    CanWander = n.Behavior.CanWander,
                    WanderIntervalSeconds = n.Behavior.WanderIntervalSeconds,
                    CanFlee = n.Behavior.CanFlee,
                    FleeHealthPercent = n.Behavior.FleeHealthPercent
                },

                // Equipment
                Equipment = n.Equipment.Select(e => new EvenniaNpcEquipment
                {
                    ItemId = e.ItemId,
                    ItemName = e.ItemName,
                    Slot = e.Slot.ToString().ToLower()
                }).ToList(),

                // Inventory
                Inventory = n.Inventory.Select(inv => new EvenniaNpcInventoryItem
                {
                    ItemId = inv.ItemId,
                    ItemName = inv.ItemName,
                    Quantity = inv.Quantity
                }).ToList(),

                // Patrol
                Patrol = new EvenniaNpcPatrol
                {
                    Enabled = n.Patrol.Enabled,
                    Loop = n.Patrol.Loop,
                    Waypoints = n.Patrol.Waypoints.Select(w => new EvenniaNpcPatrolWaypoint
                    {
                        RoomId = w.RoomId,
                        RoomName = w.RoomName,
                        WaitSeconds = w.WaitSeconds
                    }).ToList()
                }
            }).ToList(),
            Spawns = project.Spawns.Select(s => new EvenniaSpawn
            {
                Id = s.Id,
                RoomId = s.RoomId,
                EntityId = s.EntityId,
                EntityType = s.EntityType.ToString().ToLower(),
                Quantity = s.Quantity,
                RespawnSeconds = s.RespawnSeconds,
                Enabled = s.Enabled
            }).ToList(),
            Shops = project.Shops.Select(sh => new EvenniaShop
            {
                Id = sh.Id,
                Key = sh.Key,
                Description = sh.Description,
                Notes = sh.Notes,

                // Evennia metadata (inlined from EvenniaObjectMetadata)
                TypeclassPath = sh.Metadata.TypeclassPath,
                Aliases = sh.Metadata.Aliases.Select(a => new EvenniaAlias { Key = a.Key, Category = a.Category }).ToList(),
                EvenniaTags = sh.Metadata.Tags.Select(t => new EvenniaTag { Key = t.Key, Category = t.Category, Data = t.Data }).ToList(),
                Attributes = sh.Metadata.Attributes.Select(a => new EvenniaAttribute { Key = a.Key, Value = a.Value, Category = a.Category, LockString = a.LockString }).ToList(),
                Permissions = sh.Metadata.Permissions.ToList(),
                LockString = sh.Metadata.LockString,

                NpcId = sh.NpcId,
                Inventory = sh.Inventory.Select(entry => new EvenniaShopInventoryEntry
                {
                    ItemId = entry.ItemId,
                    ItemName = entry.ItemName,
                    Quantity = entry.Quantity,
                    IsUnlimited = entry.IsUnlimited,
                    BuyPrice = entry.BuyPrice,
                    SellPrice = entry.SellPrice,
                    IsEnabled = entry.IsEnabled
                }).ToList()
            }).ToList(),
            LootTables = project.LootTables.Select(lt => new EvenniaLootTable
            {
                Id = lt.Id,
                Key = lt.Key,
                Description = lt.Description,
                Notes = lt.Notes,

                // Evennia metadata (inlined from EvenniaObjectMetadata)
                TypeclassPath = lt.Metadata.TypeclassPath,
                Aliases = lt.Metadata.Aliases.Select(a => new EvenniaAlias { Key = a.Key, Category = a.Category }).ToList(),
                EvenniaTags = lt.Metadata.Tags.Select(t => new EvenniaTag { Key = t.Key, Category = t.Category, Data = t.Data }).ToList(),
                Attributes = lt.Metadata.Attributes.Select(a => new EvenniaAttribute { Key = a.Key, Value = a.Value, Category = a.Category, LockString = a.LockString }).ToList(),
                Permissions = lt.Metadata.Permissions.ToList(),
                LockString = lt.Metadata.LockString,

                Entries = lt.Entries.Select(e => new EvenniaLootEntry
                {
                    ItemId = e.ItemId,
                    ItemName = e.ItemName,
                    ChancePercent = e.ChancePercent,
                    QuantityMin = e.QuantityMin,
                    QuantityMax = e.QuantityMax,
                    IsEnabled = e.IsEnabled
                }).ToList()
            }).ToList(),
            Quests = project.Quests.Select(q => new EvenniaQuest
            {
                // Identity
                Id = q.Id,
                Key = q.Key,
                Description = q.Description,
                Notes = q.Notes,

                // Evennia metadata (inlined from EvenniaObjectMetadata)
                TypeclassPath = q.Metadata.TypeclassPath,
                Aliases = q.Metadata.Aliases.Select(a => new EvenniaAlias { Key = a.Key, Category = a.Category }).ToList(),
                EvenniaTags = q.Metadata.Tags.Select(t => new EvenniaTag { Key = t.Key, Category = t.Category, Data = t.Data }).ToList(),
                Attributes = q.Metadata.Attributes.Select(a => new EvenniaAttribute { Key = a.Key, Value = a.Value, Category = a.Category, LockString = a.LockString }).ToList(),
                Permissions = q.Metadata.Permissions.ToList(),
                LockString = q.Metadata.LockString,

                // References
                GiverNpcId = q.GiverNpcId,
                TurnInNpcId = q.TurnInNpcId,

                // Objectives
                Objectives = q.Objectives.Select(o => new EvenniaQuestObjective
                {
                    ObjectiveType = o.ObjectiveType.ToString().ToLower(),
                    TargetId = o.TargetId,
                    TargetName = o.TargetName,
                    RequiredCount = o.RequiredCount,
                    Description = o.Description
                }).ToList(),

                // Rewards
                ExperienceReward = q.ExperienceReward,
                CurrencyReward = q.CurrencyReward,

                ItemRewards = q.ItemRewards.Select(ir => new EvenniaQuestItemReward
                {
                    ItemId = ir.ItemId,
                    ItemName = ir.ItemName,
                    Quantity = ir.Quantity
                }).ToList(),

                RewardLootTableId = q.RewardLootTableId
            }).ToList(),
            Dialogues = project.Dialogues.Select(d => new EvenniaDialogue
            {
                Id = d.Id,
                Key = d.Key,
                Description = d.Description,
                Notes = d.Notes,

                // Evennia metadata (inlined from EvenniaObjectMetadata)
                TypeclassPath = d.Metadata.TypeclassPath,
                Aliases = d.Metadata.Aliases.Select(a => new EvenniaAlias { Key = a.Key, Category = a.Category }).ToList(),
                EvenniaTags = d.Metadata.Tags.Select(t => new EvenniaTag { Key = t.Key, Category = t.Category, Data = t.Data }).ToList(),
                Attributes = d.Metadata.Attributes.Select(a => new EvenniaAttribute { Key = a.Key, Value = a.Value, Category = a.Category, LockString = a.LockString }).ToList(),
                Permissions = d.Metadata.Permissions.ToList(),
                LockString = d.Metadata.LockString,

                StartNodeId = d.StartNodeId,

                Nodes = d.Nodes.Select(n => new EvenniaDialogueNode
                {
                    Id = n.Id,
                    Text = n.Text,
                    SpeakerNpcId = n.SpeakerNpcId,
                    SpeakerName = n.SpeakerName,
                    Responses = n.Responses.Select(r => new EvenniaDialogueResponse
                    {
                        Text = r.Text,
                        NextNodeId = r.NextNodeId,
                        StartsQuestId = r.StartsQuestId,
                        CompletesQuestId = r.CompletesQuestId
                    }).ToList()
                }).ToList()
            }).ToList(),
            DamageTypes = project.DamageTypes.Select(dt => new EvenniaGameDataEntry
            {
                Id = dt.Id,
                Name = dt.Name,
                Description = dt.Description
            }).ToList(),
            Factions = project.Factions.Select(f => new EvenniaGameDataEntry
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description
            }).ToList(),
            Professions = project.Professions.Select(p => new EvenniaGameDataEntry
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description
            }).ToList(),
            Species = project.Species.Select(s => new EvenniaGameDataEntry
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description
            }).ToList(),
            Alignments = project.Alignments.Select(a => new EvenniaGameDataEntry
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description
            }).ToList(),
        };

        var json = JsonSerializer.Serialize(exportData, JsonOptions);

        var parentDir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        File.WriteAllText(filePath, json);
    }

    private class EvenniaExportData
    {
        public string AreaId { get; set; } = string.Empty;
        public string AreaName { get; set; } = string.Empty;
        public int Version { get; set; }
        public List<EvenniaRoom> Rooms { get; set; } = new();
        public List<EvenniaExit> Exits { get; set; } = new();
        public List<EvenniaItem> Items { get; set; } = new();
        public List<EvenniaNpc> Npcs { get; set; } = new();
        public List<EvenniaSpawn> Spawns { get; set; } = new();
        public List<EvenniaShop> Shops { get; set; } = new();
        public List<EvenniaLootTable> LootTables { get; set; } = new();
        public List<EvenniaQuest> Quests { get; set; } = new();
        public List<EvenniaDialogue> Dialogues { get; set; } = new();
        public List<EvenniaGameDataEntry> DamageTypes { get; set; } = new();
        public List<EvenniaGameDataEntry> Factions { get; set; } = new();
        public List<EvenniaGameDataEntry> Professions { get; set; } = new();
        public List<EvenniaGameDataEntry> Species { get; set; } = new();
        public List<EvenniaGameDataEntry> Alignments { get; set; } = new();
    }

    private class EvenniaRoom
    {
        // Atlas fields
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public List<string> Tags { get; set; } = new();
        public string Notes { get; set; } = string.Empty;

        // Evennia-native fields
        public string TypeclassPath { get; set; } = string.Empty;
        public List<EvenniaAlias> Aliases { get; set; } = new();
        public List<EvenniaTag> EvenniaTags { get; set; } = new();
        public List<EvenniaAttribute> Attributes { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public string LockString { get; set; } = string.Empty;
    }

    private class EvenniaExit
    {
        // Atlas structural fields
        public string SourceRoomId { get; set; } = string.Empty;
        public string DestinationRoomId { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public string ReverseDirection { get; set; } = string.Empty;
        public bool IsOneWay { get; set; }
        public string ExitType { get; set; } = "normal";
        public List<string> Aliases { get; set; } = new();   // legacy Atlas comma-separated

        // Evennia-native fields
        public string Key { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TypeclassPath { get; set; } = string.Empty;
        public List<EvenniaAlias> AliasesList { get; set; } = new();
        public List<EvenniaTag> EvenniaTags { get; set; } = new();
        public List<EvenniaAttribute> Attributes { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public string LockString { get; set; } = string.Empty;

        public EvenniaDoor? Door { get; set; }
    }

    private class EvenniaDoor
    {
        // Atlas door convenience
        public string Name { get; set; } = string.Empty;
        public bool StartsOpen { get; set; }
        public bool StartsClosed { get; set; } = true;
        public bool StartsLocked { get; set; }
        public string KeyId { get; set; } = string.Empty;
        public string SharedDoorId { get; set; } = string.Empty;

        // Door-specific Evennia configuration
        public string Typeclass { get; set; } = string.Empty;
        public bool Lockable { get; set; } = true;
        public bool SynchronizeOpposite { get; set; } = true;
        public string TraverseLockString { get; set; } = string.Empty;
        public string LockedFailureMessage { get; set; } = string.Empty;
        public string ClosedFailureMessage { get; set; } = string.Empty;

        // Door-specific metadata
        public string Description { get; set; } = string.Empty;
        public List<EvenniaAlias> DoorAliases { get; set; } = new();
        public List<EvenniaTag> DoorTags { get; set; } = new();
        public List<EvenniaAttribute> DoorAttributes { get; set; } = new();
        public List<string> DoorPermissions { get; set; } = new();
    }

    // --- Reusable structured sub-types ---

    private class EvenniaAlias
    {
        public string Key { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    private class EvenniaTag
    {
        public string Key { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }

    private class EvenniaAttribute
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string LockString { get; set; } = string.Empty;
    }
// --- Item export DTOs ---

    private class EvenniaItem
    {
        // Identity
        public string Id { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        // Evennia metadata (inlined from EvenniaObjectMetadata)
        public string TypeclassPath { get; set; } = string.Empty;
        public List<EvenniaAlias> Aliases { get; set; } = new();
        public List<EvenniaTag> EvenniaTags { get; set; } = new();
        public List<EvenniaAttribute> Attributes { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public string LockString { get; set; } = string.Empty;

        // Classification
        public string ItemType { get; set; } = "generic";
        public string EquipmentSlot { get; set; } = "none";

        // Numeric attributes
        public int LevelRequirement { get; set; }
        public double Encumbrance { get; set; }
        public int ItemLimit { get; set; }
        public decimal BaseValue { get; set; }

        // Flags
        public bool IsGettable { get; set; } = true;
        public bool IsDroppable { get; set; } = true;
        public bool IsSellable { get; set; } = true;
        public bool IsTradeable { get; set; } = true;
        public bool IsUnique { get; set; }
        public bool IsQuestItem { get; set; }
        public bool IsMagical { get; set; }
        public int MagicLevel { get; set; }

        // Type-specific data (nullable)
        public EvenniaWeaponData? WeaponData { get; set; }
        public EvenniaArmorData? ArmorData { get; set; }

        // Collections
        public List<EvenniaItemRequirement> Requirements { get; set; } = new();
        public List<EvenniaItemModifier> Modifiers { get; set; } = new();
        public List<EvenniaItemEffect> Effects { get; set; } = new();
        public List<EvenniaItemSource> Sources { get; set; } = new();
        public List<EvenniaCraftingComponent> CraftingComponents { get; set; } = new();
    }

    private class EvenniaWeaponData
    {
        public string WeaponType { get; set; } = string.Empty;
        public int DamageMin { get; set; }
        public int DamageMax { get; set; }
        public int StrengthRequirement { get; set; }
        public double AccuracyModifier { get; set; }
        public double BackstabAccuracyModifier { get; set; }
        public double Speed { get; set; }
        public int Range { get; set; }
        public int HandsRequired { get; set; } = 1;
    }

    private class EvenniaArmorData
    {
        public string ArmorType { get; set; } = string.Empty;
        public double ArmorClass { get; set; }
        public double DamageReduction { get; set; }
        public double AccuracyModifier { get; set; }
    }

    private class EvenniaItemRequirement
    {
        public string RequirementType { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    private class EvenniaItemModifier
    {
        public string ModifierType { get; set; } = string.Empty;
        public double Value { get; set; }
        public string ValueType { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    private class EvenniaItemEffect
    {
        public string EffectType { get; set; } = string.Empty;
        public string EffectId { get; set; } = string.Empty;
        public string Trigger { get; set; } = string.Empty;
        public int Charges { get; set; }
        public int UsesPerDay { get; set; }
        public double ChancePercent { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    private class EvenniaItemSource
    {
        public string SourceType { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public double ChancePercent { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal Cost { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    private class EvenniaCraftingComponent
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }

    // --- NPC export DTOs ---

    private class EvenniaNpc
    {
        // Identity
        public string Id { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        // Evennia metadata (inlined from EvenniaObjectMetadata)
        public string TypeclassPath { get; set; } = string.Empty;
        public List<EvenniaAlias> Aliases { get; set; } = new();
        public List<EvenniaTag> EvenniaTags { get; set; } = new();
        public List<EvenniaAttribute> Attributes { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public string LockString { get; set; } = string.Empty;

        // Reference IDs
        public string LootTableId { get; set; } = string.Empty;
        public string DialogueId { get; set; } = string.Empty;

        // Stats
        public EvenniaNpcStats Stats { get; set; } = new();

        // Combat
        public EvenniaNpcCombat Combat { get; set; } = new();

        // Classification
        public EvenniaNpcClassification Classification { get; set; } = new();

        // Abilities
        public List<EvenniaNpcAbility> Abilities { get; set; } = new();

        // Resistances
        public List<EvenniaNpcResistance> Resistances { get; set; } = new();

        // Behavior
        public EvenniaNpcBehavior Behavior { get; set; } = new();

        // Equipment
        public List<EvenniaNpcEquipment> Equipment { get; set; } = new();

        // Inventory
        public List<EvenniaNpcInventoryItem> Inventory { get; set; } = new();

        // Patrol
        public EvenniaNpcPatrol Patrol { get; set; } = new();
    }

    private class EvenniaNpcStats
    {
        public int Level { get; set; }
        public int MaxHealth { get; set; }
        public int MaxMana { get; set; }
        public int MaxStamina { get; set; }
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Intellect { get; set; }
        public int Wisdom { get; set; }
        public int Charm { get; set; }
        public int ExperienceReward { get; set; }
        public decimal CurrencyReward { get; set; }
    }

    private class EvenniaNpcCombat
    {
        public int DamageMin { get; set; }
        public int DamageMax { get; set; }
        public double AccuracyModifier { get; set; }
        public double AttackSpeed { get; set; }
        public double ArmorClass { get; set; }
        public double DamageReduction { get; set; }
    }

    private class EvenniaNpcClassification
    {
        public string Species { get; set; } = string.Empty;
        public string Profession { get; set; } = string.Empty;
        public string Faction { get; set; } = string.Empty;
        public string Alignment { get; set; } = string.Empty;
    }

    private class EvenniaNpcAbility
    {
        public string AbilityId { get; set; } = string.Empty;
        public string AbilityName { get; set; } = string.Empty;
        public double ChancePercent { get; set; }
        public double CooldownSeconds { get; set; }
        public int Priority { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    private class EvenniaNpcResistance
    {
        public string DamageType { get; set; } = string.Empty;
        public double ResistancePercent { get; set; }
        public bool IsImmune { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    private class EvenniaNpcBehavior
    {
        public string AggressionMode { get; set; } = string.Empty;
        public double AggroRange { get; set; }
        public bool CanWander { get; set; }
        public double WanderIntervalSeconds { get; set; }
        public bool CanFlee { get; set; }
        public double FleeHealthPercent { get; set; }
    }

    private class EvenniaNpcEquipment
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Slot { get; set; } = string.Empty;
    }

    private class EvenniaNpcInventoryItem
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    private class EvenniaNpcPatrol
    {
        public bool Enabled { get; set; }
        public bool Loop { get; set; }
        public List<EvenniaNpcPatrolWaypoint> Waypoints { get; set; } = new();
    }

    private class EvenniaNpcPatrolWaypoint
    {
        public string RoomId { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public double WaitSeconds { get; set; }
    }

    // --- Spawn export DTOs ---

    private class EvenniaSpawn
    {
        public string Id { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public double RespawnSeconds { get; set; }
        public bool Enabled { get; set; } = true;
    }

    // --- Shop export DTOs ---

    private class EvenniaShop
    {
        // Identity
        public string Id { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        // Evennia metadata (inlined from EvenniaObjectMetadata)
        public string TypeclassPath { get; set; } = string.Empty;
        public List<EvenniaAlias> Aliases { get; set; } = new();
        public List<EvenniaTag> EvenniaTags { get; set; } = new();
        public List<EvenniaAttribute> Attributes { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public string LockString { get; set; } = string.Empty;

        // Reference
        public string NpcId { get; set; } = string.Empty;

        // Inventory
        public List<EvenniaShopInventoryEntry> Inventory { get; set; } = new();
    }

    private class EvenniaShopInventoryEntry
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public bool IsUnlimited { get; set; }
        public decimal BuyPrice { get; set; }
        public decimal SellPrice { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    // --- Loot Table export DTOs ---

    private class EvenniaLootTable
    {
        // Identity
        public string Id { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        // Evennia metadata (inlined from EvenniaObjectMetadata)
        public string TypeclassPath { get; set; } = string.Empty;
        public List<EvenniaAlias> Aliases { get; set; } = new();
        public List<EvenniaTag> EvenniaTags { get; set; } = new();
        public List<EvenniaAttribute> Attributes { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public string LockString { get; set; } = string.Empty;

        // Entries
        public List<EvenniaLootEntry> Entries { get; set; } = new();
    }

    private class EvenniaLootEntry
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public double ChancePercent { get; set; }
        public int QuantityMin { get; set; } = 1;
        public int QuantityMax { get; set; } = 1;
        public bool IsEnabled { get; set; } = true;
    }

    // --- Quest export DTOs ---

    private class EvenniaQuest
    {
        // Identity
        public string Id { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        // Evennia metadata (inlined from EvenniaObjectMetadata)
        public string TypeclassPath { get; set; } = string.Empty;
        public List<EvenniaAlias> Aliases { get; set; } = new();
        public List<EvenniaTag> EvenniaTags { get; set; } = new();
        public List<EvenniaAttribute> Attributes { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public string LockString { get; set; } = string.Empty;

        // References
        public string GiverNpcId { get; set; } = string.Empty;
        public string TurnInNpcId { get; set; } = string.Empty;

        // Objectives
        public List<EvenniaQuestObjective> Objectives { get; set; } = new();

        // Rewards
        public int ExperienceReward { get; set; }
        public decimal CurrencyReward { get; set; }

        public List<EvenniaQuestItemReward> ItemRewards { get; set; } = new();

        public string RewardLootTableId { get; set; } = string.Empty;
    }

    private class EvenniaQuestObjective
    {
        public string ObjectiveType { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public int RequiredCount { get; set; } = 1;
        public string Description { get; set; } = string.Empty;
    }

    private class EvenniaQuestItemReward
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }

    // --- Dialogue export DTOs ---

    private class EvenniaDialogue
    {
        // Identity
        public string Id { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        // Evennia metadata (inlined from EvenniaObjectMetadata)
        public string TypeclassPath { get; set; } = string.Empty;
        public List<EvenniaAlias> Aliases { get; set; } = new();
        public List<EvenniaTag> EvenniaTags { get; set; } = new();
        public List<EvenniaAttribute> Attributes { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        public string LockString { get; set; } = string.Empty;

        // Dialogue structure
        public string StartNodeId { get; set; } = string.Empty;
        public List<EvenniaDialogueNode> Nodes { get; set; } = new();
    }

    private class EvenniaDialogueNode
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string SpeakerNpcId { get; set; } = string.Empty;
        public string SpeakerName { get; set; } = string.Empty;
        public List<EvenniaDialogueResponse> Responses { get; set; } = new();
    }

    private class EvenniaDialogueResponse
    {
        public string Text { get; set; } = string.Empty;
        public string NextNodeId { get; set; } = string.Empty;
        public string StartsQuestId { get; set; } = string.Empty;
        public string CompletesQuestId { get; set; } = string.Empty;
    }

    private class EvenniaGameDataEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}