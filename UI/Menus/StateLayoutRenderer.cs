using Microsoft.Xna.Framework;
using PyGame.UI.Hud;

namespace PyGame.UI.Menus;

public sealed class StateLayoutRenderer
{
    private readonly PixelTextRenderer _textRenderer;
    private readonly UiSkinRenderer _uiSkin;

    public StateLayoutRenderer(PixelTextRenderer textRenderer, UiSkinRenderer uiSkin)
    {
        _textRenderer = textRenderer;
        _uiSkin = uiSkin;
    }

    public void DrawHeader(string title, string rightText, Color borderColor)
    {
        var rect = new Rectangle(24, 22, 912, 70);
        _uiSkin.DrawPanel(_textRenderer.SpriteBatch, rect);
        _textRenderer.DrawText(new Vector2(54, 40), title, 4, new Color(248, 238, 188));
        _textRenderer.DrawText(new Vector2(706, 46), rightText, 2, new Color(220, 228, 232));
    }

    public void DrawBodyPanels(Rectangle leftRect, Rectangle rightRect, Color borderColor)
    {
        _uiSkin.DrawPanel(_textRenderer.SpriteBatch, leftRect);
        _uiSkin.DrawPanel(_textRenderer.SpriteBatch, rightRect);
    }

    public void DrawFooter(string message, string hint, Color borderColor)
    {
        var rect = new Rectangle(24, 500, 912, 72);
        _uiSkin.DrawPanel(_textRenderer.SpriteBatch, rect);
        _textRenderer.DrawText(new Vector2(44, 514), message, 2, new Color(236, 236, 224));
        _textRenderer.DrawText(new Vector2(44, 538), hint, 1, new Color(216, 226, 232));
    }

    public void DrawSelectableRow(Rectangle rect, bool selected, Color selectedFill, Color normalFill, Color selectedBorder, Color normalBorder)
    {
        if (selected)
        {
            _uiSkin.DrawSelection(_textRenderer.SpriteBatch, rect);
        }
        else
        {
            _uiSkin.DrawPanel(_textRenderer.SpriteBatch, rect);
        }
    }
}
