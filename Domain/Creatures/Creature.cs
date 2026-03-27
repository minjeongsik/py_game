namespace PyGame.Domain.Creatures;

public sealed class Creature
{
    public required string SpeciesId { get; init; }
    public required string Nickname { get; init; }
    public required int Level { get; init; }
    public required int MaxHealth { get; init; }
    public required int CurrentHealth { get; set; }
    public bool IsFainted => CurrentHealth <= 0;

    public static Creature Create(string speciesId, string nickname, int level)
    {
        var maxHealth = 14 + (level * 3);
        return new Creature
        {
            SpeciesId = speciesId,
            Nickname = nickname,
            Level = level,
            MaxHealth = maxHealth,
            CurrentHealth = maxHealth
        };
    }
}
