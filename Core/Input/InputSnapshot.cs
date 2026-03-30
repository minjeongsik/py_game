using Microsoft.Xna.Framework.Input;

namespace PyGame.Core.Input;

public sealed class InputSnapshot
{
    private const float InitialRepeatDelay = 0.24f;
    private const float RepeatInterval = 0.08f;

    private KeyboardState _current;
    private KeyboardState _previous;
    private readonly Dictionary<Keys, float> _heldDurations = [];
    private readonly Dictionary<Keys, float> _repeatTimers = [];

    public void Update(float elapsedSeconds = 0f)
    {
        _previous = _current;
        _current = Keyboard.GetState();

        foreach (var key in _current.GetPressedKeys())
        {
            if (_previous.IsKeyDown(key))
            {
                _heldDurations[key] = _heldDurations.TryGetValue(key, out var held) ? held + elapsedSeconds : elapsedSeconds;
                _repeatTimers[key] = _repeatTimers.TryGetValue(key, out var timer) ? timer + elapsedSeconds : elapsedSeconds;
            }
            else
            {
                _heldDurations[key] = 0f;
                _repeatTimers[key] = 0f;
            }
        }

        var releasedKeys = _heldDurations.Keys.Where(key => !_current.IsKeyDown(key)).ToArray();
        for (var i = 0; i < releasedKeys.Length; i++)
        {
            _heldDurations.Remove(releasedKeys[i]);
            _repeatTimers.Remove(releasedKeys[i]);
        }
    }

    public bool IsDown(Keys key) => _current.IsKeyDown(key);

    public bool WasPressed(Keys key) => _current.IsKeyDown(key) && !_previous.IsKeyDown(key);

    public bool WasRepeated(Keys key)
    {
        if (WasPressed(key))
        {
            return true;
        }

        if (!_current.IsKeyDown(key) || !_repeatTimers.TryGetValue(key, out var timer))
        {
            return false;
        }

        if (timer < InitialRepeatDelay)
        {
            return false;
        }

        _repeatTimers[key] = timer - RepeatInterval;
        return true;
    }
}
