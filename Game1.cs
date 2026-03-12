using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyGame.Battle;
using PyGame.Core;
using PyGame.Creatures;
using PyGame.Data;
using PyGame.UI;
using PyGame.World;

namespace PyGame;

public sealed class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly Random _random = new();

    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;

    private InputState _inputState = null!;
    private GameStateManager _stateManager = null!;
    private Camera2D _camera = null!;
    private WorldMap _worldMap = null!;
    private PlayerController _player = null!;
    private EncounterService _encounterService = null!;
    private SaveGameService _saveGameService = null!;
    private JsonDataStore _dataStore = null!;
    private BattleController _battleController = null!;

    private List<CreatureInstance> _playerParty = [];

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
        _dataStore = new JsonDataStore(contentPath);
        _worldMap = WorldMapLoader.Load(Path.Combine(contentPath, "world_map.json"));
        _player = new PlayerController(new Vector2(_worldMap.PlayerSpawnX * _worldMap.TileSize, _worldMap.PlayerSpawnY * _worldMap.TileSize), 110f);
        _encounterService = new EncounterService(0.18f);
        _saveGameService = new SaveGameService();
        _battleController = new BattleController();

        InitializePlayerParty();

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
            var save = SaveGameData.CreateFromPlayer(_player.WorldPosition, "starter_vale", _playerParty, []);
            _saveGameService.Save(save);
        }

        if (_inputState.WasPressed(Microsoft.Xna.Framework.Input.Keys.F9))
        {
            var save = _saveGameService.TryLoad();
            if (save is not null)
            {
                _player.WorldPosition = save.PlayerPosition;
                _playerParty = save.Party.Count > 0 ? save.Party : _playerParty;
            }
        }

        Window.Title = BuildWindowTitle();

        switch (_stateManager.CurrentState)
        {
            case GameStateType.Title:
                if (_inputState.WasPressed(Microsoft.Xna.Framework.Input.Keys.Enter))
                {
                    _stateManager.ChangeState(GameStateType.WorldExploration);
                }
                break;

            case GameStateType.WorldExploration:
                UpdateWorldExploration(gameTime);
                break;

            case GameStateType.PauseMenu:
                if (_inputState.WasPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
                {
                    _stateManager.ChangeState(GameStateType.WorldExploration);
                }
                break;

            case GameStateType.BattleState:
                UpdateBattle();
                break;
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 22, 30));
        _spriteBatch!.Begin(transformMatrix: _camera.Transform);

        if (_stateManager.CurrentState is GameStateType.WorldExploration or GameStateType.PauseMenu)
        {
            WorldRenderer.DrawMap(_spriteBatch, _pixel!, _worldMap);
            WorldRenderer.DrawPlayer(_spriteBatch, _pixel!, _player);
        }

        _spriteBatch.End();

        _spriteBatch.Begin();
        UiRenderer.DrawOverlay(_spriteBatch, _pixel!, GraphicsDevice.Viewport, _stateManager.CurrentState, _player.WorldPosition, _worldMap.CurrentZoneName, _battleController);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void UpdateWorldExploration(GameTime gameTime)
    {
        if (_inputState.WasPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
        {
            _stateManager.ChangeState(GameStateType.PauseMenu);
            return;
        }

        _player.Update(gameTime, _inputState, _worldMap);
        _camera.Follow(_player.WorldPosition, GraphicsDevice.Viewport, _worldMap.PixelWidth, _worldMap.PixelHeight);

        if (_worldMap.IsEncounterTileAtWorldPosition(_player.WorldPosition) && _player.MovedThisFrame && _encounterService.RollEncounter(gameTime))
        {
            StartWildBattle();
        }
    }

    private void UpdateBattle()
    {
        _battleController.Update(_inputState);

        if (_battleController.IsBattleFinished && _inputState.WasPressed(Microsoft.Xna.Framework.Input.Keys.Enter))
        {
            _stateManager.ChangeState(GameStateType.WorldExploration);
        }
    }

    private void StartWildBattle()
    {
        var playerCreature = _playerParty[0];
        var playerSpecies = _dataStore.SpeciesById[playerCreature.SpeciesId];
        var wildCreature = BuildWildCreature();
        var wildSpecies = _dataStore.SpeciesById[wildCreature.SpeciesId];

        _battleController.Start(playerCreature, playerSpecies, wildCreature, wildSpecies, _dataStore.MovesById.ToDictionary(x => x.Key, x => x.Value));
        _stateManager.ChangeState(GameStateType.BattleState);
    }

    private CreatureInstance BuildWildCreature()
    {
        var zone = _dataStore.ZonesById["starter_vale"];
        var speciesId = zone.EncounterSpeciesPool[_random.Next(zone.EncounterSpeciesPool.Count)];
        var species = _dataStore.SpeciesById[speciesId];

        return new CreatureInstance
        {
            SpeciesId = speciesId,
            Nickname = species.Name,
            Level = _random.Next(2, 5),
            CurrentVitality = species.BaseVitality,
            EquippedMoveIds = species.LearnableMoveIds.Take(2).ToList()
        };
    }

    private void InitializePlayerParty()
    {
        _playerParty =
        [
            new CreatureInstance
            {
                SpeciesId = "emberlynx",
                Nickname = "Scout Emberlynx",
                Level = 5,
                CurrentVitality = 35,
                EquippedMoveIds = ["pulse_hit", "flare_bite"]
            }
        ];
    }

    private string BuildWindowTitle()
    {
        if (_stateManager.CurrentState == GameStateType.BattleState)
        {
            var commands = _battleController.GetCommandLabels();
            return $"Aether Trail | Battle | [{_battleController.SelectedCommandIndex}] {commands[0]} / {commands[1]} / {commands[2]} | {_battleController.LastMessage}";
        }

        return $"Aether Trail Prototype | {_stateManager.CurrentState} | Zone: {_worldMap.CurrentZoneName} | Pos: {_player.WorldPosition.X:0.0},{_player.WorldPosition.Y:0.0}";
    }
}
