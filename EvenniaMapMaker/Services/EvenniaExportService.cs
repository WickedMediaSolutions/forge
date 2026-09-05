using System.Text.Json;
using System.IO;
using EvenniaMapMaker.Models;

namespace EvenniaMapMaker.Services;

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
                Id = r.Id,
                Title = r.Title,
                RoomType = r.RoomType,
                Description = r.Description,
                X = r.X,
                Y = r.Y,
                Z = r.Z,
                Tags = r.Tags,
                Notes = r.Notes
            }).ToList(),
            Exits = project.Connections.Select(c => new EvenniaExit
            {
                SourceRoomId = c.SourceRoomId,
                DestinationRoomId = c.DestinationRoomId,
                Direction = c.Direction.ToString().ToLower(),
                ReverseDirection = c.ReverseDirection.ToString().ToLower(),
                IsOneWay = c.IsOneWay,
                ExitType = c.ExitType.ToString().ToLower(),
                Aliases = string.IsNullOrWhiteSpace(c.Aliases) ? new List<string>() :
                    c.Aliases.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                Door = c.Door == null ? null : new EvenniaDoor
                {
                    Name = c.Door.Name,
                    StartsOpen = c.Door.StartsOpen,
                    StartsClosed = c.Door.StartsClosed,
                    StartsLocked = c.Door.StartsLocked,
                    KeyId = c.Door.KeyId,
                    SharedDoorId = c.SharedDoorId
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
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public List<string> Tags { get; set; } = new();
        public string Notes { get; set; } = string.Empty;
    }

    private class EvenniaExit
    {
        public string SourceRoomId { get; set; } = string.Empty;
        public string DestinationRoomId { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public string ReverseDirection { get; set; } = string.Empty;
        public bool IsOneWay { get; set; }
        public string ExitType { get; set; } = "normal";
        public List<string> Aliases { get; set; } = new();
        public EvenniaDoor? Door { get; set; }
    }

    private class EvenniaDoor
    {
        public string Name { get; set; } = string.Empty;
        public bool StartsOpen { get; set; }
        public bool StartsClosed { get; set; } = true;
        public bool StartsLocked { get; set; }
        public string KeyId { get; set; } = string.Empty;
        public string SharedDoorId { get; set; } = string.Empty;
    }
}