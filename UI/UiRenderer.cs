using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyGame.Core;

namespace PyGame.UI;

public static class UiRenderer
{
    public static void DrawOverlay(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Viewport viewport,
        GameStateType state,
        Vector2 playerPos,
        string zoneName,
        DebugOverlayData debug)
    {
        switch (state)
        {
            case GameStateType.Title:
                DrawPanel(spriteBatch, pixel, new Rectangle(120, 80, viewport.Width - 240, viewport.Height - 160), new Color(20, 30, 52, 230));
                DrawBar(spriteBatch, pixel, new Rectangle(150, 120, viewport.Width - 300, 30), new Color(90, 130, 210));
                DrawBar(spriteBatch, pixel, new Rectangle(150, 170, viewport.Width - 300, 10), new Color(220, 220, 220));
                PixelFontRenderer.DrawLines(spriteBatch, pixel, new Vector2(160, 210), ["AETHER TRAIL", "PRESS ENTER TO START"], 2, new Color(236, 242, 255));
                break;

            case GameStateType.WorldExploration:
                DrawBar(spriteBatch, pixel, new Rectangle(12, 12, 330, 86), new Color(8, 12, 20, 180));
                DrawDebugSymbol(spriteBatch, pixel, new Vector2(28, 30), new Color(180, 240, 240));
                break;

            case GameStateType.PauseMenu:
                DrawPanel(spriteBatch, pixel, new Rectangle(200, 120, viewport.Width - 400, viewport.Height - 240), new Color(12, 16, 30, 240));
                DrawBar(spriteBatch, pixel, new Rectangle(220, 150, viewport.Width - 440, 8), new Color(220, 220, 220));
                PixelFontRenderer.DrawLines(spriteBatch, pixel, new Vector2(250, 190), ["PAUSED", "PRESS ESC TO RESUME"], 2, new Color(240, 240, 240));
                break;

            case GameStateType.EncounterOverlay:
                DrawPanel(spriteBatch, pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(10, 14, 10, 190));
                DrawPanel(spriteBatch, pixel, new Rectangle(120, viewport.Height - 180, viewport.Width - 240, 120), new Color(16, 22, 34, 240));
                PixelFontRenderer.DrawLines(spriteBatch, pixel, new Vector2(140, viewport.Height - 155), ["ENCOUNTER TRIGGERED", "PRESS ENTER TO RETURN"], 2, new Color(240, 246, 240));
                break;
        }

        DrawDebugOverlay(spriteBatch, pixel, viewport, debug, zoneName, playerPos);
    }

    private static void DrawDebugOverlay(SpriteBatch spriteBatch, Texture2D pixel, Viewport viewport, DebugOverlayData debug, string zoneName, Vector2 playerPos)
    {
        var panel = new Rectangle(12, viewport.Height - 138, 448, 126);
        DrawPanel(spriteBatch, pixel, panel, new Color(0, 0, 0, 150));

        var inputVector = $"{debug.LastInputDirection.X:0.00},{debug.LastInputDirection.Y:0.00}";
        var lines = new[]
        {
            $"STATE: {debug.State}",
            $"ZONE: {zoneName}",
            $"POS: {playerPos.X:0.0},{playerPos.Y:0.0} TILE: {debug.PlayerTile.X},{debug.PlayerTile.Y} ID:{debug.PlayerTileId}",
            $"INPUT: {(debug.InputDetected ? "YES" : "NO")} DIR: {inputVector} MOVED: {(debug.MovedThisFrame ? "YES" : "NO")}",
            $"WINDOW ACTIVE: {(debug.IsWindowActive ? "YES" : "NO")}"
        };

        PixelFontRenderer.DrawLines(spriteBatch, pixel, new Vector2(panel.X + 10, panel.Y + 10), lines, 2, new Color(230, 240, 230));
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
