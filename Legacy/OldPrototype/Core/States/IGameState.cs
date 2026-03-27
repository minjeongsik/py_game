using Microsoft.Xna.Framework;

namespace PyGame.Core.States;

public interface IGameState
{
    GameStateType Type { get; }
    void Update(GameTime gameTime, GameStateContext context);
}
