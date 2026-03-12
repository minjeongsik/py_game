using System.Text.Json;
using PyGame.Creatures;

namespace PyGame.Data;

public sealed class GameContentDatabase
{
    public required Dictionary<string, CreatureSpeciesDefinition> Species { get; init; }
    public required Dictionary<string, MoveDefinition> Moves { get; init; }
    public required Dictionary<string, ItemDefinition> Items { get; init; }
    public required Dictionary<string, ZoneDefinition> Zones { get; init; }

    public static GameContentDatabase Load(string contentDataRoot)
    {
        var species = LoadList<CreatureSpeciesDefinition>(Path.Combine(contentDataRoot, "creatures.json")).ToDictionary(x => x.Id);
        var moves = LoadList<MoveDefinition>(Path.Combine(contentDataRoot, "moves.json")).ToDictionary(x => x.Id);
        var items = LoadList<ItemDefinition>(Path.Combine(contentDataRoot, "items.json")).ToDictionary(x => x.Id);
        var zones = LoadList<ZoneDefinition>(Path.Combine(contentDataRoot, "zones.json")).ToDictionary(x => x.Id);

        return new GameContentDatabase
        {
            Species = species,
            Moves = moves,
            Items = items,
            Zones = zones
        };
    }

    private static List<T> LoadList<T>(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<T>>(json) ?? [];
    }
}
