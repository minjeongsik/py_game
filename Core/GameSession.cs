using Microsoft.Xna.Framework;
using PyGame.Creatures;
using PyGame.Data;

namespace PyGame.Core;

public sealed class GameSession
{
    public const int MaxPartySize = 3;

    public string CurrentZoneId { get; set; } = "haven_hamlet";
    public Vector2 PlayerPosition { get; set; }
    public List<CreatureInstance> Party { get; } = [];
    public List<CreatureInstance> Storage { get; } = [];
    public List<InventoryEntry> Inventory { get; } = [];

    public CreatureInstance ActiveCreature => Party.FirstOrDefault(c => !c.IsFainted) ?? Party.First();

    public static GameSession CreateNew(GameContentDatabase db)
    {
        var session = new GameSession();
        var starter = CreateCreatureInstance(db, "spriglet", 5);
        session.Party.Add(starter);
        session.Inventory.Add(new InventoryEntry { ItemId = "tonic_drop", Quantity = 4 });
        session.Inventory.Add(new InventoryEntry { ItemId = "lure_capsule", Quantity = 6 });
        return session;
    }

    public static CreatureInstance CreateCreatureInstance(GameContentDatabase db, string speciesId, int level)
    {
        var species = db.Species[speciesId];
        var maxVitality = species.BaseVitality + (level * 3);

        return new CreatureInstance
        {
            SpeciesId = speciesId,
            Nickname = species.Name,
            Level = level,
            MaxVitality = maxVitality,
            CurrentVitality = maxVitality,
            Power = species.BasePower + level,
            Guard = species.BaseGuard + (level / 2),
            Speed = species.BaseSpeed + (level / 2),
            EquippedMoveIds = species.LearnableMoveIds.Take(3).ToList()
        };
    }

    public bool AddCapturedCreature(CreatureInstance creature)
    {
        if (Party.Count < MaxPartySize)
        {
            Party.Add(creature);
            return true;
        }

        Storage.Add(creature);
        return false;
    }

    public int GetItemCount(string itemId)
    {
        return Inventory.FirstOrDefault(x => x.ItemId == itemId)?.Quantity ?? 0;
    }

    public bool ConsumeItem(string itemId)
    {
        var entry = Inventory.FirstOrDefault(x => x.ItemId == itemId);
        if (entry is null || entry.Quantity <= 0)
        {
            return false;
        }

        entry.Quantity -= 1;
        return true;
    }

    public void RestorePartyVitality()
    {
        foreach (var member in Party)
        {
            member.CurrentVitality = member.MaxVitality;
        }
    }
}
