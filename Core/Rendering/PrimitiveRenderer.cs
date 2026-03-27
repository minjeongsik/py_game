using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PyGame.Core.Rendering;

public sealed class PrimitiveRenderer
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;

    public PrimitiveRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _pixel = pixel;
    }

    public void Fill(Rectangle rectangle, Color color)
    {
        _spriteBatch.Draw(_pixel, rectangle, color);
    }

    public void Outline(Rectangle rectangle, int thickness, Color color)
    {
        Fill(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
        Fill(new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
        Fill(new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
        Fill(new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
    }
}
