using PyGame.Domain.Creatures;

namespace PyGame.Domain.Battle;

public sealed class Encounter
{
    public required bool IsTrainerBattle { get; init; }
    public required string OpponentName { get; init; }
    public required Creature OpponentCreature { get; init; }
    public string TrainerDefeatedFlag { get; init; } = string.Empty;
    public string TrainerVictoryFlag { get; init; } = string.Empty;
}
