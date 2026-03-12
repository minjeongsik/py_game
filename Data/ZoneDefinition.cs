namespace PyGame.Data;

public sealed class ZoneDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MapDataPath { get; set; } = string.Empty;
    public bool AllowEncounters { get; set; }
    public float EncounterRatePerSecond { get; set; }
    public List<EncounterEntryDefinition> EncounterTable { get; set; } = [];
}

public sealed class EncounterEntryDefinition
{
    public string SpeciesId { get; set; } = string.Empty;
    public int MinLevel { get; set; } = 1;
    public int MaxLevel { get; set; } = 3;
    public int Weight { get; set; } = 1;
}
