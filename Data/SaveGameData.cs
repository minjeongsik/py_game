using Microsoft.Xna.Framework;
using PyGame.Creatures;

namespace PyGame.Data;

public sealed class SaveGameData
{
    public Vector2Serializable PlayerPosition { get; set; } = new();
    public string CurrentZoneId { get; set; } = string.Empty;
    public List<CreatureInstance> Party { get; set; } = [];
    public List<CreatureInstance> Storage { get; set; } = [];
    public List<InventoryEntry> Inventory { get; set; } = [];

    public static SaveGameData CreateFromSession(string zoneId, Vector2 playerPosition, List<CreatureInstance> party, List<CreatureInstance> storage, List<InventoryEntry> inventory)
    {
        return new SaveGameData
        {
            PlayerPosition = new Vector2Serializable { X = playerPosition.X, Y = playerPosition.Y },
            CurrentZoneId = zoneId,
            Party = party,
            Storage = storage,
            Inventory = inventory
        };
    }
}

public sealed class InventoryEntry
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public sealed class Vector2Serializable
{
    public float X { get; set; }
    public float Y { get; set; }

    public Vector2 ToVector2() => new(X, Y);
}
