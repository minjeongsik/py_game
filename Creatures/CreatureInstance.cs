namespace PyGame.Creatures;

public sealed class CreatureInstance
{
    public string SpeciesId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int CurrentVitality { get; set; } = 10;
    public List<string> EquippedMoveIds { get; set; } = [];
}
