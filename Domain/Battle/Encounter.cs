using PyGame.Domain.Creatures;

namespace PyGame.Domain.Battle;

public sealed class Encounter
{
    public required bool IsTrainerBattle { get; init; }
    public required string OpponentName { get; init; }
    public required List<Creature> OpponentParty { get; init; }
    public string TrainerDefeatedFlag { get; init; } = string.Empty;
    public string TrainerVictoryFlag { get; init; } = string.Empty;
    public int OpponentIndex { get; set; }
    public Creature OpponentCreature => OpponentParty[OpponentIndex];
    public bool HasRemainingOpponents => OpponentIndex < OpponentParty.Count - 1;

    public Creature AdvanceToNextOpponent()
    {
        if (!HasRemainingOpponents)
        {
            return OpponentCreature;
        }

        OpponentIndex++;
        return OpponentCreature;
    }
}
