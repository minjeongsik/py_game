using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PyGame.Core.Camera;

public sealed class Camera2D
{
    public Matrix CreateWorldTransform(Vector2 focusWorldPosition, Viewport viewport, Point worldPixelSize)
    {
        var targetX = (viewport.Width * 0.5f) - focusWorldPosition.X;
        var targetY = (viewport.Height * 0.5f) - focusWorldPosition.Y;

        var minX = Math.Min(0f, viewport.Width - worldPixelSize.X);
        var minY = Math.Min(0f, viewport.Height - worldPixelSize.Y);

        var clampedX = Math.Clamp(targetX, minX, 0f);
        var clampedY = Math.Clamp(targetY, minY, 0f);

        return Matrix.CreateTranslation(clampedX, clampedY, 0f);
    }
}
