using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PyGame.Domain.Battle;
using PyGame.Domain.Creatures;
using PyGame.GameFlow.StateManager;

namespace PyGame.GameFlow.States.Battle;

public sealed class BattleState : IGameState
{
    private static readonly MoveDefinition[] FallbackMoves =
    [
        new MoveDefinition { Id = "tackle", Name = "몸통박치기", TypeId = "neutral", Power = 5 }
    ];

    private readonly string[] _options = ["기술", "도구", "교체", "포획", "도주"];
    private int _selected;
    private int _switchSelection;
    private int _moveSelection;
    private int _itemSelection;
    private bool _awaitingDismiss;
    private bool _selectingSwitch;
    private bool _selectingMoves;
    private bool _selectingItems;
    private bool _forcedSwitch;
    private string _resultMessage = string.Empty;
    private Encounter? _boundEncounter;
    private Creature? _boundActiveCreature;
    private MoveDefinition[] _playerMoves = FallbackMoves;
    private MoveDefinition[] _enemyMoves = FallbackMoves;

    public GameStateId Id => GameStateId.Battle;

    public void Update(GameTime gameTime, GameContext context)
    {
        _ = gameTime;

        var encounter = context.Session.ActiveEncounter;
        if (encounter is null)
        {
            ResetLocalState();
            context.StateManager.ChangeState(context.Session.ReturnState);
            return;
        }

        if (!ReferenceEquals(_boundEncounter, encounter))
        {
            _boundEncounter = encounter;
            _selected = 0;
            _switchSelection = 0;
            _moveSelection = 0;
            _itemSelection = 0;
            _awaitingDismiss = false;
            _selectingSwitch = false;
            _selectingMoves = false;
            _selectingItems = false;
            _forcedSwitch = false;
            _resultMessage = encounter.IsTrainerBattle
                ? $"트레이너 {encounter.OpponentName}이(가) 승부를 걸어왔다!"
                : $"야생 {encounter.OpponentCreature.Nickname}이(가) 나타났다!";
            BindMoveSets(context, encounter);
        }
        else if (!ReferenceEquals(_boundActiveCreature, context.Session.Party.ActiveCreature))
        {
            BindMoveSets(context, encounter);
        }

        if (_awaitingDismiss)
        {
            if (context.Input.WasPressed(Keys.Enter) || context.Input.WasPressed(Keys.Space) || context.Input.WasPressed(Keys.Escape))
            {
                context.Session.ActiveEncounter = null;
                ResetLocalState();
                context.StateManager.ChangeState(context.Session.ReturnState);
            }

            return;
        }

        if (_selectingSwitch)
        {
            UpdateSwitchSelection(context, encounter);
            return;
        }

        if (_selectingMoves)
        {
            UpdateMoveSelection(context, encounter);
            return;
        }

        if (_selectingItems)
        {
            UpdateItemSelection(context, encounter);
            return;
        }

        if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W))
        {
            _selected = (_selected + _options.Length - 1) % _options.Length;
        }

        if (context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S))
        {
            _selected = (_selected + 1) % _options.Length;
        }

        if (context.Input.WasPressed(Keys.Escape))
        {
            ResolveRun(context, encounter);
            return;
        }

        if (!context.Input.WasPressed(Keys.Enter) && !context.Input.WasPressed(Keys.Space))
        {
            return;
        }

        switch (_selected)
        {
            case 0:
                _selectingMoves = true;
                _moveSelection = Math.Clamp(_moveSelection, 0, _playerMoves.Length - 1);
                _resultMessage = "사용할 기술을 고르세요.";
                break;
            case 1:
                _selectingItems = true;
                _itemSelection = 0;
                _resultMessage = "사용할 도구를 고르세요.";
                break;
            case 2:
                OpenSwitchMenu(context, false);
                break;
            case 3:
                ResolveCapture(context, encounter);
                break;
            default:
                ResolveRun(context, encounter);
                break;
        }
    }

    public void Draw(GameTime gameTime, GameContext context)
    {
        _ = gameTime;

        var encounter = context.Session.ActiveEncounter;
        var player = context.Session.Party.ActiveCreature;
        var playerSpecies = context.Definitions.Species[player.SpeciesId];
        var enemySpecies = encounter is null ? playerSpecies : context.Definitions.Species[encounter.OpponentCreature.SpeciesId];

        context.SpriteBatch.Begin();
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, context.Viewport.Height), new Color(204, 226, 198));
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, 236), new Color(186, 212, 178));
        context.PrimitiveRenderer.Fill(new Rectangle(0, 236, context.Viewport.Width, 304), new Color(116, 152, 110));
        context.PrimitiveRenderer.Fill(new Rectangle(58, 74, 308, 128), new Color(232, 238, 210));
        context.PrimitiveRenderer.Fill(new Rectangle(548, 44, 324, 150), new Color(22, 30, 40, 238));
        context.PrimitiveRenderer.Outline(new Rectangle(548, 44, 324, 150), 3, new Color(214, 188, 108));
        context.PrimitiveRenderer.Fill(new Rectangle(566, 196, 246, 94), new Color(224, 232, 202));
        context.PrimitiveRenderer.Fill(new Rectangle(88, 252, 360, 136), new Color(22, 30, 40, 238));
        context.PrimitiveRenderer.Outline(new Rectangle(88, 252, 360, 136), 3, new Color(214, 188, 108));

        if (encounter is not null)
        {
            DrawCreatureSprite(context, new Rectangle(132, 88, 150, 98), encounter.OpponentCreature.SpeciesId, false);
            DrawBattleCard(context, new Rectangle(548, 44, 324, 150), encounter.OpponentCreature, enemySpecies.PrimaryTypeId, encounter.IsTrainerBattle ? "트레이너 배틀" : "야생 배틀", false);
        }

        DrawCreatureSprite(context, new Rectangle(248, 190, 168, 116), player.SpeciesId, true);
        DrawBattleCard(context, new Rectangle(88, 252, 360, 136), player, playerSpecies.PrimaryTypeId, "내 선두", true);

        context.PrimitiveRenderer.Fill(new Rectangle(24, 350, 912, 166), new Color(18, 24, 34, 240));
        context.PrimitiveRenderer.Outline(new Rectangle(24, 350, 912, 166), 3, new Color(208, 184, 100));
        context.TextRenderer.DrawText(new Vector2(50, 376), _resultMessage, 2, new Color(236, 236, 224));
        context.TextRenderer.DrawText(new Vector2(50, 412), $"파티 {context.Session.Party.Count}/{Domain.Party.Party.MaxSize}  박스 {context.Session.Storage.Count}", 2, new Color(212, 220, 226));

        if (_awaitingDismiss)
        {
            context.TextRenderer.DrawText(new Vector2(50, 470), "엔터를 누르면 월드로 돌아갑니다.", 2, new Color(212, 220, 226));
        }
        else if (_selectingSwitch)
        {
            DrawSwitchPanel(context);
            context.TextRenderer.DrawText(new Vector2(50, 470), _forcedSwitch ? "계속 싸울 크리처를 고르세요." : "교체하면 상대가 한 번 더 행동합니다.", 2, new Color(212, 220, 226));
        }
        else if (_selectingItems)
        {
            DrawItemPanel(context);
            context.TextRenderer.DrawText(new Vector2(50, 470), $"상처약 X{context.Session.Inventory.GetQuantity("potion")}  포획구 X{context.Session.Inventory.GetQuantity("capture-sphere")}", 2, new Color(212, 220, 226));
        }
        else if (_selectingMoves)
        {
            DrawMovePanel(context);
            var move = _playerMoves[_moveSelection];
            context.TextRenderer.DrawText(new Vector2(50, 470), $"{move.Name}  타입 {GetTypeLabel(move.TypeId)}  위력 {move.Power}", 2, new Color(212, 220, 226));
        }
        else
        {
            context.MenuRenderer.Draw(new Vector2(650, 390), _options, _selected, 3);
            context.TextRenderer.DrawText(new Vector2(50, 470), "기술로 공격하고, 도구로 회복하며, 포획구로 야생 크리처를 잡을 수 있습니다.", 2, new Color(212, 220, 226));
        }

        context.SpriteBatch.End();
    }

    private void UpdateMoveSelection(GameContext context, Encounter encounter)
    {
        if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W))
        {
            _moveSelection = (_moveSelection + _playerMoves.Length - 1) % _playerMoves.Length;
        }

        if (context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S))
        {
            _moveSelection = (_moveSelection + 1) % _playerMoves.Length;
        }

        if (context.Input.WasPressed(Keys.Escape))
        {
            _selectingMoves = false;
            _resultMessage = $"{encounter.OpponentCreature.Nickname}이(가) 지켜보고 있다.";
            return;
        }

        if (!context.Input.WasPressed(Keys.Enter) && !context.Input.WasPressed(Keys.Space))
        {
            return;
        }

        _selectingMoves = false;
        ResolveMoveAttack(context, encounter, _playerMoves[_moveSelection]);
    }

    private void UpdateItemSelection(GameContext context, Encounter encounter)
    {
        if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W) ||
            context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S))
        {
            _itemSelection = 0;
        }

        if (context.Input.WasPressed(Keys.Escape))
        {
            _selectingItems = false;
            _resultMessage = $"{encounter.OpponentCreature.Nickname}이(가) 지켜보고 있다.";
            return;
        }

        if (!context.Input.WasPressed(Keys.Enter) && !context.Input.WasPressed(Keys.Space))
        {
            return;
        }

        _selectingItems = false;
        UseBattlePotion(context, encounter);
    }

    private void UpdateSwitchSelection(GameContext context, Encounter encounter)
    {
        var party = context.Session.Party.Members;
        _switchSelection = Math.Clamp(_switchSelection, 0, Math.Max(0, party.Count - 1));

        if (context.Input.WasPressed(Keys.Up) || context.Input.WasPressed(Keys.W))
        {
            _switchSelection = (_switchSelection + party.Count - 1) % party.Count;
        }

        if (context.Input.WasPressed(Keys.Down) || context.Input.WasPressed(Keys.S))
        {
            _switchSelection = (_switchSelection + 1) % party.Count;
        }

        if (!_forcedSwitch && context.Input.WasPressed(Keys.Escape))
        {
            _selectingSwitch = false;
            _resultMessage = $"{encounter.OpponentCreature.Nickname}이(가) 전투를 준비하고 있다.";
            return;
        }

        if (!context.Input.WasPressed(Keys.Enter) && !context.Input.WasPressed(Keys.Space))
        {
            return;
        }

        if (!context.Session.Party.CanSwitchTo(_switchSelection))
        {
            _resultMessage = "지금은 그 크리처로 교체할 수 없습니다.";
            return;
        }

        context.Audio.PlayConfirm();
        context.Session.Party.SwitchTo(_switchSelection);
        BindMoveSets(context, encounter);
        var active = context.Session.Party.ActiveCreature;

        if (_forcedSwitch)
        {
            _forcedSwitch = false;
            _selectingSwitch = false;
            _resultMessage = $"가라! {active.Nickname}!";
            context.Session.StatusMessage = $"{active.Nickname}을(를) 내보냈습니다";
            return;
        }

        var enemyMove = ChooseEnemyMove();
        var playerSpecies = context.Definitions.Species[active.SpeciesId];
        var modifier = TypeChart.GetModifier(enemyMove.TypeId, playerSpecies.PrimaryTypeId);
        var enemyDamage = CalculateDamage(encounter.OpponentCreature.Level, active.Level, enemyMove.Power, modifier);
        active.CurrentHealth = Math.Max(0, active.CurrentHealth - enemyDamage);

        if (active.IsFainted)
        {
            if (context.Session.Party.HasSwitchOption())
            {
                _resultMessage = $"{active.Nickname}이(가) 교체 직후 {encounter.OpponentCreature.Nickname}의 {enemyMove.Name}에 {enemyDamage} 피해를 받고 쓰러졌다. 다른 크리처를 고르세요.";
                context.Session.StatusMessage = $"{active.Nickname}이(가) 쓰러졌습니다";
                _forcedSwitch = true;
                return;
            }

            ResolveDefeat(context, $"{active.Nickname}이(가) 교체 직후 {encounter.OpponentCreature.Nickname}의 {enemyMove.Name}에 버티지 못했다.");
            return;
        }

        _selectingSwitch = false;
        _resultMessage = $"{active.Nickname}이(가) 나왔다! {encounter.OpponentCreature.Nickname}의 {enemyMove.Name}에 {enemyDamage} 피해를 입었다. {GetEffectText(modifier)}";
        context.Session.StatusMessage = $"{active.Nickname}을(를) 내보냈습니다";
    }

    private void ResolveMoveAttack(GameContext context, Encounter encounter, MoveDefinition move)
    {
        var player = context.Session.Party.ActiveCreature;
        var enemySpecies = context.Definitions.Species[encounter.OpponentCreature.SpeciesId];
        var modifier = TypeChart.GetModifier(move.TypeId, enemySpecies.PrimaryTypeId);
        var damage = CalculateDamage(player.Level, encounter.OpponentCreature.Level, move.Power, modifier);
        context.Audio.PlayConfirm();

        encounter.OpponentCreature.CurrentHealth = Math.Max(0, encounter.OpponentCreature.CurrentHealth - damage);
        var leadMessage = $"{player.Nickname}의 {move.Name}! {damage} 피해. {GetEffectText(modifier)}";

        if (encounter.OpponentCreature.IsFainted)
        {
            if (encounter.IsTrainerBattle)
            {
                context.Session.Progression.TryAddFlag(encounter.TrainerDefeatedFlag);
                context.Session.Progression.TryAddFlag(encounter.TrainerVictoryFlag);
                context.Session.Money += 90;
                _resultMessage = $"{leadMessage} 트레이너 {encounter.OpponentName}을(를) 이겼다!";
                context.Session.StatusMessage = $"트레이너 {encounter.OpponentName}에게 승리하고 90원을 받았습니다";
            }
            else
            {
                _resultMessage = $"{leadMessage} 야생 {encounter.OpponentCreature.Nickname}이(가) 쓰러졌다.";
                context.Session.StatusMessage = $"{encounter.OpponentCreature.Nickname}을(를) 쓰러뜨렸습니다";
            }

            _awaitingDismiss = true;
            return;
        }

        EnemyTurn(context, encounter, leadMessage);
    }

    private void ResolveCapture(GameContext context, Encounter encounter)
    {
        context.Audio.PlayConfirm();

        if (encounter.IsTrainerBattle)
        {
            _resultMessage = "트레이너의 크리처는 포획할 수 없습니다.";
            context.Session.StatusMessage = _resultMessage;
            _awaitingDismiss = true;
            return;
        }

        if (!context.Session.Inventory.UseOne("capture-sphere"))
        {
            _resultMessage = "포획구가 필요합니다.";
            return;
        }

        var healthRatio = 1f - ((float)encounter.OpponentCreature.CurrentHealth / encounter.OpponentCreature.MaxHealth);
        var chance = Math.Clamp(0.35f + (healthRatio * 0.4f) + (0.03f * (5 - encounter.OpponentCreature.Level)), 0.25f, 0.88f);
        if (Random.Shared.NextDouble() <= chance)
        {
            if (context.Session.Party.IsFull)
            {
                var stored = CloneCreature(encounter);
                context.Session.Storage.Add(stored);
                _resultMessage = $"{stored.Nickname} 포획 성공! 보관함으로 보냈습니다.";
                context.Session.StatusMessage = $"{stored.Nickname}을(를) 보관함으로 보냈습니다";
                _awaitingDismiss = true;
                return;
            }

            var captured = CloneCreature(encounter);
            context.Session.Party.Add(captured);
            _resultMessage = $"{captured.Nickname} 포획 성공! 파티에 합류했습니다.";
            context.Session.StatusMessage = $"{captured.Nickname}을(를) 포획했습니다";
            _awaitingDismiss = true;
            return;
        }

        EnemyTurn(context, encounter, $"{encounter.OpponentCreature.Nickname}이(가) 포획구를 뿌리치고 나왔다!");
    }

    private void ResolveRun(GameContext context, Encounter encounter)
    {
        context.Audio.PlayConfirm();
        _resultMessage = $"{encounter.OpponentCreature.Nickname}에게서 무사히 빠져나왔다.";
        context.Session.StatusMessage = _resultMessage;
        _awaitingDismiss = true;
    }

    private void OpenSwitchMenu(GameContext context, bool forced)
    {
        if (!context.Session.Party.HasSwitchOption())
        {
            _resultMessage = "지금 교체할 다른 크리처가 없습니다.";
            return;
        }

        _forcedSwitch = forced;
        _selectingSwitch = true;
        _selectingMoves = false;
        _selectingItems = false;
        _switchSelection = context.Session.Party.ActiveIndex;
        _resultMessage = forced ? "크리처가 쓰러졌습니다. 다른 크리처를 고르세요." : "교체할 크리처를 고르세요.";
    }

    private void EnemyTurn(GameContext context, Encounter encounter, string leadMessage)
    {
        var player = context.Session.Party.ActiveCreature;
        var enemyMove = ChooseEnemyMove();
        var playerSpecies = context.Definitions.Species[player.SpeciesId];
        var modifier = TypeChart.GetModifier(enemyMove.TypeId, playerSpecies.PrimaryTypeId);
        var damage = CalculateDamage(encounter.OpponentCreature.Level, player.Level, enemyMove.Power, modifier);
        player.CurrentHealth = Math.Max(0, player.CurrentHealth - damage);

        var counterMessage = $"{encounter.OpponentCreature.Nickname}의 {enemyMove.Name}! {damage} 피해. {GetEffectText(modifier)}";
        if (player.IsFainted)
        {
            if (context.Session.Party.HasSwitchOption())
            {
                _resultMessage = $"{leadMessage} {counterMessage} {player.Nickname}이(가) 쓰러졌다.";
                context.Session.StatusMessage = $"{player.Nickname}이(가) 쓰러졌습니다";
                OpenSwitchMenu(context, true);
                return;
            }

            ResolveDefeat(context, $"{leadMessage} {counterMessage}");
            return;
        }

        _resultMessage = $"{leadMessage} {counterMessage}";
        context.Session.StatusMessage = _resultMessage;
    }

    private void ResolveDefeat(GameContext context, string leadMessage)
    {
        context.Session.Party.RecoverAfterDefeat();
        context.Session.CurrentMapId = context.Session.RecoveryMapId;
        context.Session.PlayerTilePosition = context.Session.RecoveryTilePosition;
        context.Session.FacingDirection = new Point(0, 1);
        context.Session.ReturnState = GameStateId.World;
        _resultMessage = $"{leadMessage} 파티 전원이 한계에 닿았다. 회복 지점으로 돌아가 모두 회복했다.";
        context.Session.StatusMessage = "회복 지점으로 돌아왔습니다";
        _awaitingDismiss = true;
        _selectingSwitch = false;
        _selectingMoves = false;
        _forcedSwitch = false;
    }

    private void DrawSwitchPanel(GameContext context)
    {
        context.PrimitiveRenderer.Fill(new Rectangle(598, 214, 284, 188), new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(new Rectangle(598, 214, 284, 188), 3, new Color(208, 184, 108));
        context.TextRenderer.DrawText(new Vector2(626, 230), "교체", 3, new Color(248, 238, 188));

        var y = 270;
        for (var i = 0; i < context.Session.Party.Members.Count; i++)
        {
            var creature = context.Session.Party.Members[i];
            var selected = i == _switchSelection;
            var active = i == context.Session.Party.ActiveIndex;
            var color = creature.IsFainted ? new Color(150, 120, 120) : selected ? new Color(250, 226, 132) : new Color(220, 228, 232);
            var label = active ? "*" : creature.IsFainted ? "X" : " ";
            context.TextRenderer.DrawText(new Vector2(620, y), $"{label} {creature.Nickname}", 2, color);
            y += 28;
        }
    }

    private void DrawMovePanel(GameContext context)
    {
        context.PrimitiveRenderer.Fill(new Rectangle(586, 368, 318, 136), new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(new Rectangle(586, 368, 318, 136), 3, new Color(208, 184, 108));
        context.TextRenderer.DrawText(new Vector2(612, 382), "기술", 3, new Color(248, 238, 188));

        var y = 422;
        for (var i = 0; i < _playerMoves.Length; i++)
        {
            var move = _playerMoves[i];
            var selected = i == _moveSelection;
            var color = selected ? new Color(250, 226, 132) : new Color(220, 228, 232);
            context.TextRenderer.DrawText(new Vector2(612, y), move.Name, 2, color);
            context.TextRenderer.DrawText(new Vector2(770, y), GetTypeLabel(move.TypeId), 2, color);
            y += 28;
        }
    }

    private void DrawItemPanel(GameContext context)
    {
        context.PrimitiveRenderer.Fill(new Rectangle(586, 368, 318, 108), new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(new Rectangle(586, 368, 318, 108), 3, new Color(208, 184, 108));
        context.TextRenderer.DrawText(new Vector2(612, 382), "도구", 3, new Color(248, 238, 188));
        context.TextRenderer.DrawText(new Vector2(612, 422), "상처약", 2, _itemSelection == 0 ? new Color(250, 226, 132) : new Color(220, 228, 232));
    }

    private void DrawBattleCard(GameContext context, Rectangle rect, Creature creature, string typeId, string label, bool playerSide)
    {
        context.TextRenderer.DrawText(new Vector2(rect.X + 28, rect.Y + 24), label, 2, new Color(244, 236, 188));
        context.TextRenderer.DrawText(new Vector2(rect.X + 28, rect.Y + 56), creature.Nickname, 3, Color.White);
        context.TextRenderer.DrawText(new Vector2(rect.X + 28, rect.Y + 88), $"Lv {creature.Level}  타입 {GetTypeLabel(typeId)}", 2, new Color(218, 228, 234));
        DrawHpBar(context, new Rectangle(rect.X + 28, rect.Y + 116, playerSide ? 220 : 210, 12), creature.CurrentHealth, creature.MaxHealth, playerSide ? new Color(78, 186, 92) : new Color(212, 86, 82), new Color(72, 88, 62));
        context.TextRenderer.DrawText(new Vector2(rect.X + 28, rect.Y + 132), $"{creature.CurrentHealth}/{creature.MaxHealth}", 2, new Color(214, 224, 228));
    }

    private void DrawCreatureSprite(GameContext context, Rectangle rect, string speciesId, bool playerSide)
    {
        var palette = GetEncounterPalette(speciesId);
        var shadowHeight = playerSide ? 20 : 16;
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 26, rect.Bottom - shadowHeight, rect.Width - 52, 14), new Color(88, 104, 72, 120));

        switch (speciesId)
        {
            case "sproutle":
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 44, rect.Y + 18, 44, 36), palette.Primary);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 30, rect.Y + 48, 74, 34), palette.Secondary);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 56, rect.Y + 8, 14, 18), palette.Accent);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 42, rect.Y + 34, 10, 8), new Color(36, 42, 28));
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 80, rect.Y + 34, 10, 8), new Color(36, 42, 28));
                break;
            case "brookit":
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 30, rect.Y + 30, 78, 38), palette.Primary);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 48, rect.Y + 16, 40, 24), palette.Secondary);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 88, rect.Y + 38, 24, 16), palette.Accent);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 58, rect.Y + 32, 8, 6), new Color(28, 38, 54));
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 74, rect.Y + 32, 8, 6), new Color(28, 38, 54));
                break;
            default:
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 42, rect.Y + 18, 46, 34), palette.Primary);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 28, rect.Y + 46, 78, 38), palette.Secondary);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 90, rect.Y + 22, 22, 18), palette.Accent);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 54, rect.Y + 30, 8, 8), new Color(44, 36, 28));
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 72, rect.Y + 30, 8, 8), new Color(44, 36, 28));
                break;
        }
    }

    private void BindMoveSets(GameContext context, Encounter encounter)
    {
        _boundActiveCreature = context.Session.Party.ActiveCreature;
        _playerMoves = ResolveMoves(context, _boundActiveCreature.SpeciesId);
        _enemyMoves = ResolveMoves(context, encounter.OpponentCreature.SpeciesId);
        _moveSelection = Math.Clamp(_moveSelection, 0, _playerMoves.Length - 1);
    }

    private MoveDefinition[] ResolveMoves(GameContext context, string speciesId)
    {
        if (!context.Definitions.Species.TryGetValue(speciesId, out var species) || species.MoveIds.Count == 0)
        {
            return FallbackMoves;
        }

        var moves = new MoveDefinition[species.MoveIds.Count];
        for (var i = 0; i < species.MoveIds.Count; i++)
        {
            moves[i] = context.Definitions.Moves.TryGetValue(species.MoveIds[i], out var move)
                ? move
                : FallbackMoves[0];
        }

        return moves;
    }

    private MoveDefinition ChooseEnemyMove()
    {
        return _enemyMoves[Random.Shared.Next(_enemyMoves.Length)];
    }

    private void UseBattlePotion(GameContext context, Encounter encounter)
    {
        var item = context.Definitions.Items["potion"];
        var player = context.Session.Party.ActiveCreature;
        if (player.CurrentHealth >= player.MaxHealth)
        {
            _resultMessage = $"{player.Nickname}은(는) 이미 체력이 가득합니다.";
            return;
        }

        if (!context.Session.Inventory.UseOne(item.Id))
        {
            _resultMessage = "상처약이 없습니다.";
            return;
        }

        player.CurrentHealth = Math.Min(player.MaxHealth, player.CurrentHealth + item.HealAmount);
        EnemyTurn(context, encounter, $"{player.Nickname}에게 {item.Name}을(를) 사용했다.");
    }

    private static int CalculateDamage(int attackerLevel, int defenderLevel, int power, float modifier)
    {
        var baseDamage = power + Random.Shared.Next(1, 4) + Math.Max(0, attackerLevel - defenderLevel);
        return Math.Max(1, (int)MathF.Round(baseDamage * modifier));
    }

    private static string GetTypeLabel(string typeId)
    {
        return typeId switch
        {
            "leaf" => "풀",
            "flame" => "불꽃",
            "wave" => "물결",
            _ => "무속성"
        };
    }

    private static string GetEffectText(float modifier)
    {
        if (modifier >= 1.2f)
        {
            return "효과가 굉장했다!";
        }

        if (modifier <= 0.8f)
        {
            return "효과가 별로였다.";
        }

        return "무난하게 적중했다.";
    }

    private static void DrawHpBar(GameContext context, Rectangle rect, int current, int max, Color fill, Color back)
    {
        context.PrimitiveRenderer.Fill(rect, back);
        context.PrimitiveRenderer.Outline(rect, 1, new Color(24, 28, 24));
        var safeMax = Math.Max(1, max);
        var width = Math.Max(0, (int)((current / (float)safeMax) * (rect.Width - 2)));
        if (width > 0)
        {
            context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 1, rect.Y + 1, width, rect.Height - 2), fill);
        }
    }

    private static Creature CloneCreature(Encounter encounter)
    {
        var source = encounter.OpponentCreature;
        return new Creature
        {
            SpeciesId = source.SpeciesId,
            Nickname = source.Nickname,
            Level = source.Level,
            MaxHealth = source.MaxHealth,
            CurrentHealth = source.CurrentHealth
        };
    }

    private static EncounterPalette GetEncounterPalette(string speciesId)
    {
        return speciesId switch
        {
            "sproutle" => new EncounterPalette(new Color(82, 150, 78), new Color(136, 196, 106), new Color(224, 230, 134)),
            "brookit" => new EncounterPalette(new Color(76, 128, 190), new Color(112, 186, 220), new Color(226, 242, 252)),
            _ => new EncounterPalette(new Color(178, 108, 66), new Color(226, 168, 92), new Color(242, 224, 154))
        };
    }

    private void ResetLocalState()
    {
        _boundEncounter = null;
        _boundActiveCreature = null;
        _awaitingDismiss = false;
        _selectingSwitch = false;
        _selectingMoves = false;
        _selectingItems = false;
        _forcedSwitch = false;
        _selected = 0;
        _switchSelection = 0;
        _moveSelection = 0;
        _itemSelection = 0;
        _playerMoves = FallbackMoves;
        _enemyMoves = FallbackMoves;
        _resultMessage = string.Empty;
    }

    private readonly record struct EncounterPalette(Color Primary, Color Secondary, Color Accent);
}
