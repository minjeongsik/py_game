using Microsoft.Xna.Framework;

namespace PyGame.GameFlow.StateManager;

public interface IGameState
{
    GameStateId Id { get; }
    void Update(GameTime gameTime, GameContext context);
    void Draw(GameTime gameTime, GameContext context);
}
