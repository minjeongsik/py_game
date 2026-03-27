namespace PyGame.Domain.World;

public sealed class WorldPickup
{
    public string Id { get; init; } = string.Empty;
    public int X { get; init; }
    public int Y { get; init; }
    public string ItemId { get; init; } = string.Empty;
    public int Quantity { get; init; } = 1;
    public string CollectedFlag { get; init; } = string.Empty;
}
