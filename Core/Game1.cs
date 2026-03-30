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
    private RetroArtAssets? _art;
    private PixelTextRenderer? _textRenderer;
    private MenuRenderer? _menuRenderer;
    private DialogueBoxRenderer? _dialogueRenderer;
    private UiSkinRenderer? _uiSkin;

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
        ApplyQaBootMode();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);

        _primitiveRenderer = new PrimitiveRenderer(_spriteBatch, _pixel);
        _art = new RetroArtAssets(GraphicsDevice, Path.Combine(AppContext.BaseDirectory, "Content", "Sprites"));
        _textRenderer = new PixelTextRenderer(_spriteBatch, _pixel);
        _uiSkin = new UiSkinRenderer(GraphicsDevice);
        _menuRenderer = new MenuRenderer(_textRenderer, _uiSkin);
        _dialogueRenderer = new DialogueBoxRenderer(_textRenderer, _uiSkin);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        var context = CreateContext();
        _stateManager.Update(gameTime, context);

        if (_exitRequested)
        {
            Exit();
            return;
        }

        Window.Title = $"몬스터 월드 | {_stateManager.CurrentId} | {context.Session.CurrentMapId}";
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
        _uiSkin?.Dispose();
        _uiSkin = null;
        _textRenderer?.Dispose();
        _textRenderer = null;
        _art?.Dispose();
        _art = null;
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
            Art = _art!,
            TextRenderer = _textRenderer!,
            MenuRenderer = _menuRenderer!,
            DialogueRenderer = _dialogueRenderer!,
            UiSkin = _uiSkin!,
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

    private void ApplyQaBootMode()
    {
        var mode = Environment.GetEnvironmentVariable("PYGAME_QA_BOOT_MODE");
        if (string.IsNullOrWhiteSpace(mode))
        {
            return;
        }

        if (string.Equals(mode, "world", StringComparison.OrdinalIgnoreCase))
        {
            _stateManager.ChangeState(GameStateId.World);
            return;
        }

        if (!string.Equals(mode, "battle", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var species = _definitions.Species["embercub"];
        _session.ActiveEncounter = new Domain.Battle.Encounter
        {
            IsTrainerBattle = false,
            OpponentName = "야생",
            OpponentParty = [Domain.Creatures.Creature.Create(species.Id, species.Name, 5)]
        };
        _session.ReturnState = GameStateId.World;
        _session.StatusMessage = "QA 전투 부트 모드";
        _stateManager.ChangeState(GameStateId.Battle);
    }
}
