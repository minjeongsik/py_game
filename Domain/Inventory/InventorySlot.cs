namespace PyGame.Domain.Inventory;

public sealed class InventorySlot
{
    public required string ItemId { get; init; }
    public int Quantity { get; set; }
}
