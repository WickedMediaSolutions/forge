using EvenniaAtlas.Models;

namespace EvenniaAtlas.Services;

public enum ValidationSeverity { Error, Warning }

public class ValidationIssue
{
    public ValidationSeverity Severity { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RoomId { get; set; }
    public string? ConnectionId { get; set; }
    public string SeverityLabel => Severity == ValidationSeverity.Error ? "ERROR" : "WARNING";
    public string LocationLabel
    {
        get
        {
            if (!string.IsNullOrEmpty(RoomId) && !string.IsNullOrEmpty(ConnectionId))
                return $"Room: {RoomId} / Exit: {ConnectionId}";
            if (!string.IsNullOrEmpty(RoomId)) return $"Room: {RoomId}";
            if (!string.IsNullOrEmpty(ConnectionId)) return $"Exit: {ConnectionId}";
            return "";
        }
    }
}
public class MapValidationService
{
    public List<ValidationIssue> Validate(MapProject project)
    {
        var issues = new List<ValidationIssue>();
        var rooms = project.Rooms;
        var connections = project.Connections;
        var roomById = rooms.Where(r => !string.IsNullOrWhiteSpace(r.Id)).ToDictionary(r => r.Id, r => r);

        // ===== ROOM VALIDATION =====
        foreach (var g in rooms.Where(r => !string.IsNullOrWhiteSpace(r.Id)).GroupBy(r => r.Id).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "R001", Message = $"Duplicate room ID '{g.Key}' found {g.Count()} times.", RoomId = g.Key });

        foreach (var r in rooms.Where(r => string.IsNullOrWhiteSpace(r.Id)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "R002", Message = "Room has empty ID.", RoomId = "(empty)" });

        foreach (var g in rooms.GroupBy(r => (r.X, r.Y, r.Z)).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "R003", Message = $"Duplicate coordinates ({g.Key.X},{g.Key.Y},{g.Key.Z}) by: {string.Join(", ", g.Select(r => r.Id))}.", RoomId = g.First().Id });

        // ===== CONNECTION VALIDATION =====
        foreach (var c in connections.Where(c => string.IsNullOrWhiteSpace(c.Id)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "C001", Message = "Connection has empty ID.", ConnectionId = "(empty)" });

        foreach (var g in connections.Where(c => !string.IsNullOrWhiteSpace(c.Id)).GroupBy(c => c.Id).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "C002", Message = $"Duplicate connection ID '{g.Key}' found {g.Count()} times.", ConnectionId = g.Key });
// Per-connection rules
        foreach (var c in connections)
        {
            if (string.IsNullOrWhiteSpace(c.Id)) continue;
            if (!string.IsNullOrWhiteSpace(c.SourceRoomId) && !roomById.ContainsKey(c.SourceRoomId))
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "C003", Message = $"Source room '{c.SourceRoomId}' not found.", ConnectionId = c.Id });
            if (!string.IsNullOrWhiteSpace(c.DestinationRoomId) && !roomById.ContainsKey(c.DestinationRoomId))
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "C004", Message = $"Destination room '{c.DestinationRoomId}' not found.", ConnectionId = c.Id });
            if (!string.IsNullOrWhiteSpace(c.SourceRoomId) && c.SourceRoomId == c.DestinationRoomId)
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Code = "C005", Message = "Connection references same source and destination.", ConnectionId = c.Id });

            // Coordinate/direction mismatch
            if (roomById.TryGetValue(c.SourceRoomId, out var src) && roomById.TryGetValue(c.DestinationRoomId, out var dst))
            {
                var (dX, dY, dZ) = DirectionHelper.GetCoordinateDelta(c.Direction);
                var ex = src.X + dX; var ey = src.Y + dY; var ez = src.Z + dZ;
                if (dst.X != ex || dst.Y != ey || dst.Z != ez)
                    issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "C006", Message = $"Direction {c.Direction} mismatch: ({src.X},{src.Y},{src.Z}) + ({dX},{dY},{dZ}) = ({ex},{ey},{ez}), dest at ({dst.X},{dst.Y},{dst.Z}).", ConnectionId = c.Id });
            }
