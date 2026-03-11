namespace PyGame.Creatures;

public sealed class CreatureSpeciesDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EssenceType { get; set; } = string.Empty;
    public int BaseVitality { get; set; }
    public int BasePower { get; set; }
    public int BaseGuard { get; set; }
    public List<string> LearnableMoveIds { get; set; } = [];
}
