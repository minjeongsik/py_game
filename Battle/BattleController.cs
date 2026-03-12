using PyGame.Core;
using PyGame.Creatures;
using PyGame.Data;

namespace PyGame.Battle;

public sealed class BattleController
{
    private readonly Random _random = new();

    private Dictionary<string, MoveDefinition> _moveLookup = null!;

    public BattleStatus Status { get; private set; } = BattleStatus.CommandSelection;
    public BattleCreature PlayerCreature { get; private set; } = null!;
    public BattleCreature WildCreature { get; private set; } = null!;
    public int SelectedCommandIndex { get; private set; }
    public string LastMessage { get; private set; } = "A wild presence appeared.";

    public bool IsBattleFinished => Status is BattleStatus.Victory or BattleStatus.Defeat or BattleStatus.Fled;

    public void Start(CreatureInstance playerInstance, CreatureSpeciesDefinition playerSpecies, CreatureInstance wildInstance, CreatureSpeciesDefinition wildSpecies, Dictionary<string, MoveDefinition> moveLookup)
    {
        _moveLookup = moveLookup;
        PlayerCreature = new BattleCreature(playerInstance, playerSpecies);
        WildCreature = new BattleCreature(wildInstance, wildSpecies);
        SelectedCommandIndex = 0;
        Status = BattleStatus.CommandSelection;
        LastMessage = $"A wild {wildSpecies.Name} engages.";
    }

    public void Update(InputState input)
    {
        if (Status != BattleStatus.CommandSelection)
        {
            return;
        }

        if (input.WasPressed(Microsoft.Xna.Framework.Input.Keys.Up))
        {
            SelectedCommandIndex = Math.Max(0, SelectedCommandIndex - 1);
        }

        if (input.WasPressed(Microsoft.Xna.Framework.Input.Keys.Down))
        {
            SelectedCommandIndex = Math.Min(2, SelectedCommandIndex + 1);
        }

        if (input.WasPressed(Microsoft.Xna.Framework.Input.Keys.Enter))
        {
            var action = (BattleActionType)SelectedCommandIndex;
            ResolveTurn(action);
        }
    }

    public string[] GetCommandLabels()
    {
        var firstMove = ResolveMoveName(PlayerCreature.Instance.EquippedMoveIds, 0, "Pulse Hit");
        var secondMove = ResolveMoveName(PlayerCreature.Instance.EquippedMoveIds, 1, "Arc Jolt");
        return [firstMove, secondMove, "Retreat"];
    }

    private void ResolveTurn(BattleActionType playerAction)
    {
        if (playerAction == BattleActionType.Flee)
        {
            var fleeChance = PlayerCreature.Speed >= WildCreature.Speed ? 0.75f : 0.45f;
            if (_random.NextSingle() < fleeChance)
            {
                Status = BattleStatus.Fled;
                LastMessage = "You safely retreated.";
                return;
            }

            LastMessage = "Retreat failed.";
            PerformWildMove();
            if (PlayerCreature.IsFainted)
            {
                Status = BattleStatus.Defeat;
                LastMessage = "Your ally can no longer fight.";
            }

            return;
        }

        var enemyAction = _random.Next(2) == 0 ? BattleActionType.UseMoveSlot1 : BattleActionType.UseMoveSlot2;
        var playerActsFirst = PlayerCreature.Speed >= WildCreature.Speed;

        if (playerActsFirst)
        {
            PerformAttack(PlayerCreature, WildCreature, playerAction);
            if (WildCreature.IsFainted)
            {
                Status = BattleStatus.Victory;
                LastMessage = $"Wild {WildCreature.Species.Name} was overcome.";
                return;
            }

            PerformAttack(WildCreature, PlayerCreature, enemyAction);
            if (PlayerCreature.IsFainted)
            {
                Status = BattleStatus.Defeat;
                LastMessage = $"{PlayerCreature.Species.Name} was overcome.";
                return;
            }
        }
        else
        {
            PerformAttack(WildCreature, PlayerCreature, enemyAction);
            if (PlayerCreature.IsFainted)
            {
                Status = BattleStatus.Defeat;
                LastMessage = $"{PlayerCreature.Species.Name} was overcome.";
                return;
            }

            PerformAttack(PlayerCreature, WildCreature, playerAction);
            if (WildCreature.IsFainted)
            {
                Status = BattleStatus.Victory;
                LastMessage = $"Wild {WildCreature.Species.Name} was overcome.";
                return;
            }
        }

        Status = BattleStatus.CommandSelection;
    }

    private void PerformWildMove()
    {
        var action = _random.Next(2) == 0 ? BattleActionType.UseMoveSlot1 : BattleActionType.UseMoveSlot2;
        PerformAttack(WildCreature, PlayerCreature, action);
    }

    private void PerformAttack(BattleCreature attacker, BattleCreature defender, BattleActionType action)
    {
        var move = ResolveMove(attacker.Instance.EquippedMoveIds, action);
        var damage = CalculateDamage(attacker, defender, move);
        defender.CurrentVitality -= damage;
        LastMessage = $"{attacker.Species.Name} used {move.Name} for {damage}.";
    }

    private MoveDefinition ResolveMove(List<string> equippedMoveIds, BattleActionType action)
    {
        var moveIndex = action == BattleActionType.UseMoveSlot1 ? 0 : 1;

        if (moveIndex < equippedMoveIds.Count && _moveLookup.TryGetValue(equippedMoveIds[moveIndex], out var equippedMove))
        {
            return equippedMove;
        }

        return _moveLookup.Values.First();
    }

    private string ResolveMoveName(List<string> equippedMoveIds, int index, string fallback)
    {
        if (index < equippedMoveIds.Count && _moveLookup.TryGetValue(equippedMoveIds[index], out var move))
        {
            return move.Name;
        }

        return fallback;
    }

    private static int CalculateDamage(BattleCreature attacker, BattleCreature defender, MoveDefinition move)
    {
        var affinity = TypeChart.GetMultiplier(move.EssenceType, defender.Species.EssenceType);
        var rawDamage = (move.Power + attacker.Power) - (defender.Guard / 2f);
        var adjusted = MathF.Max(1f, rawDamage * affinity);
        return (int)MathF.Round(adjusted);
    }
}