// Duplicate directional exit
            var dupDirs = connections.Where(oc => oc.Id != c.Id && oc.SourceRoomId == c.SourceRoomId && oc.Direction == c.Direction).ToList();
            foreach (var dup in dupDirs)
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "C007", Message = $"Duplicate direction {c.Direction} from '{c.SourceRoomId}': '{c.Id}' and '{dup.Id}'.", ConnectionId = c.Id, RoomId = c.SourceRoomId });

            // Reverse exit
            if (!c.IsOneWay)
            {
                var rev = FindStructuralReverse(c, connections);
                if (rev == null)
                    issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "C008", Message = $"Bidirectional but no reverse from '{c.DestinationRoomId}' to '{c.SourceRoomId}' with {c.ReverseDirection}.", ConnectionId = c.Id });
                else if (rev.IsOneWay)
                    issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Code = "C009", Message = $"Bidirectional but reverse '{rev.Id}' is one-way.", ConnectionId = c.Id });
            }
// Door validation
            if (c.HasDoor && c.Door != null)
            {
                var d = c.Door;
                if (d.StartsOpen && d.StartsClosed)
                    issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "D001", Message = "Door has both StartsOpen and StartsClosed.", ConnectionId = c.Id });
                if (d.StartsLocked && !d.StartsClosed)
                    issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "D002", Message = "Door StartsLocked but not StartsClosed.", ConnectionId = c.Id });
                if (d.StartsLocked && !d.Lockable)
                    issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "D003", Message = "Door StartsLocked but Lockable is false.", ConnectionId = c.Id });

                if (d.SynchronizeOpposite && !c.IsOneWay)
                {
                    var dr = FindStructuralReverse(c, connections);
                    if (dr == null)
                        issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "D004", Message = "Sync enabled but no structural reverse.", ConnectionId = c.Id });
                    else if (dr.HasDoor && dr.Door != null)
                    {
                        var rd = dr.Door;
                        if (!string.IsNullOrEmpty(c.SharedDoorId) && !string.IsNullOrEmpty(dr.SharedDoorId) && c.SharedDoorId != dr.SharedDoorId)
                            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "D005", Message = $"Sync pair SharedDoorId mismatch: '{c.SharedDoorId}' vs '{dr.SharedDoorId}'.", ConnectionId = c.Id });
                        if (d.Name != rd.Name) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "D006", Message = "Sync pair Name mismatch.", ConnectionId = c.Id });
                        if (d.Typeclass != rd.Typeclass) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "D006", Message = "Sync pair Typeclass mismatch.", ConnectionId = c.Id });
                        if (d.StartsOpen != rd.StartsOpen || d.StartsClosed != rd.StartsClosed || d.StartsLocked != rd.StartsLocked || d.Lockable != rd.Lockable)
                            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "D006", Message = "Sync pair state config mismatch.", ConnectionId = c.Id });
                        if (d.KeyId != rd.KeyId) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "D006", Message = "Sync pair KeyId mismatch.", ConnectionId = c.Id });
                        if (d.TraverseLockString != rd.TraverseLockString) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "D006", Message = "Sync pair TraverseLockString mismatch.", ConnectionId = c.Id });
                        if (d.LockedFailureMessage != rd.LockedFailureMessage) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "D006", Message = "Sync pair LockedFailure mismatch.", ConnectionId = c.Id });
                        if (d.ClosedFailureMessage != rd.ClosedFailureMessage) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "D006", Message = "Sync pair ClosedFailure mismatch.", ConnectionId = c.Id });
                    }
                }
            }
        }
