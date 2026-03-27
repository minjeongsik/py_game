namespace PyGame.Domain.Creatures;

public sealed class SpeciesDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string PrimaryTypeId { get; init; } = "neutral";
    public List<string> MoveIds { get; init; } = [];
}
