namespace PyGame.Domain.Battle;

public sealed class EncounterDefinition
{
    public string CreatureId { get; init; } = string.Empty;
    public int MinLevel { get; init; }
    public int MaxLevel { get; init; }
}
