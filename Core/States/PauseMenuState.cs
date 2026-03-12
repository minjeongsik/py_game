using Microsoft.Xna.Framework;

namespace PyGame.Core.States;

public sealed class PauseMenuState : IGameState
{
    public GameStateType Type => GameStateType.PauseMenu;

    public void Update(GameTime gameTime, GameStateContext context)
    {
        _ = gameTime;
        _ = context;
    }
}
