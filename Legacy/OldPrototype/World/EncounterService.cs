using PyGame.Data;

namespace PyGame.World;

public sealed class EncounterService
{
    private readonly Random _random = new();

    public bool RollEncounter(Microsoft.Xna.Framework.GameTime gameTime, float chancePerSecond)
    {
        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var rollThreshold = chancePerSecond * delta;
        return _random.NextSingle() < rollThreshold;
    }

    public EncounterEntryDefinition RollEncounterEntry(List<EncounterEntryDefinition> table)
    {
        var total = table.Sum(x => x.Weight);
        var roll = _random.Next(0, total);
        var cumulative = 0;

        foreach (var entry in table)
        {
            cumulative += entry.Weight;
            if (roll < cumulative)
            {
                return entry;
            }
        }

        return table.Last();
    }

    public int RollLevel(int min, int max)
    {
        return _random.Next(min, max + 1);
    }

    public bool RollCapture(float chance)
    {
        return _random.NextSingle() <= chance;
    }

    public int NextDamageVariance() => _random.Next(0, 4);

    public bool RollRunSuccess(int playerSpeed, int enemySpeed)
    {
        var chance = Math.Clamp(0.45f + ((playerSpeed - enemySpeed) * 0.03f), 0.15f, 0.95f);
        return _random.NextSingle() < chance;
    }
}
