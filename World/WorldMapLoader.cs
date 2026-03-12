using System.Text.Json;

namespace PyGame.World;

public static class WorldMapLoader
{
    public static WorldMap Load(string path)
    {
        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<WorldMapData>(json) ?? throw new InvalidOperationException("Failed to deserialize map data.");

        if (data.Tiles.Count == 0)
        {
            throw new InvalidOperationException("Map tile rows are empty.");
        }

        var height = data.Tiles.Count;
        var width = data.Tiles[0].Count;
        var tiles = new int[height, width];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                tiles[y, x] = data.Tiles[y][x];
            }
        }

        return new WorldMap(data.Name, data.TileSize, data.PlayerSpawnX, data.PlayerSpawnY, tiles);
    }
}
