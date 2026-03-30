namespace PyGame.Domain.World;

public sealed class MapWarp
{
    public int X { get; init; }
    public int Y { get; init; }
    public string TargetMapId { get; init; } = string.Empty;
    public int TargetX { get; init; }
    public int TargetY { get; init; }
    public string TransitionText { get; init; } = string.Empty;
}
