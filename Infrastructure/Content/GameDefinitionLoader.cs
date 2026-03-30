namespace PyGame.Infrastructure.Content;

public static class GameDefinitionLoader
{
    public static GameDefinitions Load(string path)
    {
        _ = path;
        var definitions = PrototypeDefinitionFactory.Create();
        GameDefinitionValidator.Validate(definitions);
        return definitions;
    }
}
