using Microsoft.Xna.Framework.Graphics;
using PyGame.Core.Audio;
using PyGame.Core.Camera;
using PyGame.Core.Input;
using PyGame.Core.Rendering;
using PyGame.Infrastructure.Content;
using PyGame.Infrastructure.Save;
using PyGame.UI.Dialogue;
using PyGame.UI.Hud;
using PyGame.UI.Menus;

namespace PyGame.GameFlow.StateManager;

public sealed class GameContext
{
    public required InputSnapshot Input { get; init; }
    public required AudioService Audio { get; init; }
    public required Camera2D Camera { get; init; }
    public required PrimitiveRenderer PrimitiveRenderer { get; init; }
    public required PixelTextRenderer TextRenderer { get; init; }
    public required MenuRenderer MenuRenderer { get; init; }
    public required DialogueBoxRenderer DialogueRenderer { get; init; }
    public required GraphicsDevice GraphicsDevice { get; init; }
    public required SpriteBatch SpriteBatch { get; init; }
    public required GameDefinitions Definitions { get; init; }
    public required GameSession Session { get; init; }
    public required GameStateManager StateManager { get; init; }
    public required SaveGameService SaveGameService { get; init; }
    public required Action ExitGame { get; init; }
    public required Action ResetSession { get; init; }
    public required Action<GameSession> ReplaceSession { get; init; }

    public Viewport Viewport => GraphicsDevice.Viewport;
}
