using Microsoft.Xna.Framework;

namespace PyGame.Core.States;

public sealed class EncounterOverlayState : IGameState
{
    public GameStateType Type => GameStateType.Battle;

    public void Update(GameTime gameTime, GameStateContext context)
    {
        _ = gameTime;
        _ = context;
    }
}
