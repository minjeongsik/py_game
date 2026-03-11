using Microsoft.Xna.Framework;

namespace PyGame.World;

public sealed class WorldMap
{
    private readonly int[,] _tiles;

    public WorldMap(string zoneName, int tileSize, int playerSpawnX, int playerSpawnY, int[,] tiles)
    {
        CurrentZoneName = zoneName;
        TileSize = tileSize;
        PlayerSpawnX = playerSpawnX;
        PlayerSpawnY = playerSpawnY;
        _tiles = tiles;
    }

    public string CurrentZoneName { get; }
    public int TileSize { get; }
    public int PlayerSpawnX { get; }
    public int PlayerSpawnY { get; }

    public int Width => _tiles.GetLength(1);
    public int Height => _tiles.GetLength(0);
    public int PixelWidth => Width * TileSize;
    public int PixelHeight => Height * TileSize;

    public int GetTileAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return 1;
        }

        return _tiles[y, x];
    }

    public bool IsBlockedTile(int tileId) => tileId == 1;

    public bool IsEncounterTile(int tileId) => tileId == 2;

    public bool IsBlockedAtWorldPosition(Vector2 position)
    {
        var tilePos = ToTilePosition(position);
        return IsBlockedTile(GetTileAt(tilePos.X, tilePos.Y));
    }

    public bool IsEncounterTileAtWorldPosition(Vector2 position)
    {
        var tilePos = ToTilePosition(position);
        return IsEncounterTile(GetTileAt(tilePos.X, tilePos.Y));
    }

    public Point ToTilePosition(Vector2 worldPosition)
    {
        return new Point((int)(worldPosition.X / TileSize), (int)(worldPosition.Y / TileSize));
    }
}