// Metadata warnings for rooms
        foreach (var r in rooms.Where(r => !string.IsNullOrWhiteSpace(r.Id)))
        {
            foreach (var a in r.Aliases ?? new()) if (string.IsNullOrWhiteSpace(a.Key) && !string.IsNullOrWhiteSpace(a.Category)) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Code = "M001", Message = "Room alias with empty Key.", RoomId = r.Id });
            foreach (var t in r.EvenniaTags ?? new()) if (string.IsNullOrWhiteSpace(t.Key)) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Code = "M002", Message = "Room tag with empty Key.", RoomId = r.Id });
            foreach (var a in r.Attributes ?? new()) if (string.IsNullOrWhiteSpace(a.Key)) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Code = "M003", Message = "Room attribute with empty Key.", RoomId = r.Id });
            foreach (var p in r.Permissions ?? new()) if (string.IsNullOrWhiteSpace(p)) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Code = "M004", Message = "Room has empty permission.", RoomId = r.Id });
        }
        // Metadata warnings for connections
        foreach (var c in connections.Where(c => !string.IsNullOrWhiteSpace(c.Id)))
        {
            foreach (var a in c.AliasesList ?? new()) if (string.IsNullOrWhiteSpace(a.Key) && !string.IsNullOrWhiteSpace(a.Category)) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Code = "M001", Message = "Exit alias with empty Key.", ConnectionId = c.Id });
            foreach (var t in c.EvenniaTags ?? new()) if (string.IsNullOrWhiteSpace(t.Key)) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Code = "M002", Message = "Exit tag with empty Key.", ConnectionId = c.Id });
            foreach (var a in c.Attributes ?? new()) if (string.IsNullOrWhiteSpace(a.Key)) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Code = "M003", Message = "Exit attribute with empty Key.", ConnectionId = c.Id });
            foreach (var p in c.Permissions ?? new()) if (string.IsNullOrWhiteSpace(p)) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Code = "M004", Message = "Exit has empty permission.", ConnectionId = c.Id });
        }
        // Metadata warnings for doors (same M001-M004 codes as rooms/exits)
        foreach (var c in connections.Where(c => !string.IsNullOrWhiteSpace(c.Id) && c.Door != null))
        {
            var d = c.Door!;
            foreach (var a in d.DoorAliases ?? new()) if (string.IsNullOrWhiteSpace(a.Key) && !string.IsNullOrWhiteSpace(a.Category)) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Code = "M001", Message = "Door alias with empty Key.", ConnectionId = c.Id });
            foreach (var t in d.DoorTags ?? new()) if (string.IsNullOrWhiteSpace(t.Key)) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Code = "M002", Message = "Door tag with empty Key.", ConnectionId = c.Id });
            foreach (var a in d.DoorAttributes ?? new()) if (string.IsNullOrWhiteSpace(a.Key)) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Code = "M003", Message = "Door attribute with empty Key.", ConnectionId = c.Id });
            foreach (var p in d.DoorPermissions ?? new()) if (string.IsNullOrWhiteSpace(p)) issues.Add(new ValidationIssue { Severity = ValidationSeverity.Warning, Code = "M004", Message = "Door has empty permission.", ConnectionId = c.Id });
        }

        // Items
        foreach (var i in project.Items.Where(it => string.IsNullOrWhiteSpace(it.Id)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "I001", Message = "Item has empty ID." });
        foreach (var g in project.Items.Where(it => !string.IsNullOrWhiteSpace(it.Id)).GroupBy(it => it.Id).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "I002", Message = "Duplicate Item ID '" + g.Key + "' found " + g.Count() + " times." });

        // NPCs
        foreach (var n in project.Npcs.Where(n => string.IsNullOrWhiteSpace(n.Id)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "N001", Message = "NPC has empty ID." });
        foreach (var g in project.Npcs.Where(n => !string.IsNullOrWhiteSpace(n.Id)).GroupBy(n => n.Id).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "N002", Message = "Duplicate NPC ID '" + g.Key + "' found " + g.Count() + " times." });

        // Spawns
        foreach (var s in project.Spawns.Where(s => string.IsNullOrWhiteSpace(s.Id)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "S001", Message = "Spawn has empty ID." });
        foreach (var g in project.Spawns.Where(s => !string.IsNullOrWhiteSpace(s.Id)).GroupBy(s => s.Id).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "S002", Message = "Duplicate Spawn ID '" + g.Key + "' found " + g.Count() + " times." });

        // Shops
        foreach (var s in project.Shops.Where(s => string.IsNullOrWhiteSpace(s.Id)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "H001", Message = "Shop has empty ID." });
        foreach (var g in project.Shops.Where(s => !string.IsNullOrWhiteSpace(s.Id)).GroupBy(s => s.Id).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "H002", Message = "Duplicate Shop ID '" + g.Key + "' found " + g.Count() + " times." });

        // LootTables
        foreach (var l in project.LootTables.Where(l => string.IsNullOrWhiteSpace(l.Id)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "L001", Message = "LootTable has empty ID." });
        foreach (var g in project.LootTables.Where(l => !string.IsNullOrWhiteSpace(l.Id)).GroupBy(l => l.Id).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "L002", Message = "Duplicate LootTable ID '" + g.Key + "' found " + g.Count() + " times." });

        // Quests
        foreach (var q in project.Quests.Where(q => string.IsNullOrWhiteSpace(q.Id)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "Q001", Message = "Quest has empty ID." });
        foreach (var g in project.Quests.Where(q => !string.IsNullOrWhiteSpace(q.Id)).GroupBy(q => q.Id).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "Q002", Message = "Duplicate Quest ID '" + g.Key + "' found " + g.Count() + " times." });

        // Dialogues
        foreach (var d in project.Dialogues.Where(d => string.IsNullOrWhiteSpace(d.Id)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DG001", Message = "Dialogue has empty ID." });
        foreach (var g in project.Dialogues.Where(d => !string.IsNullOrWhiteSpace(d.Id)).GroupBy(d => d.Id).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DG002", Message = "Duplicate Dialogue ID '" + g.Key + "' found " + g.Count() + " times." });

        // ===== GAME DATA REGISTRY ID VALIDATION =====
        // DamageTypes
        foreach (var e in project.DamageTypes.Where(e => string.IsNullOrWhiteSpace(e.Id)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DT001", Message = "DamageType entry has empty ID." });
        foreach (var g in project.DamageTypes.Where(e => !string.IsNullOrWhiteSpace(e.Id)).GroupBy(e => e.Id).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DT002", Message = "Duplicate DamageType ID '" + g.Key + "' found " + g.Count() + " times." });

        // Factions
        foreach (var e in project.Factions.Where(e => string.IsNullOrWhiteSpace(e.Id)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "FC001", Message = "Faction entry has empty ID." });
        foreach (var g in project.Factions.Where(e => !string.IsNullOrWhiteSpace(e.Id)).GroupBy(e => e.Id).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "FC002", Message = "Duplicate Faction ID '" + g.Key + "' found " + g.Count() + " times." });

        // Professions
        foreach (var e in project.Professions.Where(e => string.IsNullOrWhiteSpace(e.Id)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "PF001", Message = "Profession entry has empty ID." });
        foreach (var g in project.Professions.Where(e => !string.IsNullOrWhiteSpace(e.Id)).GroupBy(e => e.Id).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "PF002", Message = "Duplicate Profession ID '" + g.Key + "' found " + g.Count() + " times." });

        // Species
        foreach (var e in project.Species.Where(e => string.IsNullOrWhiteSpace(e.Id)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "SS001", Message = "Species entry has empty ID." });
        foreach (var g in project.Species.Where(e => !string.IsNullOrWhiteSpace(e.Id)).GroupBy(e => e.Id).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "SS002", Message = "Duplicate Species ID '" + g.Key + "' found " + g.Count() + " times." });

        // Alignments
        foreach (var e in project.Alignments.Where(e => string.IsNullOrWhiteSpace(e.Id)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "AL001", Message = "Alignment entry has empty ID." });
        foreach (var g in project.Alignments.Where(e => !string.IsNullOrWhiteSpace(e.Id)).GroupBy(e => e.Id).Where(g => g.Count() > 1))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "AL002", Message = "Duplicate Alignment ID '" + g.Key + "' found " + g.Count() + " times." });

        // ===== DANGLING REFERENCE VALIDATION (Phase B) =====
        var itemById = project.Items.Where(i => !string.IsNullOrWhiteSpace(i.Id)).ToDictionary(i => i.Id, i => i);
        var npcById = project.Npcs.Where(n => !string.IsNullOrWhiteSpace(n.Id)).ToDictionary(n => n.Id, n => n);
        var lootTableById = project.LootTables.Where(l => !string.IsNullOrWhiteSpace(l.Id)).ToDictionary(l => l.Id, l => l);
        var questById = project.Quests.Where(q => !string.IsNullOrWhiteSpace(q.Id)).ToDictionary(q => q.Id, q => q);
        var dialogueById = project.Dialogues.Where(d => !string.IsNullOrWhiteSpace(d.Id)).ToDictionary(d => d.Id, d => d);

        // --- ROOM dangling references ---
        // DR001: Spawn.RoomId
        foreach (var s in project.Spawns.Where(s => !string.IsNullOrWhiteSpace(s.RoomId) && !roomById.ContainsKey(s.RoomId)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR001", Message = $"Spawn '{s.Id}' references missing Room '{s.RoomId}'." });

        // DR002: NPC Patrol.Waypoints[].RoomId
        foreach (var n in project.Npcs.Where(n => n.Patrol != null))
            foreach (var wp in n.Patrol.Waypoints.Where(wp => !string.IsNullOrWhiteSpace(wp.RoomId) && !roomById.ContainsKey(wp.RoomId)))
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR002", Message = $"NPC '{n.Id}' patrol waypoint references missing Room '{wp.RoomId}'." });

        // DR003: Quest VisitRoom objective TargetId
        foreach (var q in project.Quests)
            foreach (var obj in q.Objectives.Where(o => o.ObjectiveType == QuestObjectiveType.VisitRoom && !string.IsNullOrWhiteSpace(o.TargetId) && !roomById.ContainsKey(o.TargetId)))
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR003", Message = $"Quest '{q.Id}' VisitRoom objective references missing Room '{obj.TargetId}'." });

        // --- ITEM dangling references ---
        // DR004: Item.CraftingComponents[].ItemId
        foreach (var i in project.Items)
            foreach (var cc in i.CraftingComponents.Where(cc => !string.IsNullOrWhiteSpace(cc.ItemId) && !itemById.ContainsKey(cc.ItemId)))
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR004", Message = $"Item '{i.Id}' crafting component references missing Item '{cc.ItemId}'." });

        // DR005: Spawn.EntityId when EntityType == Item
        foreach (var s in project.Spawns.Where(s => s.EntityType == EntityType.Item && !string.IsNullOrWhiteSpace(s.EntityId) && !itemById.ContainsKey(s.EntityId)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR005", Message = $"Spawn '{s.Id}' references missing Item '{s.EntityId}'." });
// DR006: NPC Equipment[].ItemId
        foreach (var n in project.Npcs)
            foreach (var eq in n.Equipment.Where(eq => !string.IsNullOrWhiteSpace(eq.ItemId) && !itemById.ContainsKey(eq.ItemId)))
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR006", Message = $"NPC '{n.Id}' equipment references missing Item '{eq.ItemId}'." });

        // DR007: NPC Inventory[].ItemId
        foreach (var n in project.Npcs)
            foreach (var inv in n.Inventory.Where(inv => !string.IsNullOrWhiteSpace(inv.ItemId) && !itemById.ContainsKey(inv.ItemId)))
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR007", Message = $"NPC '{n.Id}' inventory references missing Item '{inv.ItemId}'." });

        // DR008: Shop Inventory[].ItemId
        foreach (var s in project.Shops)
            foreach (var inv in s.Inventory.Where(inv => !string.IsNullOrWhiteSpace(inv.ItemId) && !itemById.ContainsKey(inv.ItemId)))
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR008", Message = $"Shop '{s.Id}' inventory references missing Item '{inv.ItemId}'." });

        // DR009: LootTable Entries[].ItemId
        foreach (var lt in project.LootTables)
            foreach (var e in lt.Entries.Where(e => !string.IsNullOrWhiteSpace(e.ItemId) && !itemById.ContainsKey(e.ItemId)))
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR009", Message = $"LootTable '{lt.Id}' entry references missing Item '{e.ItemId}'." });

        // DR010: Quest CollectItem objective TargetId
        foreach (var q in project.Quests)
            foreach (var obj in q.Objectives.Where(o => o.ObjectiveType == QuestObjectiveType.CollectItem && !string.IsNullOrWhiteSpace(o.TargetId) && !itemById.ContainsKey(o.TargetId)))
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR010", Message = $"Quest '{q.Id}' CollectItem objective references missing Item '{obj.TargetId}'." });

        // DR011: Quest ItemRewards[].ItemId
        foreach (var q in project.Quests)
            foreach (var ir in q.ItemRewards.Where(ir => !string.IsNullOrWhiteSpace(ir.ItemId) && !itemById.ContainsKey(ir.ItemId)))
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR011", Message = $"Quest '{q.Id}' item reward references missing Item '{ir.ItemId}'." });

        // --- NPC dangling references ---
        // DR012: Spawn.EntityId when EntityType == Npc
        foreach (var s in project.Spawns.Where(s => s.EntityType == EntityType.Npc && !string.IsNullOrWhiteSpace(s.EntityId) && !npcById.ContainsKey(s.EntityId)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR012", Message = $"Spawn '{s.Id}' references missing NPC '{s.EntityId}'." });

        // DR013: Shop.NpcId
        foreach (var s in project.Shops.Where(s => !string.IsNullOrWhiteSpace(s.NpcId) && !npcById.ContainsKey(s.NpcId)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR013", Message = $"Shop '{s.Id}' references missing NPC '{s.NpcId}'." });

        // DR014: Quest.GiverNpcId
        foreach (var q in project.Quests.Where(q => !string.IsNullOrWhiteSpace(q.GiverNpcId) && !npcById.ContainsKey(q.GiverNpcId)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR014", Message = $"Quest '{q.Id}' references missing Giver NPC '{q.GiverNpcId}'." });

        // DR015: Quest.TurnInNpcId
        foreach (var q in project.Quests.Where(q => !string.IsNullOrWhiteSpace(q.TurnInNpcId) && !npcById.ContainsKey(q.TurnInNpcId)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR015", Message = $"Quest '{q.Id}' references missing TurnIn NPC '{q.TurnInNpcId}'." });

        // DR016: Quest KillNpc objective TargetId
        foreach (var q in project.Quests)
            foreach (var obj in q.Objectives.Where(o => o.ObjectiveType == QuestObjectiveType.KillNpc && !string.IsNullOrWhiteSpace(o.TargetId) && !npcById.ContainsKey(o.TargetId)))
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR016", Message = $"Quest '{q.Id}' KillNpc objective references missing NPC '{obj.TargetId}'." });

        // DR017: Dialogue Nodes[].SpeakerNpcId
        foreach (var d in project.Dialogues)
            foreach (var node in d.Nodes.Where(n => !string.IsNullOrWhiteSpace(n.SpeakerNpcId) && !npcById.ContainsKey(n.SpeakerNpcId)))
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR017", Message = $"Dialogue '{d.Id}' node '{node.Id}' references missing NPC '{node.SpeakerNpcId}'." });

        // --- LOOT TABLE dangling references ---
        // DR018: NPC.LootTableId
        foreach (var n in project.Npcs.Where(n => !string.IsNullOrWhiteSpace(n.LootTableId) && !lootTableById.ContainsKey(n.LootTableId)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR018", Message = $"NPC '{n.Id}' references missing LootTable '{n.LootTableId}'." });

        // DR019: Quest.RewardLootTableId
        foreach (var q in project.Quests.Where(q => !string.IsNullOrWhiteSpace(q.RewardLootTableId) && !lootTableById.ContainsKey(q.RewardLootTableId)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR019", Message = $"Quest '{q.Id}' references missing Reward LootTable '{q.RewardLootTableId}'." });

        // --- QUEST dangling references ---
        // DR020: Dialogue Responses[].StartsQuestId
        foreach (var d in project.Dialogues)
            foreach (var node in d.Nodes)
                foreach (var resp in node.Responses.Where(r => !string.IsNullOrWhiteSpace(r.StartsQuestId) && !questById.ContainsKey(r.StartsQuestId)))
                    issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR020", Message = $"Dialogue '{d.Id}' response references missing Quest '{resp.StartsQuestId}' (StartsQuestId)." });

        // DR021: Dialogue Responses[].CompletesQuestId
        foreach (var d in project.Dialogues)
            foreach (var node in d.Nodes)
                foreach (var resp in node.Responses.Where(r => !string.IsNullOrWhiteSpace(r.CompletesQuestId) && !questById.ContainsKey(r.CompletesQuestId)))
                    issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR021", Message = $"Dialogue '{d.Id}' response references missing Quest '{resp.CompletesQuestId}' (CompletesQuestId)." });

        // --- DIALOGUE dangling references ---
        // DR022: NPC.DialogueId
        foreach (var n in project.Npcs.Where(n => !string.IsNullOrWhiteSpace(n.DialogueId) && !dialogueById.ContainsKey(n.DialogueId)))
            issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR022", Message = $"NPC '{n.Id}' references missing Dialogue '{n.DialogueId}'." });

        // --- INTRA-DIALOGUE dangling references ---
        // DR023 & DR024: StartNodeId and Response.NextNodeId (within same Dialogue)
        foreach (var d in project.Dialogues)
        {
            var nodeIds = new HashSet<string>(d.Nodes.Where(n => !string.IsNullOrWhiteSpace(n.Id)).Select(n => n.Id));
            if (!string.IsNullOrWhiteSpace(d.StartNodeId) && !nodeIds.Contains(d.StartNodeId))
                issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR023", Message = $"Dialogue '{d.Id}' StartNodeId '{d.StartNodeId}' not found in dialogue nodes." });
            foreach (var node in d.Nodes)
                foreach (var resp in node.Responses.Where(r => !string.IsNullOrWhiteSpace(r.NextNodeId) && !nodeIds.Contains(r.NextNodeId)))
                    issues.Add(new ValidationIssue { Severity = ValidationSeverity.Error, Code = "DR024", Message = $"Dialogue '{d.Id}' node '{node.Id}' response NextNodeId '{resp.NextNodeId}' not found in dialogue nodes." });
        }

        issues = issues.GroupBy(i => i.Code + "|" + i.ConnectionId + "|" + i.RoomId + "|" + i.Message).Select(g => g.First()).ToList();
        return issues;
    }

    private static ConnectionModel? FindStructuralReverse(ConnectionModel conn, List<ConnectionModel> connections)
    {
        if (string.IsNullOrEmpty(conn.SourceRoomId) || string.IsNullOrEmpty(conn.DestinationRoomId)) return null;
        return connections.FirstOrDefault(c => c.Id != conn.Id && c.SourceRoomId == conn.DestinationRoomId && c.DestinationRoomId == conn.SourceRoomId && c.Direction == conn.ReverseDirection);
    }
}
