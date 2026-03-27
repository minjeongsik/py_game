using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyGame.Core.Audio;
using PyGame.Core.Camera;
using PyGame.Core.Input;
using PyGame.Core.Rendering;
using PyGame.GameFlow;
using PyGame.GameFlow.StateManager;
using PyGame.GameFlow.States.Bag;
using PyGame.GameFlow.States.Battle;
using PyGame.GameFlow.States.Dialogue;
using PyGame.GameFlow.States.Party;
using PyGame.GameFlow.States.Pause;
using PyGame.GameFlow.States.Shop;
using PyGame.GameFlow.States.Storage;
using PyGame.GameFlow.States.Title;
using PyGame.GameFlow.States.World;
using PyGame.Infrastructure.Content;
using PyGame.Infrastructure.Save;
using PyGame.UI.Dialogue;
using PyGame.UI.Hud;
using PyGame.UI.Menus;

namespace PyGame;

public sealed class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly InputSnapshot _input = new();
    private readonly AudioService _audio = new();
    private readonly Camera2D _camera = new();
    private readonly SaveGameService _saveGameService = new();

    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;
    private PrimitiveRenderer? _primitiveRenderer;
    private PixelTextRenderer? _textRenderer;
    private MenuRenderer? _menuRenderer;
    private DialogueBoxRenderer? _dialogueRenderer;

    private GameDefinitions _definitions = null!;
    private GameSession _session = null!;
    private GameStateManager _stateManager = null!;
    private bool _exitRequested;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = 960;
        _graphics.PreferredBackBufferHeight = 540;
    }

    protected override void Initialize()
    {
        var definitionsPath = Path.Combine(AppContext.BaseDirectory, "Content", "Definitions", "game-definitions.json");
        _definitions = GameDefinitionLoader.Load(definitionsPath);
        _session = GameSessionFactory.CreateNew(_definitions);
        _stateManager = new GameStateManager(new IGameState[]
        {
            new TitleState(),
            new WorldState(),
            new BagState(),
            new PartyState(),
            new StorageState(),
            new ShopState(),
            new DialogueState(),
            new BattleState(),
            new PauseState()
        });

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);

        _primitiveRenderer = new PrimitiveRenderer(_spriteBatch, _pixel);
        _textRenderer = new PixelTextRenderer(_spriteBatch, _pixel);
        _menuRenderer = new MenuRenderer(_textRenderer);
        _dialogueRenderer = new DialogueBoxRenderer(_primitiveRenderer, _textRenderer);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();

        var context = CreateContext();
        _stateManager.Update(gameTime, context);

        if (_exitRequested)
        {
            Exit();
            return;
        }

        Window.Title = $"몬스터 필드 | {_stateManager.CurrentId} | {context.Session.CurrentMapId}";
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 24, 30));
        _stateManager.Draw(gameTime, CreateContext());
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _dialogueRenderer = null;
        _menuRenderer = null;
        _textRenderer?.Dispose();
        _textRenderer = null;
        _primitiveRenderer = null;
        _pixel?.Dispose();
        _pixel = null;
        _spriteBatch?.Dispose();
        _spriteBatch = null;
        base.UnloadContent();
    }

    private GameContext CreateContext()
    {
        return new GameContext
        {
            Input = _input,
            Audio = _audio,
            Camera = _camera,
            PrimitiveRenderer = _primitiveRenderer!,
            TextRenderer = _textRenderer!,
            MenuRenderer = _menuRenderer!,
            DialogueRenderer = _dialogueRenderer!,
            GraphicsDevice = GraphicsDevice,
            SpriteBatch = _spriteBatch!,
            Definitions = _definitions,
            Session = _session,
            StateManager = _stateManager,
            SaveGameService = _saveGameService,
            ExitGame = () => _exitRequested = true,
            ResetSession = () => _session = GameSessionFactory.CreateNew(_definitions),
            ReplaceSession = session => _session = session
        };
    }
}
