using Microsoft.Xna.Framework;
using PyGame.UI.Hud;

namespace PyGame.UI.Menus;

public sealed class MenuRenderer
{
    private readonly PixelTextRenderer _textRenderer;
    private readonly UiSkinRenderer _uiSkin;

    public MenuRenderer(PixelTextRenderer textRenderer, UiSkinRenderer uiSkin)
    {
        _textRenderer = textRenderer;
        _uiSkin = uiSkin;
    }

    public void Draw(Vector2 position, IReadOnlyList<string> options, int selectedIndex, int scale)
    {
        var y = position.Y;
        for (var i = 0; i < options.Count; i++)
        {
            var rowRect = new Rectangle((int)position.X - 20, (int)y - 6, 320, (scale * 10) + 10);
            if (i == selectedIndex)
            {
                _uiSkin.DrawSelection(_textRenderer.SpriteBatch, rowRect);
            }

            var color = i == selectedIndex ? new Color(24, 34, 48) : new Color(228, 232, 236);
            _textRenderer.DrawText(new Vector2(position.X + (i == selectedIndex ? 10 : 0), y), options[i], scale, color);
            y += (scale * 10) + 6;
        }
    }
}
