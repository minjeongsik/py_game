using System.Text.Json;
using Microsoft.Xna.Framework;
using PyGame.Creatures;

namespace PyGame.Data;

public sealed class SaveGameService
{
    private const string SaveFileName = "savegame.json";

    public void Save(SaveGameData data)
    {
        var path = GetSavePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public SaveSnapshot? TryLoad()
    {
        var path = GetSavePath();
        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<SaveGameData>(json);

        if (data is null)
        {
            return null;
        }

        return new SaveSnapshot(data.PlayerPosition.ToVector2(), data.CurrentZoneId, data.Party, data.Storage, data.Inventory);
    }

    public bool HasSaveFile() => File.Exists(GetSavePath());

    public string GetSaveFilePathForDisplay() => GetSavePath();

    private static string GetSavePath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "PyGame", SaveFileName);
    }
}

public sealed record SaveSnapshot(
    Vector2 PlayerPosition,
    string ZoneId,
    List<CreatureInstance> Party,
    List<CreatureInstance> Storage,
    List<InventoryEntry> Inventory);
