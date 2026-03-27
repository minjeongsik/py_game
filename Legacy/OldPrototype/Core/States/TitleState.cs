using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PyGame.Core.States;

public sealed class TitleState : IGameState
{
    public GameStateType Type => GameStateType.Title;

    public void Update(GameTime gameTime, GameStateContext context)
    {
        _ = gameTime;

        var selection = context.TitleSelection;

        if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W))
        {
            selection = (selection + 2) % 3;
        }

        if (context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S))
        {
            selection = (selection + 1) % 3;
        }

        context.SetTitleSelection(selection);

        if (!context.Input.WasPressed(Keys.Enter))
        {
            return;
        }

        if (selection == 0)
        {
            context.PlayConfirm();
            context.StartNewGame();
            return;
        }

        if (selection == 1)
        {
            context.PlayConfirm();
            if (!context.ContinueGame())
            {
                context.SetHudMessage("NO SAVE DATA FOUND");
            }

            return;
        }

        context.PlayConfirm();
        context.ExitGame();
    }
}
