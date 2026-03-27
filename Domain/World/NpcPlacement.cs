namespace PyGame.Domain.World;

public sealed class NpcPlacement
{
    public string NpcId { get; init; } = string.Empty;
    public int X { get; init; }
    public int Y { get; init; }
    public int FacingX { get; init; }
    public int FacingY { get; init; } = 1;
    public int SightRange { get; init; }
}
