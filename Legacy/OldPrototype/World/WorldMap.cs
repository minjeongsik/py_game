using Microsoft.Xna.Framework;

namespace PyGame.World;

public sealed class WorldMap
{
    private readonly int[,] _tiles;

    public WorldMap(string id, string zoneName, int tileSize, int playerSpawnX, int playerSpawnY, int[,] tiles, List<WorldPortal> portals)
    {
        Id = id;
        CurrentZoneName = zoneName;
        TileSize = tileSize;
        PlayerSpawnX = playerSpawnX;
        PlayerSpawnY = playerSpawnY;
        _tiles = tiles;
        Portals = portals;
    }

    public string Id { get; }
    public string CurrentZoneName { get; }
    public int TileSize { get; }
    public int PlayerSpawnX { get; }
    public int PlayerSpawnY { get; }
    public List<WorldPortal> Portals { get; }

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
    public bool IsSafeTile(int tileId) => tileId == 3;
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

    public Point ResolveSpawnTile()
    {
        if (!IsBlockedTile(GetTileAt(PlayerSpawnX, PlayerSpawnY)))
        {
            return new Point(PlayerSpawnX, PlayerSpawnY);
        }

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (!IsBlockedTile(GetTileAt(x, y)))
                {
                    return new Point(x, y);
                }
            }
        }

        return Point.Zero;
    }

    public WorldPortal? TryGetPortalAt(Point tile)
    {
        return Portals.FirstOrDefault(p => p.X == tile.X && p.Y == tile.Y);
    }
}

public sealed class WorldPortal
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public required string TargetZoneId { get; init; }
    public required int TargetX { get; init; }
    public required int TargetY { get; init; }
}
