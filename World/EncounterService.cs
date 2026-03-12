namespace PyGame.World;

public sealed class EncounterService
{
    private readonly float _chancePerSecond;
    private readonly Random _random = new();

    public EncounterService(float chancePerSecond)
    {
        _chancePerSecond = chancePerSecond;
    }

    public bool RollEncounter(Microsoft.Xna.Framework.GameTime gameTime)
    {
        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var rollThreshold = _chancePerSecond * delta;
        return _random.NextSingle() < rollThreshold;
    }
}
