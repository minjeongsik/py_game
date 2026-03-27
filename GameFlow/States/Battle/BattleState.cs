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

    private readonly string[] _options = ["기술", "가방", "교체", "포획", "도주"];
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
                _resultMessage = "전투에서 사용할 도구를 고르세요.";
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
        DrawBattleBackdrop(context);

        if (encounter is not null)
        {
            DrawCreatureStage(context, new Rectangle(60, 70, 320, 150), encounter.OpponentCreature.SpeciesId, false);
            DrawBattleCard(
                context,
                new Rectangle(584, 42, 320, 132),
                encounter.OpponentCreature,
                enemySpecies.PrimaryTypeId,
                encounter.IsTrainerBattle ? $"트레이너 {encounter.OpponentName}" : "야생 몬스터",
                false);
        }

        DrawCreatureStage(context, new Rectangle(548, 192, 300, 166), player.SpeciesId, true);
        DrawBattleCard(context, new Rectangle(58, 234, 358, 140), player, playerSpecies.PrimaryTypeId, "내 선두", true);
        DrawMessagePanel(context, context.Session, encounter);

        if (_awaitingDismiss)
        {
            DrawHintLine(context, "Enter 또는 Space를 누르면 필드로 돌아갑니다.");
        }
        else if (_selectingSwitch)
        {
            DrawSwitchPanel(context);
            DrawHintLine(context, _forcedSwitch ? "기절했습니다. 다음 몬스터를 반드시 골라야 합니다." : "교체하면 상대가 곧바로 행동합니다.");
        }
        else if (_selectingItems)
        {
            DrawItemPanel(context);
            DrawHintLine(context, $"상처약 {context.Session.Inventory.GetQuantity("potion")}개  포획구 {context.Session.Inventory.GetQuantity("capture-sphere")}개");
        }
        else if (_selectingMoves)
        {
            DrawMovePanel(context);
            var move = _playerMoves[_moveSelection];
            DrawHintLine(context, $"{move.Name}  속성 {GetTypeLabel(move.TypeId)}  위력 {move.Power}");
        }
        else
        {
            DrawActionPanel(context);
            DrawHintLine(context, "기술, 가방, 교체, 포획, 도주 중 하나를 선택하세요.");
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
            _resultMessage = $"{encounter.OpponentCreature.Nickname}이(가) 다음 움직임을 보고 있다.";
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
            _resultMessage = $"{encounter.OpponentCreature.Nickname}이(가) 틈을 노리고 있다.";
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
            _resultMessage = $"{encounter.OpponentCreature.Nickname}이(가) 빈틈을 보고 있다.";
            return;
        }

        if (!context.Input.WasPressed(Keys.Enter) && !context.Input.WasPressed(Keys.Space))
        {
            return;
        }

        if (!context.Session.Party.CanSwitchTo(_switchSelection))
        {
            _resultMessage = "지금은 그 몬스터로 교체할 수 없습니다.";
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
            _resultMessage = $"가라, {active.Nickname}!";
            context.Session.StatusMessage = $"{active.Nickname}을(를) 내보냈다.";
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
                _resultMessage = $"{active.Nickname}은(는) 교체 직후 {encounter.OpponentCreature.Nickname}의 {enemyMove.Name}에 맞아 쓰러졌다. 다른 몬스터를 골라야 한다.";
                context.Session.StatusMessage = $"{active.Nickname}이(가) 쓰러졌다.";
                _forcedSwitch = true;
                return;
            }

            ResolveDefeat(context, $"{active.Nickname}은(는) 교체 직후 {encounter.OpponentCreature.Nickname}의 {enemyMove.Name}을(를) 버티지 못했다.");
            return;
        }

        _selectingSwitch = false;
        _resultMessage = $"{active.Nickname}을(를) 내보냈다. 하지만 {encounter.OpponentCreature.Nickname}의 {enemyMove.Name}으로 {enemyDamage} 피해를 받았다. {GetEffectText(modifier)}";
        context.Session.StatusMessage = $"{active.Nickname}을(를) 내보냈다.";
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
                context.Session.StatusMessage = $"트레이너 {encounter.OpponentName}에게 승리하고 90원을 받았다.";
            }
            else
            {
                _resultMessage = $"{leadMessage} 야생 {encounter.OpponentCreature.Nickname}이(가) 쓰러졌다.";
                context.Session.StatusMessage = $"{encounter.OpponentCreature.Nickname}을(를) 쓰러뜨렸다.";
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
            _resultMessage = "트레이너의 몬스터는 포획할 수 없습니다.";
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
                _resultMessage = $"{stored.Nickname} 포획 성공! 보관함으로 전송했다.";
                context.Session.StatusMessage = $"{stored.Nickname}을(를) 보관함으로 보냈다.";
                _awaitingDismiss = true;
                return;
            }

            var captured = CloneCreature(encounter);
            context.Session.Party.Add(captured);
            _resultMessage = $"{captured.Nickname} 포획 성공! 파티에 합류했다.";
            context.Session.StatusMessage = $"{captured.Nickname}을(를) 포획했다.";
            _awaitingDismiss = true;
            return;
        }

        EnemyTurn(context, encounter, $"{encounter.OpponentCreature.Nickname}이(가) 포획구를 튕겨 내고 버텼다.");
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
            _resultMessage = "지금 교체할 다른 몬스터가 없습니다.";
            return;
        }

        _forcedSwitch = forced;
        _selectingSwitch = true;
        _selectingMoves = false;
        _selectingItems = false;
        _switchSelection = context.Session.Party.ActiveIndex;
        _resultMessage = forced ? "몬스터가 쓰러졌습니다. 다른 몬스터를 고르세요." : "교체할 몬스터를 고르세요.";
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
                context.Session.StatusMessage = $"{player.Nickname}이(가) 쓰러졌다.";
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
        _resultMessage = $"{leadMessage} 파티 전원이 쓰러져 회복 지점으로 돌아가 모두 회복되었다.";
        context.Session.StatusMessage = "회복 지점으로 돌아왔습니다.";
        _awaitingDismiss = true;
        _selectingSwitch = false;
        _selectingMoves = false;
        _forcedSwitch = false;
    }

    private void DrawBattleBackdrop(GameContext context)
    {
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, context.Viewport.Height), new Color(206, 226, 200));
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, 264), new Color(182, 210, 176));
        context.PrimitiveRenderer.Fill(new Rectangle(0, 264, context.Viewport.Width, 276), new Color(104, 142, 104));
        context.PrimitiveRenderer.Fill(new Rectangle(52, 108, 286, 82), new Color(230, 236, 208));
        context.PrimitiveRenderer.Fill(new Rectangle(520, 270, 334, 88), new Color(224, 230, 202));
        context.PrimitiveRenderer.Fill(new Rectangle(36, 386, 888, 126), new Color(16, 24, 34, 238));
        context.PrimitiveRenderer.Outline(new Rectangle(36, 386, 888, 126), 3, new Color(208, 184, 100));
    }

    private void DrawMessagePanel(GameContext context, GameSession session, Encounter? encounter)
    {
        context.TextRenderer.DrawText(new Vector2(62, 408), _resultMessage, 2, new Color(236, 236, 224));
        context.TextRenderer.DrawText(new Vector2(62, 438), $"파티 {session.Party.Count}/{Domain.Party.Party.MaxSize}  보관함 {session.Storage.Count}  소지금 {session.Money}", 2, new Color(212, 220, 226));

        if (encounter is not null)
        {
            context.TextRenderer.DrawText(
                new Vector2(62, 468),
                encounter.IsTrainerBattle ? "트레이너전에서는 포획이 불가능합니다." : "야생전에서는 포획과 도주가 가능합니다.",
                1,
                new Color(202, 214, 222));
        }
    }

    private void DrawHintLine(GameContext context, string hint)
    {
        context.TextRenderer.DrawText(new Vector2(62, 490), hint, 1, new Color(212, 220, 226));
    }

    private void DrawActionPanel(GameContext context)
    {
        var panel = new Rectangle(646, 390, 252, 118);
        context.PrimitiveRenderer.Fill(panel, new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(panel, 3, new Color(208, 184, 108));
        context.TextRenderer.DrawText(new Vector2(panel.X + 20, panel.Y + 16), "행동 선택", 2, new Color(248, 238, 188));

        for (var i = 0; i < _options.Length; i++)
        {
            var selected = i == _selected;
            var itemRect = new Rectangle(panel.X + 16, panel.Y + 44 + (i * 13), 220, 12);
            if (selected)
            {
                context.PrimitiveRenderer.Fill(itemRect, new Color(214, 188, 108));
            }

            context.TextRenderer.DrawText(
                new Vector2(itemRect.X + 8, itemRect.Y - 4),
                _options[i],
                1,
                selected ? new Color(20, 24, 26) : new Color(220, 228, 232));
        }
    }

    private void DrawSwitchPanel(GameContext context)
    {
        var panel = new Rectangle(596, 194, 308, 192);
        context.PrimitiveRenderer.Fill(panel, new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(panel, 3, new Color(208, 184, 108));
        context.TextRenderer.DrawText(new Vector2(panel.X + 22, panel.Y + 16), "교체할 몬스터", 2, new Color(248, 238, 188));

        var y = panel.Y + 52;
        for (var i = 0; i < context.Session.Party.Members.Count; i++)
        {
            var creature = context.Session.Party.Members[i];
            var selected = i == _switchSelection;
            var active = i == context.Session.Party.ActiveIndex;
            var color = creature.IsFainted ? new Color(150, 120, 120) : selected ? new Color(250, 226, 132) : new Color(220, 228, 232);
            var prefix = active ? "선두" : creature.IsFainted ? "기절" : "대기";
            context.TextRenderer.DrawText(new Vector2(panel.X + 22, y), $"{prefix} {creature.Nickname}", 2, color);
            context.TextRenderer.DrawText(new Vector2(panel.X + 178, y), $"HP {creature.CurrentHealth}/{creature.MaxHealth}", 1, color);
            y += 28;
        }
    }

    private void DrawMovePanel(GameContext context)
    {
        var panel = new Rectangle(560, 362, 344, 146);
        context.PrimitiveRenderer.Fill(panel, new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(panel, 3, new Color(208, 184, 108));
        context.TextRenderer.DrawText(new Vector2(panel.X + 22, panel.Y + 14), "기술 선택", 2, new Color(248, 238, 188));

        var y = panel.Y + 48;
        for (var i = 0; i < _playerMoves.Length; i++)
        {
            var move = _playerMoves[i];
            var selected = i == _moveSelection;
            var color = selected ? new Color(250, 226, 132) : new Color(220, 228, 232);
            context.TextRenderer.DrawText(new Vector2(panel.X + 22, y), move.Name, 2, color);
            context.TextRenderer.DrawText(new Vector2(panel.X + 170, y), GetTypeLabel(move.TypeId), 1, color);
            context.TextRenderer.DrawText(new Vector2(panel.X + 246, y), $"위력 {move.Power}", 1, color);
            y += 28;
        }
    }

    private void DrawItemPanel(GameContext context)
    {
        var panel = new Rectangle(560, 372, 344, 116);
        context.PrimitiveRenderer.Fill(panel, new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(panel, 3, new Color(208, 184, 108));
        context.TextRenderer.DrawText(new Vector2(panel.X + 22, panel.Y + 14), "가방", 2, new Color(248, 238, 188));
        var selectedColor = _itemSelection == 0 ? new Color(250, 226, 132) : new Color(220, 228, 232);
        context.TextRenderer.DrawText(new Vector2(panel.X + 22, panel.Y + 52), "상처약", 2, selectedColor);
        context.TextRenderer.DrawText(new Vector2(panel.X + 188, panel.Y + 56), $"남은 수량 {context.Session.Inventory.GetQuantity("potion")}", 1, selectedColor);
    }

    private void DrawBattleCard(GameContext context, Rectangle rect, Creature creature, string typeId, string label, bool playerSide)
    {
        context.PrimitiveRenderer.Fill(rect, new Color(18, 26, 36, 236));
        context.PrimitiveRenderer.Outline(rect, 3, new Color(214, 188, 108));
        context.TextRenderer.DrawText(new Vector2(rect.X + 20, rect.Y + 16), label, 2, new Color(244, 236, 188));
        context.TextRenderer.DrawText(new Vector2(rect.X + 20, rect.Y + 44), creature.Nickname, 3, Color.White);
        context.TextRenderer.DrawText(new Vector2(rect.X + 20, rect.Y + 78), $"Lv {creature.Level}  속성 {GetTypeLabel(typeId)}", 2, new Color(218, 228, 234));
        DrawHpBar(
            context,
            new Rectangle(rect.X + 20, rect.Y + 108, playerSide ? 220 : 210, 12),
            creature.CurrentHealth,
            creature.MaxHealth,
            playerSide ? new Color(78, 186, 92) : new Color(212, 86, 82),
            new Color(72, 88, 62));
        context.TextRenderer.DrawText(new Vector2(rect.X + 20, rect.Y + 122), $"HP {creature.CurrentHealth}/{creature.MaxHealth}", 1, new Color(214, 224, 228));
    }

    private void DrawCreatureStage(GameContext context, Rectangle rect, string speciesId, bool playerSide)
    {
        var palette = GetEncounterPalette(speciesId);
        var stageColor = playerSide ? new Color(214, 228, 188) : new Color(228, 236, 206);
        context.PrimitiveRenderer.Fill(rect, stageColor);
        context.PrimitiveRenderer.Outline(rect, 2, new Color(124, 130, 96));

        var shadowHeight = playerSide ? 22 : 18;
        context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 34, rect.Bottom - shadowHeight, rect.Width - 68, 16), new Color(88, 104, 72, 120));

        switch (speciesId)
        {
            case "sproutle":
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 112, rect.Y + 20, 54, 40), palette.Primary);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 94, rect.Y + 52, 92, 44), palette.Secondary);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 126, rect.Y + 10, 18, 20), palette.Accent);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 108, rect.Y + 40, 10, 8), new Color(36, 42, 28));
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 152, rect.Y + 40, 10, 8), new Color(36, 42, 28));
                break;
            case "brookit":
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 90, rect.Y + 40, 102, 44), palette.Primary);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 112, rect.Y + 22, 56, 26), palette.Secondary);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 178, rect.Y + 48, 28, 18), palette.Accent);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 126, rect.Y + 44, 8, 6), new Color(28, 38, 54));
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 144, rect.Y + 44, 8, 6), new Color(28, 38, 54));
                break;
            default:
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 108, rect.Y + 18, 58, 38), palette.Primary);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 90, rect.Y + 50, 100, 50), palette.Secondary);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 176, rect.Y + 22, 24, 18), palette.Accent);
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 122, rect.Y + 34, 8, 8), new Color(44, 36, 28));
                context.PrimitiveRenderer.Fill(new Rectangle(rect.X + 146, rect.Y + 34, 8, 8), new Color(44, 36, 28));
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
        EnemyTurn(context, encounter, $"{player.Nickname}에게 상처약을 사용했다.");
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
            return "효과가 뛰어나다!";
        }

        if (modifier <= 0.8f)
        {
            return "효과가 약하다.";
        }

        return "평범하게 들어갔다.";
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
