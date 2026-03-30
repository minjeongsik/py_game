using PyGame.Domain.Battle;
using PyGame.Domain.Creatures;
using PyGame.Domain.Inventory;
using PyGame.GameFlow.StateManager;

namespace PyGame.GameFlow.States.Battle;

internal static class BattleMoveHelper
{
    public static readonly MoveDefinition[] FallbackMoves =
    [
        new MoveDefinition { Id = "tackle", Name = "몸통박치기", TypeId = "neutral", Power = 5, MaxPp = 30, Accuracy = 95 }
    ];

    public static MoveDefinition[] ResolveMoves(GameContext context, Creature creature)
    {
        if (!context.Definitions.Species.TryGetValue(creature.SpeciesId, out var species) || species.MoveIds.Count == 0)
        {
            EnsureMovePp(creature, FallbackMoves[0]);
            return FallbackMoves;
        }

        var moves = new MoveDefinition[species.MoveIds.Count];
        for (var i = 0; i < species.MoveIds.Count; i++)
        {
            moves[i] = context.Definitions.Moves.TryGetValue(species.MoveIds[i], out var move) ? move : FallbackMoves[0];
            EnsureMovePp(creature, moves[i]);
        }

        return moves;
    }

    public static MoveDefinition? ChooseEnemyMove(Creature enemy, MoveDefinition[] enemyMoves)
    {
        var available = 0;
        for (var i = 0; i < enemyMoves.Length; i++)
        {
            if (GetCurrentPp(enemy, enemyMoves[i]) > 0)
            {
                available++;
            }
        }

        if (available == 0)
        {
            return null;
        }

        var pick = Random.Shared.Next(available);
        for (var i = 0; i < enemyMoves.Length; i++)
        {
            if (GetCurrentPp(enemy, enemyMoves[i]) <= 0)
            {
                continue;
            }

            if (pick-- == 0)
            {
                return enemyMoves[i];
            }
        }

        return enemyMoves[0];
    }

    public static List<ItemDefinition> GetBattleUsableItems(GameContext context)
    {
        var results = new List<ItemDefinition>();
        var slots = context.Session.Inventory.Slots;
        for (var i = 0; i < slots.Count; i++)
        {
            if (slots[i].Quantity > 0 &&
                context.Definitions.Items.TryGetValue(slots[i].ItemId, out var item) &&
                item.Category == "healing")
            {
                results.Add(item);
            }
        }

        return results;
    }

    public static int CalculateDamage(int attackerLevel, int defenderLevel, int power, float modifier)
    {
        return Math.Max(1, (int)MathF.Round((power + Random.Shared.Next(1, 4) + Math.Max(0, attackerLevel - defenderLevel)) * modifier));
    }

    public static bool CheckAccuracy(MoveDefinition move) => Random.Shared.Next(1, 101) <= Math.Clamp(move.Accuracy, 1, 100);

    public static void EnsureMovePp(Creature creature, MoveDefinition move)
    {
        if (!creature.MovePp.ContainsKey(move.Id))
        {
            creature.MovePp[move.Id] = move.MaxPp;
        }
    }

    public static int GetCurrentPp(Creature creature, MoveDefinition move)
    {
        EnsureMovePp(creature, move);
        return creature.MovePp[move.Id];
    }

    public static void SpendPp(Creature creature, MoveDefinition move)
    {
        EnsureMovePp(creature, move);
        creature.MovePp[move.Id] = Math.Max(0, creature.MovePp[move.Id] - 1);
    }

    public static Creature CloneCreature(Creature source)
    {
        return new Creature
        {
            SpeciesId = source.SpeciesId,
            Nickname = source.Nickname,
            Level = source.Level,
            MaxHealth = source.MaxHealth,
            CurrentHealth = source.CurrentHealth,
            Experience = source.Experience,
            MovePp = source.MovePp.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal)
        };
    }
}
