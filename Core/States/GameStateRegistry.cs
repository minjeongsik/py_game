namespace PyGame.Core.States;

public sealed class GameStateRegistry
{
    private readonly Dictionary<GameStateType, IGameState> _states;

    public GameStateRegistry(IEnumerable<IGameState> states)
    {
        _states = states.ToDictionary(state => state.Type);
    }

    public IGameState Get(GameStateType type)
    {
        return _states[type];
    }
}
