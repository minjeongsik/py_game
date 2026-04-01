using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyGame.UI.Menus;

namespace PyGame.UI.Hud;

public sealed class InputHintRenderer
{
    private readonly PixelTextRenderer _textRenderer;
    private readonly UiSkinRenderer _uiSkin;

    public InputHintRenderer(PixelTextRenderer textRenderer, UiSkinRenderer uiSkin)
    {
        _textRenderer = textRenderer;
        _uiSkin = uiSkin;
    }

    public void DrawHints(SpriteBatch spriteBatch, Rectangle bounds, IReadOnlyList<string> hints)
    {
        if (hints.Count == 0)
        {
            return;
        }

        _uiSkin.DrawPanel(spriteBatch, bounds);
        var lineY = bounds.Y + 10;
        foreach (var line in hints)
        {
            _textRenderer.DrawText(new Vector2(bounds.X + 18, lineY), line, 1, new Color(240, 240, 234));
            lineY += 18;
        }
    }
}
