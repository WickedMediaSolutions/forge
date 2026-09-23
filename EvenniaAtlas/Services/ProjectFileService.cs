using System.Text.Json;
using System.IO;
using EvenniaAtlas.Models;

namespace EvenniaAtlas.Services;

public class ProjectFileService
{
    private const int SupportedVersion = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MapProject Load(string filePath)
    {
        var json = File.ReadAllText(filePath);

        // Inspect version before full deserialization
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("version", out var versionElement))
        {
            var fileVersion = versionElement.GetInt32();
            if (fileVersion > SupportedVersion)
            {
                throw new InvalidDataException(
                    $"Cannot open project version {fileVersion}. " +
                    $"This version of Rites of Passage: The Forge supports project versions up to {SupportedVersion}. " +
                    "Please use a newer version of Rites of Passage: The Forge to open this project.");
            }
        }

        var project = JsonSerializer.Deserialize<MapProject>(json, JsonOptions)
                      ?? throw new InvalidDataException("Failed to deserialize project file.");

        // Ensure all collections are non-null for older file versions
        project.Rooms ??= new();
        project.Connections ??= new();
        project.Items ??= new();
        project.Npcs ??= new();
        project.Spawns ??= new();
        project.Shops ??= new();
        project.LootTables ??= new();
        project.Quests ??= new();

        // Ensure Dialogues is non-null for older file versions
        project.Dialogues ??= new();

        project.DamageTypes ??= new();
        project.Factions ??= new();
        project.Professions ??= new();
        project.Species ??= new();
        project.Alignments ??= new();

        // Ensure per-Dialogue Metadata and Nodes are non-null for older file versions
        foreach (var dialogue in project.Dialogues)
        {
            dialogue.Metadata ??= new();
            dialogue.Nodes ??= new();

            foreach (var node in dialogue.Nodes)
            {
                node.Responses ??= new();
            }
        }

        // Ensure per-NPC Stats is non-null for older file versions
        foreach (var npc in project.Npcs)
            npc.Stats ??= new();

        // Ensure per-NPC Combat is non-null for older file versions
        foreach (var npc in project.Npcs)
            npc.Combat ??= new();

        // Ensure per-NPC Classification is non-null for older file versions
        foreach (var npc in project.Npcs)
            npc.Classification ??= new();

        // Ensure per-NPC Abilities is non-null for older file versions
        foreach (var npc in project.Npcs)
            npc.Abilities ??= new();

        // Ensure per-NPC Resistances is non-null for older file versions
        foreach (var npc in project.Npcs)
            npc.Resistances ??= new();

        // Ensure per-NPC Behavior is non-null for older file versions
        foreach (var npc in project.Npcs)
            npc.Behavior ??= new();

        // Ensure per-NPC Patrol is non-null for legacy/deserialized data
        foreach (var npc in project.Npcs)
        {
            npc.Patrol ??= new();
            npc.Patrol.Waypoints ??= new();
        }

        // Ensure per-NPC Equipment is non-null for older file versions
        foreach (var npc in project.Npcs)
            npc.Equipment ??= new();

        // Ensure per-NPC Inventory is non-null for older file versions
        foreach (var npc in project.Npcs)
            npc.Inventory ??= new();

        // Ensure per-NPC Metadata is non-null for older file versions
        foreach (var npc in project.Npcs)
            npc.Metadata ??= new();

        // Ensure per-Shop Metadata and Inventory are non-null for older file versions
        foreach (var shop in project.Shops)
        {
            shop.Metadata ??= new();
            shop.Inventory ??= new();
        }

        // Ensure per-LootTable Metadata and Entries are non-null for older file versions
        foreach (var table in project.LootTables)
        {
            table.Metadata ??= new();
            table.Entries ??= new();
        }

        // Ensure per-Quest Metadata, Objectives, and ItemRewards are non-null for older file versions
        foreach (var quest in project.Quests)
        {
            quest.Metadata ??= new();
            quest.Objectives ??= new();
            quest.ItemRewards ??= new();
        }

        return project;
    }

    public void Save(string filePath, MapProject project)
    {
        project.Version = SupportedVersion;
        var json = JsonSerializer.Serialize(project, JsonOptions);
        var tempPath = filePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, filePath, overwrite: true);
    }
}