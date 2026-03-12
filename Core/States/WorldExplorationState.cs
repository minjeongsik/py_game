using Microsoft.Xna.Framework;

namespace PyGame.Core.States;

public sealed class WorldExplorationState : IGameState
{
    public GameStateType Type => GameStateType.WorldExploration;

    public void Update(GameTime gameTime, GameStateContext context)
    {
        _ = gameTime;
        _ = context;
    }
}
