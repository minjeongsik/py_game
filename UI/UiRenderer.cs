using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyGame.Battle;
using PyGame.Core;

namespace PyGame.UI;

public static class UiRenderer
{
    public static void DrawOverlay(SpriteBatch spriteBatch, Texture2D pixel, Viewport viewport, GameStateType state, Vector2 playerPos, string zoneName, BattleController battleController)
    {
        switch (state)
        {
            case GameStateType.Title:
                DrawPanel(spriteBatch, pixel, new Rectangle(120, 80, viewport.Width - 240, viewport.Height - 160), new Color(20, 30, 52, 230));
                DrawBar(spriteBatch, pixel, new Rectangle(150, 120, viewport.Width - 300, 30), new Color(90, 130, 210));
                DrawBar(spriteBatch, pixel, new Rectangle(150, 170, viewport.Width - 300, 10), new Color(220, 220, 220));
                break;

            case GameStateType.WorldExploration:
                DrawBar(spriteBatch, pixel, new Rectangle(12, 12, 300, 72), new Color(8, 12, 20, 180));
                DrawDebugSymbol(spriteBatch, pixel, new Vector2(28, 30), new Color(180, 240, 240));
                break;

            case GameStateType.PauseMenu:
                DrawPanel(spriteBatch, pixel, new Rectangle(200, 120, viewport.Width - 400, viewport.Height - 240), new Color(12, 16, 30, 240));
                DrawBar(spriteBatch, pixel, new Rectangle(220, 150, viewport.Width - 440, 8), new Color(220, 220, 220));
                break;

            case GameStateType.BattleState:
                DrawBattleOverlay(spriteBatch, pixel, viewport, battleController);
                break;
        }

        _ = playerPos;
        _ = zoneName;
    }

    private static void DrawBattleOverlay(SpriteBatch spriteBatch, Texture2D pixel, Viewport viewport, BattleController battle)
    {
        DrawPanel(spriteBatch, pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(14, 14, 24, 255));

        DrawPanel(spriteBatch, pixel, new Rectangle(72, 80, 280, 110), new Color(34, 48, 66));
        DrawPanel(spriteBatch, pixel, new Rectangle(viewport.Width - 352, 260, 280, 110), new Color(58, 70, 42));

        DrawVitalityBar(spriteBatch, pixel, new Rectangle(90, 165, 240, 12), battle.WildCreature.CurrentVitality, battle.WildCreature.MaxVitality, new Color(220, 86, 96));
        DrawVitalityBar(spriteBatch, pixel, new Rectangle(viewport.Width - 334, 345, 240, 12), battle.PlayerCreature.CurrentVitality, battle.PlayerCreature.MaxVitality, new Color(86, 220, 126));

        DrawPanel(spriteBatch, pixel, new Rectangle(40, viewport.Height - 170, viewport.Width - 80, 130), new Color(12, 18, 30));

        var options = battle.GetCommandLabels();
        for (var i = 0; i < options.Length; i++)
        {
            var optionRect = new Rectangle(68, viewport.Height - 145 + (i * 34), 240, 24);
            var isSelected = battle.SelectedCommandIndex == i && battle.Status == BattleStatus.CommandSelection;
            DrawPanel(spriteBatch, pixel, optionRect, isSelected ? new Color(100, 150, 240) : new Color(60, 74, 94));
        }

        if (battle.Status is BattleStatus.Victory or BattleStatus.Defeat or BattleStatus.Fled)
        {
            DrawPanel(spriteBatch, pixel, new Rectangle(viewport.Width - 300, viewport.Height - 150, 220, 70), new Color(130, 90, 30));
        }
    }

    private static void DrawVitalityBar(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, int current, int max, Color fillColor)
    {
        DrawPanel(spriteBatch, pixel, rect, new Color(36, 40, 48));
        var ratio = max > 0 ? (float)current / max : 0;
        ratio = Math.Clamp(ratio, 0f, 1f);
        DrawPanel(spriteBatch, pixel, new Rectangle(rect.X + 2, rect.Y + 2, (int)((rect.Width - 4) * ratio), rect.Height - 4), fillColor);
    }

    private static void DrawPanel(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
    {
        spriteBatch.Draw(pixel, rect, color);
    }

    private static void DrawBar(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
    {
        spriteBatch.Draw(pixel, rect, color);
    }

    private static void DrawDebugSymbol(SpriteBatch spriteBatch, Texture2D pixel, Vector2 position, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle((int)position.X, (int)position.Y, 6, 6), color);
        spriteBatch.Draw(pixel, new Rectangle((int)position.X + 10, (int)position.Y, 20, 6), color);
    }
}
