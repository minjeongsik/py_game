using PyGame.Creatures;

namespace PyGame.Battle;

public sealed class BattleCreature
{
    public BattleCreature(CreatureInstance instance, CreatureSpeciesDefinition species)
    {
        Instance = instance;
        Species = species;
        MaxVitality = species.BaseVitality + (instance.Level * 4);
        if (instance.CurrentVitality <= 0)
        {
            instance.CurrentVitality = MaxVitality;
        }

        instance.CurrentVitality = Math.Clamp(instance.CurrentVitality, 1, MaxVitality);
    }

    public CreatureInstance Instance { get; }
    public CreatureSpeciesDefinition Species { get; }
    public int MaxVitality { get; }

    public int CurrentVitality
    {
        get => Instance.CurrentVitality;
        set => Instance.CurrentVitality = Math.Clamp(value, 0, MaxVitality);
    }

    public int Power => Species.BasePower + Instance.Level;
    public int Guard => Species.BaseGuard + Instance.Level;
    public int Speed => Species.BaseSpeed + Instance.Level;
    public bool IsFainted => CurrentVitality <= 0;
}
