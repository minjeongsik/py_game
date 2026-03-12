using Microsoft.Xna.Framework;

namespace PyGame.Core.States;

public sealed class TitleState : IGameState
{
    public GameStateType Type => GameStateType.Title;

    public void Update(GameTime gameTime, GameStateContext context)
    {
        _ = gameTime;
        _ = context;
    }
}
