using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PyGame.World;

public static class WorldRenderer
{
    public static void DrawMap(SpriteBatch spriteBatch, Texture2D pixel, WorldMap map)
    {
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                var tile = map.GetTileAt(x, y);
                var color = tile switch
                {
                    0 => new Color(84, 176, 98),
                    1 => new Color(58, 72, 92),
                    2 => new Color(48, 120, 78),
                    _ => Color.Magenta
                };

                spriteBatch.Draw(pixel, new Rectangle(x * map.TileSize, y * map.TileSize, map.TileSize, map.TileSize), color);
            }
        }
    }

    public static void DrawPlayer(SpriteBatch spriteBatch, Texture2D pixel, PlayerController player)
    {
        spriteBatch.Draw(pixel, player.Bounds, new Color(232, 220, 96));
    }
}
