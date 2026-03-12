using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PyGame.Data;

namespace PyGame.Core.States;

public sealed class WorldExplorationState : IGameState
{
    public GameStateType Type => GameStateType.WorldExploration;

    public void Update(GameTime gameTime, GameStateContext context)
    {
        if (context.Input.WasPressed(Keys.Escape))
        {
            context.StateManager.ChangeState(GameStateType.PauseMenu);
            return;
        }

        if (context.Input.WasPressed(Keys.F5))
        {
            var save = SaveGameData.CreateFromPlayer(context.Player.WorldPosition, context.WorldMap.CurrentZoneName, [], []);
            context.SaveGameService.Save(save);
        }

        if (context.Input.WasPressed(Keys.F9))
        {
            var save = context.SaveGameService.TryLoad();
            if (save is not null)
            {
                context.Player.WorldPosition = save.PlayerPosition;
            }
        }

        context.Player.Update(gameTime, context.Input, context.WorldMap);
        context.Camera.Follow(context.Player.WorldPosition, context.GetViewport(), context.WorldMap.PixelWidth, context.WorldMap.PixelHeight);

        if (context.WorldMap.IsEncounterTileAtWorldPosition(context.Player.WorldPosition) && context.Player.MovedThisFrame)
        {
            if (context.EncounterService.RollEncounter(gameTime))
            {
                context.StateManager.ChangeState(GameStateType.EncounterOverlay);
            }
        }
    }
}
