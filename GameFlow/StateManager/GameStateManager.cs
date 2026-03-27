using Microsoft.Xna.Framework;

namespace PyGame.GameFlow.StateManager;

public sealed class GameStateManager
{
    private readonly Dictionary<GameStateId, IGameState> _states;

    public GameStateManager(IEnumerable<IGameState> states)
    {
        _states = states.ToDictionary(x => x.Id);
        CurrentId = GameStateId.Title;
    }

    public GameStateId CurrentId { get; private set; }

    public void ChangeState(GameStateId nextState)
    {
        CurrentId = nextState;
    }

    public void Update(GameTime gameTime, GameContext context)
    {
        _states[CurrentId].Update(gameTime, context);
    }

    public void Draw(GameTime gameTime, GameContext context)
    {
        _states[CurrentId].Draw(gameTime, context);
    }
}
