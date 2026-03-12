namespace PyGame.World;

public sealed class WorldMapData
{
    public string Name { get; set; } = "Starter Vale";
    public int TileSize { get; set; } = 32;
    public int PlayerSpawnX { get; set; } = 2;
    public int PlayerSpawnY { get; set; } = 2;
    public List<List<int>> Tiles { get; set; } = [];
}
