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

        issues = issues.GroupBy(i => i.Code + "|" + i.ConnectionId + "|" + i.RoomId + "|" + i.Message).Select(g => g.First()).ToList();
        return issues;
    }

    private static ConnectionModel? FindStructuralReverse(ConnectionModel conn, List<ConnectionModel> connections)
    {
        if (string.IsNullOrEmpty(conn.SourceRoomId) || string.IsNullOrEmpty(conn.DestinationRoomId)) return null;
        return connections.FirstOrDefault(c => c.Id != conn.Id && c.SourceRoomId == conn.DestinationRoomId && c.DestinationRoomId == conn.SourceRoomId && c.Direction == conn.ReverseDirection);
    }
}
