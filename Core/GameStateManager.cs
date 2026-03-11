namespace PyGame.Core;

public sealed class GameStateManager
{
    public GameStateType CurrentState { get; private set; }

    public void ChangeState(GameStateType newState)
    {
        CurrentState = newState;
    }
}
