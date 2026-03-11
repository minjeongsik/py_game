using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PyGame.Core;

namespace PyGame.World;

public sealed class PlayerController
{
    public PlayerController(Vector2 worldPosition, float moveSpeed)
    {
        WorldPosition = worldPosition;
        MoveSpeed = moveSpeed;
    }

    public Vector2 WorldPosition { get; set; }
    public float MoveSpeed { get; }
    public bool MovedThisFrame { get; private set; }

    public Rectangle Bounds => new((int)WorldPosition.X + 4, (int)WorldPosition.Y + 4, 24, 24);

    public void Update(GameTime gameTime, InputState input, WorldMap map)
    {
        var direction = Vector2.Zero;

        if (input.IsDown(Keys.W) || input.IsDown(Keys.Up)) direction.Y -= 1;
        if (input.IsDown(Keys.S) || input.IsDown(Keys.Down)) direction.Y += 1;
        if (input.IsDown(Keys.A) || input.IsDown(Keys.Left)) direction.X -= 1;
        if (input.IsDown(Keys.D) || input.IsDown(Keys.Right)) direction.X += 1;

        if (direction != Vector2.Zero)
        {
            direction.Normalize();
        }

        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var attempted = WorldPosition + direction * MoveSpeed * delta;
        MovedThisFrame = false;

        if (direction == Vector2.Zero)
        {
            return;
        }

        if (CanMoveTo(new Vector2(attempted.X, WorldPosition.Y), map))
        {
            WorldPosition = new Vector2(attempted.X, WorldPosition.Y);
            MovedThisFrame = true;
        }

        if (CanMoveTo(new Vector2(WorldPosition.X, attempted.Y), map))
        {
            WorldPosition = new Vector2(WorldPosition.X, attempted.Y);
            MovedThisFrame = true;
        }
    }

    private bool CanMoveTo(Vector2 targetPosition, WorldMap map)
    {
        var probe = new Rectangle((int)targetPosition.X + 4, (int)targetPosition.Y + 4, 24, 24);
        var corners = new[]
        {
            new Vector2(probe.Left, probe.Top),
            new Vector2(probe.Right, probe.Top),
            new Vector2(probe.Left, probe.Bottom),
            new Vector2(probe.Right, probe.Bottom)
        };

        foreach (var corner in corners)
        {
            if (map.IsBlockedAtWorldPosition(corner))
            {
                return false;
            }
        }

        return true;
    }
}
