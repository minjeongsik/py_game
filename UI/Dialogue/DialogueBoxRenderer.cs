using Microsoft.Xna.Framework;
using PyGame.UI.Hud;
using PyGame.UI.Menus;

namespace PyGame.UI.Dialogue;

public sealed class DialogueBoxRenderer
{
    private readonly PixelTextRenderer _textRenderer;
    private readonly UiSkinRenderer _uiSkin;

    public DialogueBoxRenderer(PixelTextRenderer textRenderer, UiSkinRenderer uiSkin)
    {
        _textRenderer = textRenderer;
        _uiSkin = uiSkin;
    }

    public void Draw(string speaker, string line, string prompt)
    {
        var box = new Rectangle(46, 286, 868, 198);
        _uiSkin.DrawPanel(_textRenderer.SpriteBatch, box);
        _uiSkin.DrawPanel(_textRenderer.SpriteBatch, new Rectangle(68, 304, 220, 34), true);
        _textRenderer.DrawText(new Vector2(88, 314), speaker, 2, new Color(252, 242, 194));
        _textRenderer.DrawText(new Vector2(82, 360), line, 2, Color.White);
        _textRenderer.DrawText(new Vector2(82, 436), prompt, 2, new Color(208, 216, 222));
    }
}
