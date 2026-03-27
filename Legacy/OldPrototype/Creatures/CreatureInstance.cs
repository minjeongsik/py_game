namespace PyGame.Creatures;

public sealed class CreatureInstance
{
    public string SpeciesId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int MaxVitality { get; set; } = 10;
    public int CurrentVitality { get; set; } = 10;
    public int Power { get; set; } = 6;
    public int Guard { get; set; } = 5;
    public int Speed { get; set; } = 5;
    public List<string> EquippedMoveIds { get; set; } = [];

    public bool IsFainted => CurrentVitality <= 0;
}
