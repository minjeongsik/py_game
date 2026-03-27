namespace PyGame.Domain.Battle;

public sealed class MoveDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string TypeId { get; init; } = "neutral";
    public int Power { get; init; } = 5;
}
