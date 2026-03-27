using Microsoft.Xna.Framework.Input;

namespace PyGame.Core;

public sealed class InputState
{
    private KeyboardState _previous;
    private KeyboardState _current;

    public void Update()
    {
        _previous = _current;
        _current = Keyboard.GetState();
    }

    public bool IsDown(Keys key) => _current.IsKeyDown(key);

    public bool WasPressed(Keys key) => _current.IsKeyDown(key) && !_previous.IsKeyDown(key);
}
