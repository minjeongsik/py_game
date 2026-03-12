using Microsoft.Xna.Framework;
using PyGame.Core;

namespace PyGame.UI;

public sealed class DebugOverlayData
{
    public required GameStateType State { get; init; }
    public required Vector2 PlayerPosition { get; init; }
    public required Vector2 LastInputDirection { get; init; }
    public required bool InputDetected { get; init; }
    public required bool MovedThisFrame { get; init; }
    public required bool IsWindowActive { get; init; }
    public required Point PlayerTile { get; init; }
    public required int PlayerTileId { get; init; }
}
