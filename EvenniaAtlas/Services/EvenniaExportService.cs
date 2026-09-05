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
            }).ToList()
        };

        var json = JsonSerializer.Serialize(exportData, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    private class EvenniaExportData
    {
        public string AreaId { get; set; } = string.Empty;
        public string AreaName { get; set; } = string.Empty;
        public int Version { get; set; }
        public List<EvenniaRoom> Rooms { get; set; } = new();
        public List<EvenniaExit> Exits { get; set; } = new();
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
}