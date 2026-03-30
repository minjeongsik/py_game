using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PyGame.GameFlow.StateManager;

namespace PyGame.GameFlow.States.Dialogue;

public sealed class DialogueState : IGameState
{
    public GameStateId Id => GameStateId.Dialogue;

    public void Update(GameTime gameTime, GameContext context)
    {
        _ = gameTime;
        var dialogue = context.Session.ActiveDialogue;
        if (dialogue is null)
        {
            context.StateManager.ChangeState(context.Session.ReturnState);
            return;
        }

        if (context.Input.WasPressed(Keys.Escape))
        {
            context.Audio.PlayCancel();
            context.Session.ActiveDialogue = null;
            context.Session.StatusMessage = "대화를 종료했습니다.";
            context.StateManager.ChangeState(context.Session.ReturnState);
            return;
        }

        if (!context.Input.WasPressed(Keys.Enter) && !context.Input.WasPressed(Keys.Space))
        {
            return;
        }

        context.Audio.PlayConfirm();
        if (dialogue.Advance())
        {
            return;
        }

        context.Session.ActiveDialogue = null;
        context.Session.StatusMessage = "대화가 끝났습니다.";
        context.StateManager.ChangeState(context.Session.ReturnState);
    }

    public void Draw(GameTime gameTime, GameContext context)
    {
        _ = gameTime;
        context.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, context.Viewport.Height), new Color(24, 58, 74));
        context.UiSkin.DrawPanel(context.SpriteBatch, new Rectangle(40, 42, 880, 180));
        context.TextRenderer.DrawText(new Vector2(92, 94), "대화", 4, new Color(248, 236, 176));

        var dialogue = context.Session.ActiveDialogue;
        if (dialogue is not null)
        {
            context.DialogueRenderer.Draw(dialogue.Speaker, dialogue.CurrentLine, "Enter 다음  ESC 닫기");
        }

        context.SpriteBatch.End();
    }
}
