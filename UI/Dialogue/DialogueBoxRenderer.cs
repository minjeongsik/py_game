using Microsoft.Xna.Framework;
using PyGame.Core.Rendering;
using PyGame.UI.Hud;

namespace PyGame.UI.Dialogue;

public sealed class DialogueBoxRenderer
{
    private readonly PrimitiveRenderer _primitiveRenderer;
    private readonly PixelTextRenderer _textRenderer;

    public DialogueBoxRenderer(PrimitiveRenderer primitiveRenderer, PixelTextRenderer textRenderer)
    {
        _primitiveRenderer = primitiveRenderer;
        _textRenderer = textRenderer;
    }

    public void Draw(string speaker, string line, string prompt)
    {
        var box = new Rectangle(46, 286, 868, 198);
        _primitiveRenderer.Fill(box, new Color(10, 18, 28, 245));
        _primitiveRenderer.Outline(box, 4, new Color(208, 184, 104));
        _primitiveRenderer.Fill(new Rectangle(68, 304, 220, 32), new Color(68, 84, 108));
        _primitiveRenderer.Outline(new Rectangle(68, 304, 220, 32), 2, new Color(226, 206, 128));
        _textRenderer.DrawText(new Vector2(88, 314), speaker, 2, new Color(252, 242, 194));
        _textRenderer.DrawText(new Vector2(82, 360), line, 2, Color.White);
        _textRenderer.DrawText(new Vector2(82, 436), prompt, 2, new Color(208, 216, 222));
    }
}
