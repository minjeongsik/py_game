using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;

    private readonly InputState _input = new();
    private readonly GameStateManager _state = new();
    private readonly Camera2D _camera = new();
    private readonly EncounterService _encounterService = new();
    private readonly SaveGameService _saveService = new();
    private readonly AudioCueService _audio = new();

    private GameContentDatabase _db = null!;
    private GameSession _session = null!;
    private BattleController _battle = null!;

    private readonly Dictionary<string, WorldMap> _mapCache = [];
    private WorldMap _activeMap = null!;

    private int _titleSelection;
    private int _pauseSelection;
    private bool _debugVisible = true;
    private float _portalCooldown;
    private string _hudMessage = "WELCOME TO AETHER TRAIL";

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
        var contentPath = Path.Combine(AppContext.BaseDirectory, "Content", "Data");
        _db = GameContentDatabase.Load(contentPath);
        _session = GameSession.CreateNew(_db);
        _battle = new BattleController(_db, _session, _encounterService);
        LoadZone(_session.CurrentZoneId, null);

        _state.ChangeState(GameStateType.Title);
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
        _input.Update();

        if (_input.WasPressed(Keys.F3))
        {
            _debugVisible = !_debugVisible;
        }

        if (_portalCooldown > 0)
        {
            _portalCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        switch (_state.CurrentState)
        {
            case GameStateType.Title:
                UpdateTitle();
                break;
            case GameStateType.WorldExploration:
                UpdateWorld(gameTime);
                break;
            case GameStateType.Battle:
                UpdateBattle();
                break;
            case GameStateType.PauseMenu:
                UpdatePause();
                break;
        }

        UpdateWindowTitle();
        base.Update(gameTime);
    }

    private void UpdateTitle()
    {
        if (_input.WasPressed(Keys.Up)) _titleSelection = (_titleSelection + 2) % 3;
        if (_input.WasPressed(Keys.Down)) _titleSelection = (_titleSelection + 1) % 3;
        if (!_input.WasPressed(Keys.Enter))
        {
            return;
        }

        if (_titleSelection == 0)
        {
            _audio.PlayConfirm();
            StartNewGame();
            return;
        }

        if (_titleSelection == 1)
        {
            _audio.PlayConfirm();
            if (!ContinueGame())
            {
                _hudMessage = "NO SAVE DATA FOUND";
            }

            return;
        }

        _audio.PlayConfirm();
        Exit();
    }

    private void UpdateWorld(GameTime gameTime)
    {
        if (_input.WasPressed(Keys.Escape))
        {
            _pauseSelection = 0;
            _state.ChangeState(GameStateType.PauseMenu);
            return;
        }

        var previous = _session.PlayerPosition;

        _playerProxy.WorldPosition = _session.PlayerPosition;
        _playerProxy.Update(gameTime, _input, _activeMap);
        _session.PlayerPosition = _playerProxy.WorldPosition;

        _camera.Follow(_session.PlayerPosition, GraphicsDevice.Viewport, _activeMap.PixelWidth, _activeMap.PixelHeight);

        if (_session.PlayerPosition != previous)
        {
            HandlePortals();
            HandleEncounters(gameTime);
        }

        if (_input.WasPressed(Keys.F5))
        {
            SaveSession();
            _hudMessage = "GAME SAVED";
        }
    }

    private void UpdateBattle()
    {
        if (!_battle.IsActive)
        {
            _state.ChangeState(GameStateType.WorldExploration);
            return;
        }

        if (_battle.MenuMode == BattleMenuMode.Root)
        {
            var options = _battle.GetRootOptions();
            if (_input.WasPressed(Keys.Up)) _battle.RootSelection = (_battle.RootSelection + options.Count - 1) % options.Count;
            if (_input.WasPressed(Keys.Down)) _battle.RootSelection = (_battle.RootSelection + 1) % options.Count;

            if (_input.WasPressed(Keys.Enter))
            {
                switch (_battle.RootSelection)
                {
                    case 0: _audio.PlayConfirm(); _battle.MenuMode = BattleMenuMode.SelectMove; break;
                    case 1: _audio.PlayConfirm(); _battle.MenuMode = BattleMenuMode.SelectItem; break;
                    case 2: _audio.PlayConfirm(); EndBattleIfDone(_battle.TryCapture()); break;
                    case 3: _audio.PlayConfirm(); EndBattleIfDone(_battle.TryRun()); break;
                }
            }

            return;
        }

        if (_battle.MenuMode == BattleMenuMode.SelectMove)
        {
            var moves = _battle.GetMoveOptions();
            if (moves.Count == 0)
            {
                _battle.MenuMode = BattleMenuMode.Root;
                return;
            }

            if (_input.WasPressed(Keys.Up)) _battle.MoveSelection = (_battle.MoveSelection + moves.Count - 1) % moves.Count;
            if (_input.WasPressed(Keys.Down)) _battle.MoveSelection = (_battle.MoveSelection + 1) % moves.Count;
            if (_input.WasPressed(Keys.Escape)) _battle.MenuMode = BattleMenuMode.Root;
            if (_input.WasPressed(Keys.Enter))
            {
                EndBattleIfDone(_battle.UseMove(_battle.MoveSelection));
                _battle.MenuMode = BattleMenuMode.Root;
            }

            return;
        }

        var items = _battle.GetItemOptions();
        if (items.Count == 0)
        {
            if (_input.WasPressed(Keys.Escape) || _input.WasPressed(Keys.Enter))
            {
                _battle.MenuMode = BattleMenuMode.Root;
            }

            return;
        }

        if (_input.WasPressed(Keys.Up)) _battle.ItemSelection = (_battle.ItemSelection + items.Count - 1) % items.Count;
        if (_input.WasPressed(Keys.Down)) _battle.ItemSelection = (_battle.ItemSelection + 1) % items.Count;
        if (_input.WasPressed(Keys.Escape)) _battle.MenuMode = BattleMenuMode.Root;
        if (_input.WasPressed(Keys.Enter))
        {
            EndBattleIfDone(_battle.UseItem(_battle.ItemSelection));
            _battle.MenuMode = BattleMenuMode.Root;
        }
    }

    private void UpdatePause()
    {
        var options = new[] { "RESUME", "SAVE", "TITLE" };
        if (_input.WasPressed(Keys.Up)) _pauseSelection = (_pauseSelection + options.Length - 1) % options.Length;
        if (_input.WasPressed(Keys.Down)) _pauseSelection = (_pauseSelection + 1) % options.Length;

        if (_input.WasPressed(Keys.Escape))
        {
            _state.ChangeState(GameStateType.WorldExploration);
            return;
        }

        if (!_input.WasPressed(Keys.Enter))
        {
            return;
        }

        switch (_pauseSelection)
        {
            case 0:
                _state.ChangeState(GameStateType.WorldExploration);
                break;
            case 1:
                _audio.PlayConfirm();
                SaveSession();
                _hudMessage = "GAME SAVED";
                _state.ChangeState(GameStateType.WorldExploration);
                break;
            case 2:
                _state.ChangeState(GameStateType.Title);
                break;
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 22, 30));

        if (_state.CurrentState is GameStateType.WorldExploration or GameStateType.Battle or GameStateType.PauseMenu)
        {
            _spriteBatch!.Begin(transformMatrix: _camera.Transform);
            WorldRenderer.DrawMap(_spriteBatch, _pixel!, _activeMap);
            WorldRenderer.DrawPlayer(_spriteBatch, _pixel!, _playerProxy);
            _spriteBatch.End();
        }

        _spriteBatch!.Begin();
        DrawUi();
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawUi()
    {
        var viewport = GraphicsDevice.Viewport;

        if (_state.CurrentState == GameStateType.Title)
        {
            DrawPanel(new Rectangle(130, 80, 700, 380), new Color(18, 26, 44, 235));
            PixelFontRenderer.DrawLines(_spriteBatch!, _pixel!, new Vector2(200, 120), ["AETHER TRAIL", "ORIGINAL CREATURE ADVENTURE"], 3, new Color(236, 242, 255));
            DrawMenu(new Vector2(260, 240), ["NEW GAME", "CONTINUE", "EXIT"], _titleSelection);
            PixelFontRenderer.DrawText(_spriteBatch!, _pixel!, _hudMessage, new Vector2(210, 410), 2, new Color(210, 220, 180));
        }

        if (_state.CurrentState == GameStateType.WorldExploration)
        {
            DrawPanel(new Rectangle(12, 12, 420, 74), new Color(8, 12, 20, 175));
            PixelFontRenderer.DrawLines(_spriteBatch!, _pixel!, new Vector2(22, 22), [$"ZONE {_activeMap.CurrentZoneName.ToUpperInvariant()}", _hudMessage], 2, Color.White);
        }

        if (_state.CurrentState == GameStateType.PauseMenu)
        {
            DrawPanel(new Rectangle(220, 120, 520, 280), new Color(14, 20, 32, 240));
            PixelFontRenderer.DrawText(_spriteBatch!, _pixel!, "PAUSE", new Vector2(420, 150), 3, Color.White);
            DrawMenu(new Vector2(320, 230), ["RESUME", "SAVE", "TITLE"], _pauseSelection);
        }

        if (_state.CurrentState == GameStateType.Battle && _battle.IsActive)
        {
            DrawPanel(new Rectangle(0, viewport.Height - 190, viewport.Width, 190), new Color(12, 16, 26, 240));
            PixelFontRenderer.DrawText(_spriteBatch!, _pixel!, _battle.BuildStatusLine(), new Vector2(18, viewport.Height - 175), 2, Color.White);
            var recent = _battle.Log.TakeLast(2).ToList();
            PixelFontRenderer.DrawLines(_spriteBatch!, _pixel!, new Vector2(18, viewport.Height - 145), recent, 2, new Color(226, 235, 210));

            if (_battle.MenuMode == BattleMenuMode.Root)
            {
                DrawMenu(new Vector2(560, viewport.Height - 160), _battle.GetRootOptions(), _battle.RootSelection);
            }
            else if (_battle.MenuMode == BattleMenuMode.SelectMove)
            {
                DrawMenu(new Vector2(560, viewport.Height - 160), _battle.GetMoveOptions(), _battle.MoveSelection);
            }
            else
            {
                var items = _battle.GetItemOptions();
                if (items.Count == 0)
                {
                    PixelFontRenderer.DrawText(_spriteBatch!, _pixel!, "NO USABLE ITEMS", new Vector2(560, viewport.Height - 150), 2, new Color(220, 188, 170));
                }
                else
                {
                    DrawMenu(new Vector2(560, viewport.Height - 160), items, _battle.ItemSelection);
                }
            }
        }

        if (_debugVisible)
        {
            DrawDebugOverlay(viewport);
        }
    }

    private void DrawDebugOverlay(Viewport viewport)
    {
        var tile = _activeMap.ToTilePosition(_session.PlayerPosition);
        DrawPanel(new Rectangle(12, viewport.Height - 118, 610, 106), new Color(0, 0, 0, 140));
        var lines = new[]
        {
            $"STATE {_state.CurrentState}",
            $"POS {_session.PlayerPosition.X:0.0},{_session.PlayerPosition.Y:0.0} TILE {tile.X},{tile.Y} ID {_activeMap.GetTileAt(tile.X, tile.Y)}",
            $"INPUT {(_playerProxy.InputDetectedThisFrame ? "YES" : "NO")} DIR {_playerProxy.LastInputDirection.X:0.00},{_playerProxy.LastInputDirection.Y:0.00} MOVED {(_playerProxy.MovedThisFrame ? "YES" : "NO")}",
            $"PARTY {_session.Party.Count} STORAGE {_session.Storage.Count} SAVE {_saveService.GetSaveFilePathForDisplay()}"
        };

        PixelFontRenderer.DrawLines(_spriteBatch!, _pixel!, new Vector2(20, viewport.Height - 110), lines, 2, new Color(226, 236, 226));
    }

    private void DrawMenu(Vector2 pos, List<string> options, int selected)
    {
        var y = pos.Y;
        for (var i = 0; i < options.Count; i++)
        {
            var marker = i == selected ? ">" : " ";
            PixelFontRenderer.DrawText(_spriteBatch!, _pixel!, $"{marker} {options[i]}", new Vector2(pos.X, y), 2, i == selected ? new Color(255, 236, 120) : Color.White);
            y += 28;
        }
    }

    private void DrawPanel(Rectangle rect, Color color)
    {
        _spriteBatch!.Draw(_pixel!, rect, color);
    }

    private readonly PlayerController _playerProxy = new(Vector2.Zero, 120f);

    private void StartNewGame()
    {
        _session = GameSession.CreateNew(_db);
        _battle = new BattleController(_db, _session, _encounterService);
        LoadZone("haven_hamlet", null);
        _state.ChangeState(GameStateType.WorldExploration);
        _hudMessage = "BEGIN YOUR JOURNEY";
    }

    private bool ContinueGame()
    {
        var loaded = _saveService.TryLoad();
        if (loaded is null)
        {
            return false;
        }

        _session = new GameSession
        {
            CurrentZoneId = loaded.ZoneId,
            PlayerPosition = loaded.PlayerPosition
        };

        _session.Party.AddRange(loaded.Party);
        _session.Storage.AddRange(loaded.Storage);
        _session.Inventory.AddRange(loaded.Inventory);

        if (_session.Party.Count == 0)
        {
            _session.Party.Add(GameSession.CreateCreatureInstance(_db, "spriglet", 5));
        }

        _battle = new BattleController(_db, _session, _encounterService);
        LoadZone(_session.CurrentZoneId, _session.PlayerPosition);
        _state.ChangeState(GameStateType.WorldExploration);
        _hudMessage = "CONTINUED SAVE";
        return true;
    }

    private void SaveSession()
    {
        var save = SaveGameData.CreateFromSession(
            _session.CurrentZoneId,
            _session.PlayerPosition,
            _session.Party.Select(CloneCreature).ToList(),
            _session.Storage.Select(CloneCreature).ToList(),
            _session.Inventory.Select(x => new InventoryEntry { ItemId = x.ItemId, Quantity = x.Quantity }).ToList());

        _saveService.Save(save);
    }

    private CreatureInstance CloneCreature(CreatureInstance source)
    {
        return new CreatureInstance
        {
            SpeciesId = source.SpeciesId,
            Nickname = source.Nickname,
            Level = source.Level,
            MaxVitality = source.MaxVitality,
            CurrentVitality = source.CurrentVitality,
            Power = source.Power,
            Guard = source.Guard,
            Speed = source.Speed,
            EquippedMoveIds = source.EquippedMoveIds.ToList()
        };
    }

    private void LoadZone(string zoneId, Vector2? playerPosition)
    {
        _session.CurrentZoneId = zoneId;
        var zone = _db.Zones[zoneId];

        if (!_mapCache.TryGetValue(zoneId, out var map))
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Content", "Data", zone.MapDataPath);
            map = WorldMapLoader.Load(path);
            _mapCache[zoneId] = map;
        }

        _activeMap = map;

        if (playerPosition.HasValue)
        {
            _session.PlayerPosition = playerPosition.Value;
        }
        else
        {
            var spawn = _activeMap.ResolveSpawnTile();
            _session.PlayerPosition = new Vector2(spawn.X * _activeMap.TileSize, spawn.Y * _activeMap.TileSize);
        }

        _playerProxy.WorldPosition = _session.PlayerPosition;
        _camera.Follow(_session.PlayerPosition, GraphicsDevice.Viewport, _activeMap.PixelWidth, _activeMap.PixelHeight);
    }

    private void HandlePortals()
    {
        if (_portalCooldown > 0)
        {
            return;
        }

        var tile = _activeMap.ToTilePosition(_session.PlayerPosition);
        var portal = _activeMap.TryGetPortalAt(tile);
        if (portal is null)
        {
            return;
        }

        LoadZone(portal.TargetZoneId, null);
        _session.PlayerPosition = new Vector2(portal.TargetX * _activeMap.TileSize, portal.TargetY * _activeMap.TileSize);
        _playerProxy.WorldPosition = _session.PlayerPosition;
        _camera.Follow(_session.PlayerPosition, GraphicsDevice.Viewport, _activeMap.PixelWidth, _activeMap.PixelHeight);
        _portalCooldown = 0.35f;
        _hudMessage = $"ARRIVED: {_activeMap.CurrentZoneName.ToUpperInvariant()}";
    }

    private void HandleEncounters(GameTime gameTime)
    {
        var zone = _db.Zones[_session.CurrentZoneId];
        if (!zone.AllowEncounters || !_activeMap.IsEncounterTileAtWorldPosition(_session.PlayerPosition) || !_playerProxy.MovedThisFrame)
        {
            return;
        }

        if (!_encounterService.RollEncounter(gameTime, zone.EncounterRatePerSecond))
        {
            return;
        }

        var entry = _encounterService.RollEncounterEntry(zone.EncounterTable);
        var level = _encounterService.RollLevel(entry.MinLevel, entry.MaxLevel);
        var wild = GameSession.CreateCreatureInstance(_db, entry.SpeciesId, level);
        _audio.PlayBattleStart();
        _battle.Start(wild);
        _state.ChangeState(GameStateType.Battle);
    }

    private void EndBattleIfDone(BattleResolution resolution)
    {
        if (!resolution.Ended)
        {
            return;
        }

        _state.ChangeState(GameStateType.WorldExploration);
        _hudMessage = resolution.Outcome switch
        {
            BattleOutcome.Victory => "WILD CREATURE DEFEATED",
            BattleOutcome.Captured => "CAPTURE SUCCESS",
            BattleOutcome.Ran => "YOU ESCAPED",
            BattleOutcome.Defeat => "RETREATED TO SAFETY",
            _ => _hudMessage
        };
    }

    private void UpdateWindowTitle()
    {
        Window.Title = $"Aether Trail | {_state.CurrentState} | Zone {_activeMap.CurrentZoneName} | Party {_session.Party.Count} | Vitals {_session.ActiveCreature.CurrentVitality}/{_session.ActiveCreature.MaxVitality}";
    }

    private void UpdateWindowTitle()
    {
        Window.Title = $"Aether Trail Prototype | {_stateManager.CurrentState} | Zone: {_worldMap.CurrentZoneName} | Pos: {_player.WorldPosition.X:0.0},{_player.WorldPosition.Y:0.0}";
    }
}
