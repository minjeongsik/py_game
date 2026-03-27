using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PyGame.Core.States;

public sealed class WorldExplorationState : IGameState
{
    public GameStateType Type => GameStateType.WorldExploration;

    public void Update(GameTime gameTime, GameStateContext context)
    {
        if (context.Input.WasPressed(Keys.Escape))
        {
            context.EnterPauseMenu();
            return;
        }

        var previous = context.PlayerPosition;

        context.Player.WorldPosition = context.PlayerPosition;
        context.Player.Update(gameTime, context.Input, context.WorldMap);
        context.SetPlayerPosition(context.Player.WorldPosition);

        var viewport = context.GetViewport();
        context.Camera.Follow(context.PlayerPosition, viewport, context.WorldMap.PixelWidth, context.WorldMap.PixelHeight);

        if (context.PlayerPosition != previous)
        {
            context.HandlePortals();
            context.HandleEncounters(gameTime);
        }

        if (context.Input.WasPressed(Keys.F5))
        {
            context.SaveSession();
            context.SetHudMessage("GAME SAVED");
        }
    }
}
