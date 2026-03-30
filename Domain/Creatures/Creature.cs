namespace PyGame.Domain.Creatures;

public sealed class Creature
{
    public required string SpeciesId { get; init; }
    public required string Nickname { get; init; }
    public required int Level { get; set; }
    public required int MaxHealth { get; set; }
    public required int CurrentHealth { get; set; }
    public int Experience { get; set; }
    public Dictionary<string, int> MovePp { get; init; } = new(StringComparer.Ordinal);
    public bool IsFainted => CurrentHealth <= 0;

    public static Creature Create(string speciesId, string nickname, int level)
    {
        var maxHealth = CalculateMaxHealth(level);
        return new Creature
        {
            SpeciesId = speciesId,
            Nickname = nickname,
            Level = level,
            MaxHealth = maxHealth,
            CurrentHealth = maxHealth,
            Experience = 0
        };
    }

    public static int CalculateMaxHealth(int level)
    {
        return 14 + (level * 3);
    }

    public static int GetExperienceForNextLevel(int level)
    {
        return 12 + (level * 6);
    }
}
