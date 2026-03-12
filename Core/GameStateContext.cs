using Microsoft.Xna.Framework.Graphics;
using PyGame.Data;
using PyGame.World;

namespace PyGame.Core;

public sealed class GameStateContext
{
    public required InputState Input { get; init; }
    public required GameStateManager StateManager { get; init; }
    public required Camera2D Camera { get; init; }
    public required WorldMap WorldMap { get; init; }
    public required PlayerController Player { get; init; }
    public required EncounterService EncounterService { get; init; }
    public required SaveGameService SaveGameService { get; init; }
    public required Func<Viewport> GetViewport { get; init; }
}
