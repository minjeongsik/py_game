using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PyGame.GameFlow.StateManager;

namespace PyGame.GameFlow.States.Title;

public sealed class TitleState : IGameState
{
    private int _selected;
    private string _message = "새 게임을 시작해 모험을 떠나세요.";

    public GameStateId Id => GameStateId.Title;

    public void Update(GameTime gameTime, GameContext context)
    {
        _ = gameTime;

        var options = GetOptions(context);

        if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W))
        {
            _selected = (_selected + options.Count - 1) % options.Count;
        }

        if (context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S))
        {
            _selected = (_selected + 1) % options.Count;
        }

        if (!context.Input.WasPressed(Keys.Enter))
        {
            return;
        }

        context.Audio.PlayConfirm();

        switch (options[_selected])
        {
            case "이어하기":
            {
                var loadResult = context.SaveGameService.TryLoad(context.Definitions);
                _message = loadResult.Message;
                if (!loadResult.Success || loadResult.Session is null)
                {
                    return;
                }

                loadResult.Session.StatusMessage = "저장한 모험을 이어갑니다.";
                context.ReplaceSession(loadResult.Session);
                context.StateManager.ChangeState(GameStateId.World);
                return;
            }
            case "새 게임":
                context.ResetSession();
                context.StateManager.ChangeState(GameStateId.World);
                return;
            default:
                context.ExitGame();
                return;
        }
    }

    public void Draw(GameTime gameTime, GameContext context)
    {
        _ = gameTime;

        var options = GetOptions(context);
        _selected = Math.Clamp(_selected, 0, options.Count - 1);
        var hasSave = context.SaveGameService.HasSave();

        context.SpriteBatch.Begin();
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, context.Viewport.Height), new Color(18, 34, 48));
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, 190), new Color(42, 88, 82));
        context.PrimitiveRenderer.Fill(new Rectangle(0, 190, context.Viewport.Width, 110), new Color(132, 176, 106));
        context.PrimitiveRenderer.Fill(new Rectangle(0, 300, context.Viewport.Width, 240), new Color(30, 56, 84));
        context.PrimitiveRenderer.Fill(new Rectangle(120, 92, 720, 356), new Color(16, 24, 34, 220));
        context.PrimitiveRenderer.Outline(new Rectangle(120, 92, 720, 356), 4, new Color(220, 194, 110));
        context.TextRenderer.DrawText(new Vector2(202, 138), "몬스터 필드", 5, new Color(250, 238, 188));
        context.TextRenderer.DrawText(new Vector2(228, 194), "골드 스타일 프로토타입", 2, new Color(220, 228, 234));
        context.TextRenderer.DrawText(new Vector2(240, 228), hasSave ? "저장된 모험이 있어 바로 이어할 수 있습니다" : "저장 데이터가 없으면 새 게임부터 시작합니다", 2, new Color(204, 216, 226));
        context.MenuRenderer.Draw(new Vector2(280, 278), options, _selected, 3);
        context.TextRenderer.DrawText(new Vector2(186, 392), _message, 2, new Color(204, 216, 226));
        context.SpriteBatch.End();
    }

    private static IReadOnlyList<string> GetOptions(GameContext context)
    {
        return context.SaveGameService.HasSave()
            ? ["이어하기", "새 게임", "종료"]
            : ["새 게임", "종료"];
    }
}
