using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PyGame.UI.Menus;

public sealed class UiSkinRenderer : IDisposable
{
    private readonly Dictionary<string, Texture2D> _textures = [];
    private bool _disposed;

    public UiSkinRenderer(GraphicsDevice graphicsDevice)
    {
        _textures["panel_base"] = CreatePanelTexture(graphicsDevice, new Color(250, 238, 184), new Color(220, 196, 118), new Color(108, 132, 150), new Color(18, 28, 38), new Color(12, 18, 28, 238));
        _textures["panel_light"] = CreatePanelTexture(graphicsDevice, new Color(255, 244, 204), new Color(220, 194, 120), new Color(158, 170, 182), new Color(206, 216, 224), new Color(236, 242, 246, 244));
        _textures["cursor"] = CreateCursorTexture(graphicsDevice);
        _textures["selection_fill"] = CreateSolidTexture(graphicsDevice, 8, 8, new Color(250, 220, 112, 96));
        _textures["selection_bar"] = CreateSolidTexture(graphicsDevice, 8, 8, new Color(246, 188, 70));
        _textures["hp_fill"] = CreateSolidTexture(graphicsDevice, 8, 8, new Color(114, 206, 102));
        _textures["hp_back"] = CreateSolidTexture(graphicsDevice, 8, 8, new Color(84, 96, 74));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        _textures.Clear();
        _disposed = true;
    }

    public void DrawPanel(SpriteBatch spriteBatch, Rectangle rect, bool light = false)
    {
        var texture = _textures[light ? "panel_light" : "panel_base"];
        DrawNineSlice(spriteBatch, texture, rect);
    }

    public void DrawSelection(SpriteBatch spriteBatch, Rectangle rect)
    {
        DrawPanel(spriteBatch, rect, true);
        spriteBatch.Draw(_textures["selection_fill"], new Rectangle(rect.X + 6, rect.Y + 4, rect.Width - 12, rect.Height - 8), Color.White);
        spriteBatch.Draw(_textures["selection_bar"], new Rectangle(rect.X + 6, rect.Y + 4, 8, rect.Height - 8), Color.White);
        spriteBatch.Draw(_textures["cursor"], new Rectangle(rect.X + 14, rect.Y + ((rect.Height - 16) / 2), 16, 16), new Color(30, 42, 56));
    }

    public void DrawHealthBar(SpriteBatch spriteBatch, Rectangle rect, float ratio)
    {
        spriteBatch.Draw(_textures["hp_back"], rect, Color.White);
        var fillWidth = Math.Max(0, (int)((rect.Width - 4) * Math.Clamp(ratio, 0f, 1f)));
        if (fillWidth > 0)
        {
            spriteBatch.Draw(_textures["hp_fill"], new Rectangle(rect.X + 2, rect.Y + 2, fillWidth, rect.Height - 4), Color.White);
        }
    }

    public void DrawExperienceBar(SpriteBatch spriteBatch, Rectangle rect, float ratio)
    {
        spriteBatch.Draw(_textures["hp_back"], rect, new Color(58, 64, 94));
        var fillWidth = Math.Max(0, (int)((rect.Width - 4) * Math.Clamp(ratio, 0f, 1f)));
        if (fillWidth > 0)
        {
            spriteBatch.Draw(_textures["selection_bar"], new Rectangle(rect.X + 2, rect.Y + 2, fillWidth, rect.Height - 4), new Color(104, 184, 255));
        }
    }

    private static Texture2D CreatePanelTexture(GraphicsDevice graphicsDevice, Color outer, Color innerBorder, Color trim, Color shade, Color fill)
    {
        const int size = 24;
        var data = new Color[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var color = fill;
                if (x == 0 || y == 0 || x == size - 1 || y == size - 1) color = outer;
                else if (x <= 2 || y <= 2 || x >= size - 3 || y >= size - 3) color = innerBorder;
                else if (x <= 4 || y <= 4 || x >= size - 5 || y >= size - 5) color = trim;
                else if ((x + y) % 5 == 0) color = shade;
                data[(y * size) + x] = color;
            }
        }

        var texture = new Texture2D(graphicsDevice, size, size);
        texture.SetData(data);
        return texture;
    }

    private static Texture2D CreateCursorTexture(GraphicsDevice graphicsDevice)
    {
        var rows = new[]
        {
            "........","..yy....",".yyyy...","yyyyyy..","yyyyyy..",".yyyy...","..yy....","........"
        };
        var data = new Color[64];
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                data[(y * 8) + x] = rows[y][x] == 'y' ? new Color(250, 224, 124) : Color.Transparent;
            }
        }

        var texture = new Texture2D(graphicsDevice, 8, 8);
        texture.SetData(data);
        return texture;
    }

    private static Texture2D CreateSolidTexture(GraphicsDevice graphicsDevice, int width, int height, Color color)
    {
        var texture = new Texture2D(graphicsDevice, width, height);
        var data = Enumerable.Repeat(color, width * height).ToArray();
        texture.SetData(data);
        return texture;
    }

    private static void DrawNineSlice(SpriteBatch spriteBatch, Texture2D texture, Rectangle destination)
    {
        const int corner = 8;
        var src = new Rectangle(0, 0, 24, 24);
        var centerWidth = Math.Max(1, destination.Width - (corner * 2));
        var centerHeight = Math.Max(1, destination.Height - (corner * 2));

        spriteBatch.Draw(texture, new Rectangle(destination.X, destination.Y, corner, corner), new Rectangle(src.X, src.Y, corner, corner), Color.White);
        spriteBatch.Draw(texture, new Rectangle(destination.Right - corner, destination.Y, corner, corner), new Rectangle(src.Right - corner, src.Y, corner, corner), Color.White);
        spriteBatch.Draw(texture, new Rectangle(destination.X, destination.Bottom - corner, corner, corner), new Rectangle(src.X, src.Bottom - corner, corner, corner), Color.White);
        spriteBatch.Draw(texture, new Rectangle(destination.Right - corner, destination.Bottom - corner, corner, corner), new Rectangle(src.Right - corner, src.Bottom - corner, corner, corner), Color.White);
        spriteBatch.Draw(texture, new Rectangle(destination.X + corner, destination.Y, centerWidth, corner), new Rectangle(src.X + corner, src.Y, src.Width - 16, corner), Color.White);
        spriteBatch.Draw(texture, new Rectangle(destination.X + corner, destination.Bottom - corner, centerWidth, corner), new Rectangle(src.X + corner, src.Bottom - corner, src.Width - 16, corner), Color.White);
        spriteBatch.Draw(texture, new Rectangle(destination.X, destination.Y + corner, corner, centerHeight), new Rectangle(src.X, src.Y + corner, corner, src.Height - 16), Color.White);
        spriteBatch.Draw(texture, new Rectangle(destination.Right - corner, destination.Y + corner, corner, centerHeight), new Rectangle(src.Right - corner, src.Y + corner, corner, src.Height - 16), Color.White);
        spriteBatch.Draw(texture, new Rectangle(destination.X + corner, destination.Y + corner, centerWidth, centerHeight), new Rectangle(src.X + corner, src.Y + corner, src.Width - 16, src.Height - 16), Color.White);
    }
}
