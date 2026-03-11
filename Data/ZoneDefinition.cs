namespace PyGame.Data;

public sealed class ZoneDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MapDataPath { get; set; } = string.Empty;
    public List<string> EncounterSpeciesPool { get; set; } = [];
}
