using Microsoft.Xna.Framework;
using PyGame.Domain.Battle;

namespace PyGame.Domain.World;

public sealed class WorldMap
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int TileSize { get; init; } = 32;
    public int SpawnX { get; init; }
    public int SpawnY { get; init; }
    public int RecoveryX { get; init; }
    public int RecoveryY { get; init; }
    public List<string> Rows { get; init; } = [];
    public List<MapWarp> Warps { get; init; } = [];
    public List<NpcPlacement> Npcs { get; init; } = [];
    public List<PcTerminal> PcTerminals { get; init; } = [];
    public List<WorldPickup> Pickups { get; init; } = [];
    public List<EncounterDefinition> Encounters { get; init; } = [];

    public int Width => Rows.Count == 0 ? 0 : Rows[0].Length;
    public int Height => Rows.Count;
    public Point PixelSize => new(Width * TileSize, Height * TileSize);

    public TileType GetTileType(Point point)
    {
        if (point.X < 0 || point.Y < 0 || point.X >= Width || point.Y >= Height)
        {
            return TileType.Wall;
        }

        return Rows[point.Y][point.X] switch
        {
            '#' => TileType.Wall,
            'T' => TileType.Tree,
            'g' => TileType.Grass,
            '=' => TileType.Path,
            _ => TileType.Floor
        };
    }

    public bool IsWalkable(Point point)
    {
        var tile = GetTileType(point);
        return tile is not TileType.Wall and not TileType.Tree;
    }

    public bool IsEncounterTile(Point point) => GetTileType(point) == TileType.Grass;

    public bool TryGetWarpAt(Point point, out MapWarp warp)
    {
        for (var i = 0; i < Warps.Count; i++)
        {
            if (Warps[i].X == point.X && Warps[i].Y == point.Y)
            {
                warp = Warps[i];
                return true;
            }
        }

        warp = null!;
        return false;
    }

    public bool TryGetNpcAt(Point point, out NpcPlacement npcPlacement)
    {
        for (var i = 0; i < Npcs.Count; i++)
        {
            if (Npcs[i].X == point.X && Npcs[i].Y == point.Y)
            {
                npcPlacement = Npcs[i];
                return true;
            }
        }

        npcPlacement = null!;
        return false;
    }

    public bool TryGetPcTerminalAt(Point point, out PcTerminal terminal)
    {
        for (var i = 0; i < PcTerminals.Count; i++)
        {
            if (PcTerminals[i].X == point.X && PcTerminals[i].Y == point.Y)
            {
                terminal = PcTerminals[i];
                return true;
            }
        }

        terminal = null!;
        return false;
    }

    public bool TryGetPickupAt(Point point, out WorldPickup pickup)
    {
        for (var i = 0; i < Pickups.Count; i++)
        {
            if (Pickups[i].X == point.X && Pickups[i].Y == point.Y)
            {
                pickup = Pickups[i];
                return true;
            }
        }

        pickup = null!;
        return false;
    }
}
