using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PyGame.Core.States;

public sealed class TitleState : IGameState
{
    public GameStateType Type => GameStateType.Title;

    public void Update(GameTime gameTime, GameStateContext context)
    {
        _ = gameTime;

        if (context.Input.WasPressed(Keys.Enter))
        {
            context.StateManager.ChangeState(GameStateType.WorldExploration);
        }
    }
}
