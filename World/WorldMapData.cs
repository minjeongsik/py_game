namespace PyGame.World;

public sealed class WorldMapData
{
    public string Id { get; set; } = "haven_hamlet";
    public string Name { get; set; } = "Haven Hamlet";
    public int TileSize { get; set; } = 32;
    public int PlayerSpawnX { get; set; } = 2;
    public int PlayerSpawnY { get; set; } = 2;
    public List<List<int>> Tiles { get; set; } = [];
    public List<PortalData> Portals { get; set; } = [];
}

public sealed class PortalData
{
    public int X { get; set; }
    public int Y { get; set; }
    public string TargetZoneId { get; set; } = string.Empty;
    public int TargetX { get; set; }
    public int TargetY { get; set; }
}
