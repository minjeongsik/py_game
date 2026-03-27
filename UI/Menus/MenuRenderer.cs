using Microsoft.Xna.Framework;
using PyGame.UI.Hud;

namespace PyGame.UI.Menus;

public sealed class MenuRenderer
{
    private readonly PixelTextRenderer _textRenderer;

    public MenuRenderer(PixelTextRenderer textRenderer)
    {
        _textRenderer = textRenderer;
    }

    public void Draw(Vector2 position, IReadOnlyList<string> options, int selectedIndex, int scale)
    {
        var y = position.Y;
        for (var i = 0; i < options.Count; i++)
        {
            var prefix = i == selectedIndex ? "> " : "  ";
            var color = i == selectedIndex ? new Color(255, 233, 128) : new Color(228, 232, 236);
            _textRenderer.DrawText(new Vector2(position.X, y), prefix + options[i], scale, color);
            y += scale * 8;
        }
    }
}
