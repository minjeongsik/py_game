using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PyGame.Core.States;

public sealed class PauseMenuState : IGameState
{
    public GameStateType Type => GameStateType.PauseMenu;

    public void Update(GameTime gameTime, GameStateContext context)
    {
        _ = gameTime;

        if (context.Input.WasPressed(Keys.Escape))
        {
            context.StateManager.ChangeState(GameStateType.WorldExploration);
        }
    }
}
