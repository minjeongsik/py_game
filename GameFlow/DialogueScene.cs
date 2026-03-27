namespace PyGame.GameFlow;

public sealed class DialogueScene
{
    private readonly IReadOnlyList<string> _lines;

    public DialogueScene(string speaker, IReadOnlyList<string> lines)
    {
        Speaker = speaker;
        _lines = lines;
    }

    public string Speaker { get; }
    public int Index { get; private set; }
    public string CurrentLine => _lines[Math.Clamp(Index, 0, _lines.Count - 1)];

    public bool Advance()
    {
        if (Index >= _lines.Count - 1)
        {
            return false;
        }

        Index += 1;
        return true;
    }
}
