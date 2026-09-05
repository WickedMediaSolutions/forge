using System.Text.Json;
using System.IO;
using EvenniaMapMaker.Models;

namespace EvenniaMapMaker.Services;

public class ProjectFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MapProject Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var project = JsonSerializer.Deserialize<MapProject>(json, JsonOptions)
                      ?? throw new InvalidDataException("Failed to deserialize project file.");
        return project;
    }

    public void Save(string filePath, MapProject project)
    {
        var json = JsonSerializer.Serialize(project, JsonOptions);
        File.WriteAllText(filePath, json);
    }
}