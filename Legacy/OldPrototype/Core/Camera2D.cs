using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PyGame.Core;

public sealed class Camera2D
{
    public Matrix Transform { get; private set; } = Matrix.Identity;

    public void Follow(Vector2 worldPosition, Viewport viewport, int worldWidthPixels, int worldHeightPixels)
    {
        var targetX = worldPosition.X - viewport.Width / 2f;
        var targetY = worldPosition.Y - viewport.Height / 2f;

        targetX = Math.Clamp(targetX, 0, Math.Max(0, worldWidthPixels - viewport.Width));
        targetY = Math.Clamp(targetY, 0, Math.Max(0, worldHeightPixels - viewport.Height));

        Transform = Matrix.CreateTranslation(-targetX, -targetY, 0f);
    }
}
