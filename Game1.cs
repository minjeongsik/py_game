using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyGame.Core;
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
    private Camera2D _camera = null!;
    private WorldMap _worldMap = null!;
    private PlayerController _player = null!;
    private EncounterService _encounterService = null!;
    private SaveGameService _saveGameService = null!;

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
        var spawnTile = _worldMap.ResolveSpawnTile();
        _player = new PlayerController(new Vector2(spawnTile.X * _worldMap.TileSize, spawnTile.Y * _worldMap.TileSize), 110f);
        _encounterService = new EncounterService(0.18f);
        _saveGameService = new SaveGameService();

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

        if (_inputState.WasPressed(Microsoft.Xna.Framework.Input.Keys.F5))
        {
            var save = SaveGameData.CreateFromPlayer(_player.WorldPosition, _worldMap.CurrentZoneName, [], []);
            _saveGameService.Save(save);
        }

        if (_inputState.WasPressed(Microsoft.Xna.Framework.Input.Keys.F9))
        {
            var save = _saveGameService.TryLoad();
            if (save is not null)
            {
                _player.WorldPosition = save.PlayerPosition;
            }
        }

        Window.Title = $"Aether Trail Prototype | {_stateManager.CurrentState} | Zone: {_worldMap.CurrentZoneName} | Pos: {_player.WorldPosition.X:0.0},{_player.WorldPosition.Y:0.0}";

        switch (_stateManager.CurrentState)
        {
            case GameStateType.Title:
                if (_inputState.WasPressed(Microsoft.Xna.Framework.Input.Keys.Enter))
                {
                    _stateManager.ChangeState(GameStateType.WorldExploration);
                }
                break;

            case GameStateType.WorldExploration:
                if (_inputState.WasPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
                {
                    _stateManager.ChangeState(GameStateType.PauseMenu);
                    break;
                }

                _player.Update(gameTime, _inputState, _worldMap);
                _camera.Follow(_player.WorldPosition, GraphicsDevice.Viewport, _worldMap.PixelWidth, _worldMap.PixelHeight);

                if (_worldMap.IsEncounterTileAtWorldPosition(_player.WorldPosition) && _player.MovedThisFrame)
                {
                    if (_encounterService.RollEncounter(gameTime))
                    {
                        _stateManager.ChangeState(GameStateType.EncounterOverlay);
                    }
                }
                break;

            case GameStateType.PauseMenu:
                if (_inputState.WasPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
                {
                    _stateManager.ChangeState(GameStateType.WorldExploration);
                }
                break;

            case GameStateType.EncounterOverlay:
                if (_inputState.WasPressed(Microsoft.Xna.Framework.Input.Keys.Enter))
                {
                    _stateManager.ChangeState(GameStateType.WorldExploration);
                }
                break;
        }

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
}
