using PyGame.Domain.Creatures;

namespace PyGame.Domain.Party;

public sealed class CreatureStorage
{
    private readonly List<Creature> _stored = [];

    public IReadOnlyList<Creature> Creatures => _stored;
    public int Count => _stored.Count;

    public void Add(Creature creature)
    {
        _stored.Add(creature);
    }

    public Creature RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _stored.Count);

        var creature = _stored[index];
        _stored.RemoveAt(index);
        return creature;
    }

    public void Restore(IReadOnlyList<Creature> creatures)
    {
        _stored.Clear();
        _stored.AddRange(creatures);
    }
}
