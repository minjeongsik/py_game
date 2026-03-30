using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PyGame.GameFlow.StateManager;

namespace PyGame.GameFlow.States.Title;

public sealed class TitleState : IGameState
{
    private int _selected;
    private string _message = "새 게임을 시작하거나 저장된 모험을 이어 가세요.";

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

                loadResult.Session.StatusMessage = "저장된 모험을 이어갑니다.";
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

        context.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, context.Viewport.Height), new Color(26, 48, 44));
        context.PrimitiveRenderer.Fill(new Rectangle(0, 160, context.Viewport.Width, 150), new Color(98, 148, 104));
        context.PrimitiveRenderer.Fill(new Rectangle(0, 310, context.Viewport.Width, 230), new Color(50, 72, 96));
        context.UiSkin.DrawPanel(context.SpriteBatch, new Rectangle(120, 92, 720, 356));
        context.UiSkin.DrawPanel(context.SpriteBatch, new Rectangle(180, 118, 600, 104), true);
        context.TextRenderer.DrawText(new Vector2(202, 138), "몬스터 월드", 5, new Color(250, 238, 188));
        context.TextRenderer.DrawText(new Vector2(228, 194), "레트로 수집 RPG 프로토타입", 2, new Color(220, 228, 234));
        context.TextRenderer.DrawText(new Vector2(206, 236), hasSave ? "저장된 모험이 있어 바로 이어할 수 있습니다." : "저장 데이터가 없으면 새 게임부터 시작합니다.", 2, new Color(204, 216, 226));
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
