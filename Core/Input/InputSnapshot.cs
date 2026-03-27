using Microsoft.Xna.Framework.Input;

namespace PyGame.Core.Input;

public sealed class InputSnapshot
{
    private KeyboardState _current;
    private KeyboardState _previous;

    public void Update()
    {
        _previous = _current;
        _current = Keyboard.GetState();
    }

    public bool IsDown(Keys key) => _current.IsKeyDown(key);

    public bool WasPressed(Keys key) => _current.IsKeyDown(key) && !_previous.IsKeyDown(key);
}
