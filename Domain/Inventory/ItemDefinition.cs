namespace PyGame.Domain.Inventory;

public sealed class ItemDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = "utility";
    public int Price { get; init; }
    public int HealAmount { get; init; }
}
