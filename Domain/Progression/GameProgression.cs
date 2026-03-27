namespace PyGame.Domain.Progression;

public sealed class GameProgression
{
    public HashSet<string> Flags { get; } = [];
    public int BadgeCount { get; set; }

    public bool HasFlag(string flag)
    {
        return !string.IsNullOrWhiteSpace(flag) && Flags.Contains(flag);
    }

    public bool TryAddFlag(string flag)
    {
        return !string.IsNullOrWhiteSpace(flag) && Flags.Add(flag);
    }

    public void Restore(IEnumerable<string> flags, int badgeCount)
    {
        Flags.Clear();
        foreach (var flag in flags)
        {
            if (!string.IsNullOrWhiteSpace(flag))
            {
                Flags.Add(flag);
            }
        }

        BadgeCount = badgeCount;
    }
}
