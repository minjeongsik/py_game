using PyGame.Domain.Creatures;

namespace PyGame.Domain.Party;

public sealed class Party
{
    public const int MaxSize = 6;

    private readonly List<Creature> _members = [];
    private int _activeIndex;

    public IReadOnlyList<Creature> Members => _members;
    public int Count => _members.Count;
    public bool IsFull => _members.Count >= MaxSize;
    public int ActiveIndex => _activeIndex;
    public Creature ActiveCreature => _members[_activeIndex];

    public bool Add(Creature creature)
    {
        if (IsFull)
        {
            return false;
        }

        _members.Add(creature);
        return true;
    }

    public Creature RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _members.Count);

        var creature = _members[index];
        _members.RemoveAt(index);

        if (_members.Count == 0)
        {
            _activeIndex = 0;
            return creature;
        }

        if (_activeIndex > index)
        {
            _activeIndex--;
        }
        else if (_activeIndex >= _members.Count)
        {
            _activeIndex = _members.Count - 1;
        }

        return creature;
    }

    public bool HasUsableCreature()
    {
        for (var i = 0; i < _members.Count; i++)
        {
            if (!_members[i].IsFainted)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasSwitchOption()
    {
        for (var i = 0; i < _members.Count; i++)
        {
            if (i != _activeIndex && !_members[i].IsFainted)
            {
                return true;
            }
        }

        return false;
    }

    public bool CanSwitchTo(int index)
    {
        return index >= 0 && index < _members.Count && index != _activeIndex && !_members[index].IsFainted;
    }

    public bool SwitchTo(int index)
    {
        if (!CanSwitchTo(index))
        {
            return false;
        }

        _activeIndex = index;
        return true;
    }

    public bool SetLead(int index)
    {
        if (index < 0 || index >= _members.Count)
        {
            return false;
        }

        if (index == 0)
        {
            _activeIndex = 0;
            return true;
        }

        var creature = _members[index];
        _members.RemoveAt(index);
        _members.Insert(0, creature);
        _activeIndex = 0;
        return true;
    }

    public bool EnsureUsableLead()
    {
        if (_members.Count == 0)
        {
            _activeIndex = 0;
            return false;
        }

        if (!_members[_activeIndex].IsFainted)
        {
            return true;
        }

        for (var i = 0; i < _members.Count; i++)
        {
            if (!_members[i].IsFainted)
            {
                _activeIndex = i;
                return true;
            }
        }

        return false;
    }

    public void RecoverAfterDefeat()
    {
        HealAll();
        _activeIndex = 0;
    }

    public void HealAll()
    {
        foreach (var member in _members)
        {
            member.CurrentHealth = member.MaxHealth;
        }
    }

    public void Restore(IReadOnlyList<Creature> creatures, int activeIndex)
    {
        _members.Clear();
        _members.AddRange(creatures);
        if (_members.Count == 0)
        {
            _activeIndex = 0;
            return;
        }

        _activeIndex = Math.Clamp(activeIndex, 0, _members.Count - 1);
    }
}
