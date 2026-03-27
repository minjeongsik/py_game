namespace PyGame.Domain.Inventory;

public sealed class InventoryBag
{
    private readonly List<InventorySlot> _slots = [];

    public IReadOnlyList<InventorySlot> Slots => _slots;

    public int GetQuantity(string itemId)
    {
        var slot = _slots.FirstOrDefault(x => x.ItemId == itemId);
        return slot?.Quantity ?? 0;
    }

    public void Add(string itemId, int quantity)
    {
        var slot = _slots.FirstOrDefault(x => x.ItemId == itemId);
        if (slot is not null)
        {
            slot.Quantity += quantity;
            return;
        }

        _slots.Add(new InventorySlot { ItemId = itemId, Quantity = quantity });
    }

    public bool UseOne(string itemId)
    {
        var slot = _slots.FirstOrDefault(x => x.ItemId == itemId);
        if (slot is null || slot.Quantity <= 0)
        {
            return false;
        }

        slot.Quantity--;
        if (slot.Quantity == 0)
        {
            _slots.Remove(slot);
        }

        return true;
    }

    public void Restore(IReadOnlyList<InventorySlot> slots)
    {
        _slots.Clear();
        for (var i = 0; i < slots.Count; i++)
        {
            _slots.Add(new InventorySlot
            {
                ItemId = slots[i].ItemId,
                Quantity = slots[i].Quantity
            });
        }
    }
}
