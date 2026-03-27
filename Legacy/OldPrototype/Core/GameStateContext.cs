using Microsoft.Xna.Framework;
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
    public required int TitleSelection { get; init; }
    public required Action<int> SetTitleSelection { get; init; }
    public required Action PlayConfirm { get; init; }
    public required Action StartNewGame { get; init; }
    public required Func<bool> ContinueGame { get; init; }
    public required Action<string> SetHudMessage { get; init; }
    public required Action ExitGame { get; init; }
    public required int PauseSelection { get; init; }
    public required Action<int> SetPauseSelection { get; init; }
    public required Action SaveSession { get; init; }
    public required Vector2 PlayerPosition { get; init; }
    public required Action<Vector2> SetPlayerPosition { get; init; }
    public required Action EnterPauseMenu { get; init; }
    public required Action HandlePortals { get; init; }
    public required Action<GameTime> HandleEncounters { get; init; }
}
