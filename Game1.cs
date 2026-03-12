using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyGame.Core;
using PyGame.Core.States;
using PyGame.Data;
using PyGame.UI;
using PyGame.World;

namespace PyGame;

public sealed class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;

    private InputState _inputState = null!;
    private GameStateManager _stateManager = null!;
    private GameStateRegistry _stateRegistry = null!;
    private GameStateContext _stateContext = null!;
    private Camera2D _camera = null!;
    private WorldMap _worldMap = null!;
    private PlayerController _player = null!;

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
        _inputState = new InputState();
        _stateManager = new GameStateManager();
        _camera = new Camera2D();

        var contentPath = Path.Combine(AppContext.BaseDirectory, "Content", "Data");
        _worldMap = WorldMapLoader.Load(Path.Combine(contentPath, "world_map.json"));
        _player = new PlayerController(new Vector2(_worldMap.PlayerSpawnX * _worldMap.TileSize, _worldMap.PlayerSpawnY * _worldMap.TileSize), 110f);

        _stateRegistry = new GameStateRegistry([
            new TitleState(),
            new WorldExplorationState(),
            new PauseMenuState(),
            new EncounterOverlayState()
        ]);

        _stateContext = new GameStateContext
        {
            Input = _inputState,
            StateManager = _stateManager,
            Camera = _camera,
            WorldMap = _worldMap,
            Player = _player,
            EncounterService = new EncounterService(0.18f),
            SaveGameService = new SaveGameService(),
            GetViewport = () => GraphicsDevice.Viewport
        };

        _stateManager.ChangeState(GameStateType.Title);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
    }

    protected override void Update(GameTime gameTime)
    {
        _inputState.Update();
        UpdateWindowTitle();

        var activeState = _stateRegistry.Get(_stateManager.CurrentState);
        activeState.Update(gameTime, _stateContext);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 22, 30));
        _spriteBatch!.Begin(transformMatrix: _camera.Transform);

        if (_stateManager.CurrentState is GameStateType.WorldExploration or GameStateType.PauseMenu or GameStateType.EncounterOverlay)
        {
            WorldRenderer.DrawMap(_spriteBatch, _pixel!, _worldMap);
            WorldRenderer.DrawPlayer(_spriteBatch, _pixel!, _player);
        }

        _spriteBatch.End();

        _spriteBatch.Begin();
        UiRenderer.DrawOverlay(_spriteBatch, _pixel!, GraphicsDevice.Viewport, _stateManager.CurrentState, _player.WorldPosition, _worldMap.CurrentZoneName);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void UpdateWindowTitle()
    {
        Window.Title = $"Aether Trail Prototype | {_stateManager.CurrentState} | Zone: {_worldMap.CurrentZoneName} | Pos: {_player.WorldPosition.X:0.0},{_player.WorldPosition.Y:0.0}";
    }
}
