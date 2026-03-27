using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PyGame.Core.States;

public sealed class PauseMenuState : IGameState
{
    public GameStateType Type => GameStateType.PauseMenu;

    public void Update(GameTime gameTime, GameStateContext context)
    {
        _ = gameTime;

        var options = new[] { "RESUME", "SAVE", "TITLE" };
        var selection = context.PauseSelection;

        if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W))
        {
            selection = (selection + options.Length - 1) % options.Length;
        }

        if (context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S))
        {
            selection = (selection + 1) % options.Length;
        }

        context.SetPauseSelection(selection);

        if (context.Input.WasPressed(Keys.Escape))
        {
            context.StateManager.ChangeState(GameStateType.WorldExploration);
            return;
        }

        if (!context.Input.WasPressed(Keys.Enter))
        {
            return;
        }

        switch (selection)
        {
            case 0:
                context.StateManager.ChangeState(GameStateType.WorldExploration);
                break;
            case 1:
                context.PlayConfirm();
                context.SaveSession();
                context.SetHudMessage("GAME SAVED");
                context.StateManager.ChangeState(GameStateType.WorldExploration);
                break;
            case 2:
                context.StateManager.ChangeState(GameStateType.Title);
                break;
        }
    }
}
