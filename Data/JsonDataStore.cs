using System.Text.Json;
using PyGame.Creatures;

namespace PyGame.Data;

public sealed class JsonDataStore
{
    private readonly Dictionary<string, CreatureSpeciesDefinition> _speciesById;
    private readonly Dictionary<string, MoveDefinition> _movesById;
    private readonly Dictionary<string, ZoneDefinition> _zonesById;

    public JsonDataStore(string contentDataPath)
    {
        _speciesById = LoadList<CreatureSpeciesDefinition>(Path.Combine(contentDataPath, "species_definitions.json")).ToDictionary(x => x.Id, x => x);
        _movesById = LoadList<MoveDefinition>(Path.Combine(contentDataPath, "move_definitions.json")).ToDictionary(x => x.Id, x => x);
        _zonesById = LoadList<ZoneDefinition>(Path.Combine(contentDataPath, "zone_definitions.json")).ToDictionary(x => x.Id, x => x);

        if (_speciesById.Count == 0)
        {
            throw new InvalidOperationException("Species data is empty.");
        }

        if (_movesById.Count == 0)
        {
            throw new InvalidOperationException("Move data is empty.");
        }

        if (_zonesById.Count == 0)
        {
            throw new InvalidOperationException("Zone data is empty.");
        }
    }

    public IReadOnlyDictionary<string, CreatureSpeciesDefinition> SpeciesById => _speciesById;
    public IReadOnlyDictionary<string, MoveDefinition> MovesById => _movesById;
    public IReadOnlyDictionary<string, ZoneDefinition> ZonesById => _zonesById;

    private static List<T> LoadList<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required content file is missing: {path}");
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<T>>(json) ?? [];
    }
}
