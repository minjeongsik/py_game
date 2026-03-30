namespace PyGame.Domain.Battle;

public sealed class MoveDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string TypeId { get; init; } = "neutral";
    public int Power { get; init; } = 5;
    public int MaxPp { get; init; } = 20;
    public int Accuracy { get; init; } = 100;
}
