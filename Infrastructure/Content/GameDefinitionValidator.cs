using Microsoft.Xna.Framework;
using PyGame.Domain.World;

namespace PyGame.Infrastructure.Content;

internal static class GameDefinitionValidator
{
    public static void Validate(GameDefinitions definitions)
    {
        foreach (var map in definitions.Maps.Values)
        {
            ValidateMapShape(map);
            ValidatePoint(map, new Point(map.SpawnX, map.SpawnY), "spawn");
            ValidateWalkable(map, new Point(map.SpawnX, map.SpawnY), "spawn");
            ValidatePoint(map, new Point(map.RecoveryX, map.RecoveryY), "recovery");
            ValidateWalkable(map, new Point(map.RecoveryX, map.RecoveryY), "recovery");

            for (var i = 0; i < map.Warps.Count; i++)
            {
                var warp = map.Warps[i];
                var source = new Point(warp.X, warp.Y);
                ValidatePoint(map, source, $"warp[{i}] source");
                ValidateWalkable(map, source, $"warp[{i}] source");

                if (!definitions.Maps.TryGetValue(warp.TargetMapId, out var targetMap))
                {
                    throw new InvalidOperationException($"Map '{map.Id}' warp[{i}] targets missing map '{warp.TargetMapId}'.");
                }

                var target = new Point(warp.TargetX, warp.TargetY);
                ValidatePoint(targetMap, target, $"warp[{i}] target");
                ValidateWalkable(targetMap, target, $"warp[{i}] target");
            }

            for (var i = 0; i < map.Npcs.Count; i++)
            {
                var point = new Point(map.Npcs[i].X, map.Npcs[i].Y);
                ValidatePoint(map, point, $"npc[{i}]");
                ValidateWalkable(map, point, $"npc[{i}]");
            }

            for (var i = 0; i < map.PcTerminals.Count; i++)
            {
                var point = new Point(map.PcTerminals[i].X, map.PcTerminals[i].Y);
                ValidatePoint(map, point, $"pcTerminal[{i}]");
                ValidateWalkable(map, point, $"pcTerminal[{i}]");
            }

            for (var i = 0; i < map.Pickups.Count; i++)
            {
                var point = new Point(map.Pickups[i].X, map.Pickups[i].Y);
                ValidatePoint(map, point, $"pickup[{i}]");
                ValidateWalkable(map, point, $"pickup[{i}]");
            }
        }
    }

    private static void ValidateMapShape(WorldMap map)
    {
        if (map.Rows.Count == 0)
        {
            throw new InvalidOperationException($"Map '{map.Id}' has no rows.");
        }

        var width = map.Rows[0].Length;
        for (var y = 0; y < map.Rows.Count; y++)
        {
            if (map.Rows[y].Length != width)
            {
                throw new InvalidOperationException($"Map '{map.Id}' row {y} has length {map.Rows[y].Length}, expected {width}.");
            }
        }
    }

    private static void ValidatePoint(WorldMap map, Point point, string label)
    {
        if (!map.IsInBounds(point))
        {
            throw new InvalidOperationException($"Map '{map.Id}' {label} ({point.X}, {point.Y}) is out of bounds.");
        }
    }

    private static void ValidateWalkable(WorldMap map, Point point, string label)
    {
        if (!map.IsWalkable(point))
        {
            throw new InvalidOperationException($"Map '{map.Id}' {label} ({point.X}, {point.Y}) is not walkable.");
        }
    }
}
