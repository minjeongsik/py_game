namespace PyGame.Data;

public sealed class ItemDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int HealAmount { get; set; }
    public float CapturePower { get; set; }
}
