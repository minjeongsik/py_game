using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PyGame.GameFlow.StateManager;

namespace PyGame.GameFlow.States.Pause;

public sealed class PauseState : IGameState
{
    private readonly string[] _options = ["계속하기", "저장하기", "타이틀로"];
    private int _selected;
    private string _message = "잠시 멈췄습니다. 저장 후 타이틀로 돌아갈 수 있습니다.";

    public GameStateId Id => GameStateId.Pause;

    public void Update(GameTime gameTime, GameContext context)
    {
        _ = gameTime;

        if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W))
        {
            _selected = (_selected + _options.Length - 1) % _options.Length;
        }

        if (context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S))
        {
            _selected = (_selected + 1) % _options.Length;
        }

        if (context.Input.WasPressed(Keys.Escape))
        {
            context.Audio.PlayCancel();
            context.StateManager.ChangeState(context.Session.ReturnState);
            return;
        }

        if (!context.Input.WasPressed(Keys.Enter))
        {
            return;
        }

        context.Audio.PlayConfirm();
        switch (_selected)
        {
            case 0:
                context.StateManager.ChangeState(context.Session.ReturnState);
                return;
            case 1:
            {
                var saveResult = context.SaveGameService.Save(context.Session);
                _message = saveResult.Message;
                context.Session.StatusMessage = saveResult.Message;
                return;
            }
            default:
                context.ResetSession();
                context.StateManager.ChangeState(GameStateId.Title);
                return;
        }
    }

    public void Draw(GameTime gameTime, GameContext context)
    {
        _ = gameTime;

        context.SpriteBatch.Begin();
        context.PrimitiveRenderer.Fill(new Rectangle(220, 120, 520, 280), new Color(14, 20, 28, 240));
        context.PrimitiveRenderer.Outline(new Rectangle(220, 120, 520, 280), 3, new Color(192, 172, 104));
        context.TextRenderer.DrawText(new Vector2(390, 150), "일시정지", 4, new Color(248, 236, 182));
        context.MenuRenderer.Draw(new Vector2(320, 220), _options, _selected, 3);
        context.TextRenderer.DrawText(new Vector2(262, 334), _message, 2, new Color(214, 224, 235));
        context.TextRenderer.DrawText(new Vector2(294, 360), "ESC로 바로 돌아가고, 저장은 언제든 다시 할 수 있습니다", 2, new Color(214, 224, 235));
        context.SpriteBatch.End();
    }
}
