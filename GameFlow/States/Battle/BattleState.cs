using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PyGame.Domain.Battle;
using PyGame.Domain.Creatures;
using PyGame.Domain.Inventory;
using PyGame.GameFlow.StateManager;

namespace PyGame.GameFlow.States.Battle;

public sealed class BattleState : IGameState
{
    private readonly string[] _options = BattleText.Options;
    private int _selected;
    private int _switchSelection;
    private int _moveSelection;
    private int _itemSelection;
    private int _itemTargetSelection;
    private bool _awaitingDismiss;
    private bool _selectingSwitch;
    private bool _selectingMoves;
    private bool _selectingItems;
    private bool _selectingItemTarget;
    private bool _forcedSwitch;
    private bool _returnAfterDismiss;
    private string _resultMessage = string.Empty;
    private Encounter? _boundEncounter;
    private Creature? _boundActiveCreature;
    private MoveDefinition[] _playerMoves = [];
    private MoveDefinition[] _enemyMoves = [];
    private float _enemyImpactTimer;
    private float _playerImpactTimer;
    private float _effectFlashTimer;
    private float _playerEntryTimer;
    private float _enemyEntryTimer;
    private float _superGauge;
    private Color _effectFlashColor = Color.White;

    public GameStateId Id => GameStateId.Battle;

    public void Update(GameTime gameTime, GameContext context)
    {
        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _enemyImpactTimer = Math.Max(0f, _enemyImpactTimer - elapsed);
        _playerImpactTimer = Math.Max(0f, _playerImpactTimer - elapsed);
        _effectFlashTimer = Math.Max(0f, _effectFlashTimer - elapsed);
        _playerEntryTimer = Math.Max(0f, _playerEntryTimer - elapsed);
        _enemyEntryTimer = Math.Max(0f, _enemyEntryTimer - elapsed);

        var encounter = context.Session.ActiveEncounter;
        if (encounter is null)
        {
            ResetLocalState();
            context.StateManager.ChangeState(context.Session.ReturnState);
            return;
        }

        if (!ReferenceEquals(_boundEncounter, encounter))
        {
            BindEncounter(context, encounter);
        }
        else if (!ReferenceEquals(_boundActiveCreature, context.Session.Party.ActiveCreature))
        {
            BindMoveSets(context, encounter);
        }

        if (_awaitingDismiss)
        {
            if (IsConfirmPressed(context))
            {
                if (_returnAfterDismiss)
                {
                    context.Session.Party.RecoverAfterDefeat(context.Definitions.Moves);
                    context.Session.CurrentMapId = context.Session.RecoveryMapId;
                    context.Session.PlayerTilePosition = context.Session.RecoveryTilePosition;
                    context.Session.StatusMessage = "기절해서 마지막 회복 지점으로 돌아갑니다.";
                }

                context.Session.ActiveEncounter = null;
                ResetLocalState();
                context.StateManager.ChangeState(context.Session.ReturnState);
            }

            return;
        }

        if (_selectingMoves)
        {
            UpdateMoveSelection(context, encounter);
            return;
        }

        if (_selectingItems)
        {
            UpdateItemSelection(context);
            return;
        }

        if (_selectingItemTarget)
        {
            UpdateItemTargetSelection(context, encounter);
            return;
        }

        if (_selectingSwitch)
        {
            UpdateSwitchSelection(context, encounter);
            return;
        }

        if (context.Session.Party.ActiveCreature.IsFainted)
        {
            OpenSwitchMenu(context, true);
            return;
        }

        if (IsUpPressed(context))
        {
            _selected = (_selected + _options.Length - 1) % _options.Length;
        }

        if (IsDownPressed(context))
        {
            _selected = (_selected + 1) % _options.Length;
        }

        if (context.Input.WasPressed(Keys.Escape))
        {
            ResolveRun(context, encounter);
            return;
        }

        if (!IsConfirmPressed(context))
        {
            return;
        }

        switch (_selected)
        {
            case 0:
                _selectingMoves = true;
                _resultMessage = "사용할 기술을 고르세요.";
                break;
            case 1:
                _selectingItems = true;
                _itemSelection = 0;
                _resultMessage = "회복 아이템을 고르세요.";
                break;
            case 2:
                OpenSwitchMenu(context, false);
                break;
            case 3:
                ResolveSuperMove(context, encounter);
                break;
            case 4:
                ResolveCapture(context, encounter);
                break;
            default:
                ResolveRun(context, encounter);
                break;
        }
    }

    public void Draw(GameTime gameTime, GameContext context)
    {
        var battleTime = (float)gameTime.TotalGameTime.TotalSeconds;
        var encounter = context.Session.ActiveEncounter;
        var player = context.Session.Party.ActiveCreature;
        var enemy = encounter?.OpponentCreature;

        context.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawBackdrop(context);
        DrawBattleHints(context);

        if (enemy is not null)
        {
            var enemyBob = GetBattleBobOffset(battleTime, 0.6f);
            var enemyEntryOffset = GetEntryOffset(_enemyEntryTimer, 112);
            DrawCreatureShadow(context, new Rectangle(688, 226, 132, 18), _enemyImpactTimer);
            DrawCreatureSprite(
                context,
                enemy.SpeciesId,
                false,
                new Rectangle(664 + ImpactOffset(_enemyImpactTimer) + enemyEntryOffset, 92 + enemyBob, 164, 164),
                _enemyImpactTimer);
            DrawCard(
                context,
                new Rectangle(470, 30, 230, 78),
                enemy,
                encounter!.IsTrainerBattle ? encounter.OpponentName : "야생",
                false);
        }

        var playerBob = GetBattleBobOffset(battleTime, 0f);
        var playerEntryOffset = GetEntryOffset(_playerEntryTimer, 124);
        DrawCreatureShadow(context, new Rectangle(124, 390, 172, 20), _playerImpactTimer);
        DrawCreatureSprite(
            context,
            player.SpeciesId,
            true,
            new Rectangle(88 - ImpactOffset(_playerImpactTimer) - playerEntryOffset, 214 + playerBob, 198, 198),
            _playerImpactTimer);
        DrawCard(context, new Rectangle(56, 250, 328, 96), player, "우리 선두", true);
        DrawMessage(context, encounter);

        if (_selectingMoves)
        {
            DrawMoves(context);
        }
        else if (_selectingItems)
        {
            DrawItems(context);
        }
        else if (_selectingItemTarget)
        {
            DrawTargets(context);
        }
        else if (_selectingSwitch)
        {
            DrawSwitches(context);
        }
        else
        {
            DrawActions(context);
        }

        if (_effectFlashTimer > 0f)
        {
            var alpha = (byte)Math.Clamp((int)(_effectFlashTimer * 420f), 0, 116);
            context.PrimitiveRenderer.Fill(new Rectangle(0, 0, context.Viewport.Width, context.Viewport.Height), new Color(_effectFlashColor.R, _effectFlashColor.G, _effectFlashColor.B, alpha));
        }

        context.SpriteBatch.End();
    }

    private void BindEncounter(GameContext context, Encounter encounter)
    {
        _boundEncounter = encounter;
        _selected = 0;
        _switchSelection = 0;
        _moveSelection = 0;
        _itemSelection = 0;
        _itemTargetSelection = 0;
        _awaitingDismiss = false;
        _selectingSwitch = false;
        _selectingMoves = false;
        _selectingItems = false;
        _selectingItemTarget = false;
        _forcedSwitch = false;
        _returnAfterDismiss = false;
        _enemyImpactTimer = 0f;
        _playerImpactTimer = 0f;
        _effectFlashTimer = 0f;
        _playerEntryTimer = 0.36f;
        _enemyEntryTimer = 0.36f;
        _superGauge = 0f;
        _effectFlashColor = Color.White;
        _resultMessage = encounter.IsTrainerBattle
            ? $"{encounter.OpponentName}이(가) 현장 대결을 신청했다!"
            : $"야생 {encounter.OpponentCreature.Nickname}이(가) 나타났다!";
        BindMoveSets(context, encounter);
    }

    private void BindMoveSets(GameContext context, Encounter encounter)
    {
        _boundActiveCreature = context.Session.Party.ActiveCreature;
        _playerMoves = BattleMoveHelper.ResolveMoves(context, _boundActiveCreature);
        _enemyMoves = BattleMoveHelper.ResolveMoves(context, encounter.OpponentCreature);
        _moveSelection = Math.Clamp(_moveSelection, 0, Math.Max(0, _playerMoves.Length - 1));
        _switchSelection = Math.Clamp(_switchSelection, 0, Math.Max(0, context.Session.Party.Count - 1));
    }

    private void UpdateMoveSelection(GameContext context, Encounter encounter)
    {
        if (IsUpPressed(context))
        {
            _moveSelection = (_moveSelection + _playerMoves.Length - 1) % _playerMoves.Length;
        }

        if (IsDownPressed(context))
        {
            _moveSelection = (_moveSelection + 1) % _playerMoves.Length;
        }

        if (context.Input.WasPressed(Keys.Escape))
        {
            _selectingMoves = false;
            _resultMessage = "기술 선택을 취소했습니다.";
            return;
        }

        if (!IsConfirmPressed(context))
        {
            return;
        }

        var move = _playerMoves[_moveSelection];
        var player = context.Session.Party.ActiveCreature;
        _selectingMoves = false;

        if (BattleMoveHelper.GetCurrentPp(player, move) <= 0)
        {
            _resultMessage = $"{move.Name}의 PP가 모두 떨어졌습니다.";
            context.Session.StatusMessage = _resultMessage;
            return;
        }

        BattleMoveHelper.SpendPp(player, move);
        if (!BattleMoveHelper.CheckAccuracy(move))
        {
            EnemyTurn(context, encounter, $"{player.Nickname}의 {move.Name}! 하지만 빗나갔다.");
            return;
        }

        var enemyType = context.Definitions.Species[encounter.OpponentCreature.SpeciesId].PrimaryTypeId;
        var modifier = TypeChart.GetModifier(move.TypeId, enemyType);
        var damage = BattleMoveHelper.CalculateDamage(player.Level, encounter.OpponentCreature.Level, move.Power, modifier);
        encounter.OpponentCreature.CurrentHealth = Math.Max(0, encounter.OpponentCreature.CurrentHealth - damage);
        AddSuperGauge(24f);
        TriggerEnemyImpact(move.TypeId);
        var lead = $"{player.Nickname}의 {move.Name}! {damage} 피해. {BattleText.EffectText(modifier)}";

        if (encounter.OpponentCreature.IsFainted)
        {
            ResolveEnemyDefeat(context, encounter, lead);
            return;
        }

        EnemyTurn(context, encounter, lead);
    }

    private void UpdateItemSelection(GameContext context)
    {
        var items = BattleMoveHelper.GetBattleUsableItems(context);
        if (items.Count == 0)
        {
            if (context.Input.WasPressed(Keys.Escape) || IsConfirmPressed(context))
            {
                _selectingItems = false;
                _resultMessage = "사용 가능한 회복 아이템이 없습니다.";
            }

            return;
        }

        if (IsUpPressed(context))
        {
            _itemSelection = (_itemSelection + items.Count - 1) % items.Count;
        }

        if (IsDownPressed(context))
        {
            _itemSelection = (_itemSelection + 1) % items.Count;
        }

        if (context.Input.WasPressed(Keys.Escape))
        {
            _selectingItems = false;
            _resultMessage = "가방을 닫습니다.";
            return;
        }

        if (!IsConfirmPressed(context))
        {
            return;
        }

        _selectingItems = false;
        _selectingItemTarget = true;
        _itemTargetSelection = context.Session.Party.ActiveIndex;
        _resultMessage = $"{items[_itemSelection].Name}을(를) 사용할 대상을 고르세요.";
    }

    private void UpdateItemTargetSelection(GameContext context, Encounter encounter)
    {
        var members = context.Session.Party.Members;
        if (IsUpPressed(context))
        {
            _itemTargetSelection = (_itemTargetSelection + members.Count - 1) % members.Count;
        }

        if (IsDownPressed(context))
        {
            _itemTargetSelection = (_itemTargetSelection + 1) % members.Count;
        }

        if (context.Input.WasPressed(Keys.Escape))
        {
            _selectingItemTarget = false;
            _selectingItems = true;
            _resultMessage = "아이템 선택으로 돌아갑니다.";
            return;
        }

        if (!IsConfirmPressed(context))
        {
            return;
        }

        var items = BattleMoveHelper.GetBattleUsableItems(context);
        if (items.Count == 0)
        {
            _selectingItemTarget = false;
            _resultMessage = "사용 가능한 회복 아이템이 없습니다.";
            return;
        }

        var item = items[Math.Clamp(_itemSelection, 0, items.Count - 1)];
        var target = members[_itemTargetSelection];
        _selectingItemTarget = false;

        if (target.CurrentHealth >= target.MaxHealth)
        {
            _resultMessage = $"{target.Nickname}의 체력은 이미 가득합니다.";
            return;
        }

        if (!context.Session.Inventory.UseOne(item.Id))
        {
            _resultMessage = $"{item.Name}이(가) 부족합니다.";
            return;
        }

        target.CurrentHealth = Math.Min(target.MaxHealth, target.CurrentHealth + item.HealAmount);
        _effectFlashTimer = 0.12f;
        EnemyTurn(context, encounter, $"{target.Nickname}에게 {item.Name}을(를) 사용했다.");
    }

    private void UpdateSwitchSelection(GameContext context, Encounter encounter)
    {
        var party = context.Session.Party;
        if (IsUpPressed(context))
        {
            _switchSelection = (_switchSelection + party.Count - 1) % party.Count;
        }

        if (IsDownPressed(context))
        {
            _switchSelection = (_switchSelection + 1) % party.Count;
        }

        if (!_forcedSwitch && context.Input.WasPressed(Keys.Escape))
        {
            _selectingSwitch = false;
            _resultMessage = "교체를 취소했습니다.";
            return;
        }

        if (!IsConfirmPressed(context))
        {
            return;
        }

        if (!party.CanSwitchTo(_switchSelection))
        {
            _resultMessage = "그 동료는 지금 교체할 수 없습니다.";
            return;
        }

        party.SwitchTo(_switchSelection);
        _selectingSwitch = false;
        var switchedName = party.ActiveCreature.Nickname;
        BindMoveSets(context, encounter);

        if (_forcedSwitch)
        {
            _forcedSwitch = false;
            _resultMessage = $"{switchedName}을(를) 다시 전장으로 내보냈다.";
            return;
        }

        EnemyTurn(context, encounter, $"{switchedName}을(를) 전장으로 교체했다.");
    }

    private void ResolveEnemyDefeat(GameContext context, Encounter encounter, string leadMessage)
    {
        var message = $"{leadMessage} {encounter.OpponentCreature.Nickname}이(가) 쓰러졌다.";
        if (encounter.IsTrainerBattle && encounter.HasRemainingOpponents)
        {
            var next = encounter.AdvanceToNextOpponent();
            BindMoveSets(context, encounter);
            _effectFlashTimer = 0.14f;
            _resultMessage = $"{message} {encounter.OpponentName}은(는) {next.Nickname}을(를) 내보냈다.";
            return;
        }

        var rewardText = BuildAwardExperienceMessage(context, encounter, message);
        if (encounter.IsTrainerBattle)
        {
            context.Session.Progression.TryAddFlag(encounter.TrainerDefeatedFlag);
            context.Session.Progression.TryAddFlag(encounter.TrainerVictoryFlag);
            context.Session.StatusMessage = $"{encounter.OpponentName}과의 공식 대결에서 이겼다!";
            _resultMessage = $"{rewardText} 공식 대결에서 이겼다!";
        }
        else
        {
            context.Session.StatusMessage = "야생 몬스터와의 전투를 마쳤다.";
            _resultMessage = rewardText;
        }

        _awaitingDismiss = true;
    }

    private void ResolveSuperMove(GameContext context, Encounter encounter)
    {
        if (_superGauge < 100f)
        {
            _resultMessage = "필살기 게이지가 아직 가득 차지 않았습니다.";
            return;
        }

        var player = context.Session.Party.ActiveCreature;
        var playerType = context.Definitions.Species[player.SpeciesId].PrimaryTypeId;
        var enemyType = context.Definitions.Species[encounter.OpponentCreature.SpeciesId].PrimaryTypeId;
        var modifier = TypeChart.GetModifier(playerType, enemyType);
        var damage = BattleMoveHelper.CalculateDamage(player.Level + 4, encounter.OpponentCreature.Level, 15, modifier);
        encounter.OpponentCreature.CurrentHealth = Math.Max(0, encounter.OpponentCreature.CurrentHealth - damage);
        _superGauge = 0f;
        TriggerEnemyImpact(playerType, 0.18f);
        var lead = $"{player.Nickname}의 {BattleText.SuperMoveName(playerType)}! {damage} 피해. {BattleText.EffectText(modifier)}";

        if (encounter.OpponentCreature.IsFainted)
        {
            ResolveEnemyDefeat(context, encounter, lead);
            return;
        }

        EnemyTurn(context, encounter, lead);
    }

    private void ResolveCapture(GameContext context, Encounter encounter)
    {
        if (encounter.IsTrainerBattle)
        {
            _resultMessage = "공식 대결 중인 상대 개체는 포획할 수 없습니다.";
            return;
        }

        const string captureItemId = "capture-sphere";
        if (!context.Definitions.Items.TryGetValue(captureItemId, out var sphere))
        {
            _resultMessage = "포획구 정의를 찾지 못했습니다.";
            return;
        }

        if (!context.Session.Inventory.UseOne(captureItemId))
        {
            _resultMessage = $"{sphere.Name}이(가) 부족합니다.";
            return;
        }

        var enemy = encounter.OpponentCreature;
        var healthRatio = enemy.MaxHealth <= 0 ? 1f : (float)enemy.CurrentHealth / enemy.MaxHealth;
        var chance = Math.Clamp(0.28f + ((1f - healthRatio) * 0.6f), 0.2f, 0.88f);
        _effectFlashTimer = 0.18f;

        if (Random.Shared.NextSingle() > chance)
        {
            EnemyTurn(context, encounter, $"{sphere.Name}을(를) 던졌지만 포획에 실패했다.");
            return;
        }

        var captured = BattleMoveHelper.CloneCreature(enemy);
        if (context.Session.Party.IsFull)
        {
            context.Session.Storage.Add(captured);
            _resultMessage = $"{captured.Nickname}을(를) 포획했다! 파티가 가득 차서 PC 보관함으로 보냈다.";
        }
        else
        {
            context.Session.Party.Add(captured);
            _resultMessage = $"{captured.Nickname}을(를) 포획했다! 파티에 합류했다.";
        }

        context.Session.StatusMessage = $"{captured.Nickname} 포획 성공.";
        _awaitingDismiss = true;
    }

    private void ResolveRun(GameContext context, Encounter encounter)
    {
        if (encounter.IsTrainerBattle)
        {
            _resultMessage = "공식 대결에서는 물러날 수 없습니다.";
            return;
        }

        context.Session.StatusMessage = "전투에서 무사히 벗어났다.";
        _resultMessage = "무사히 빠져나왔다.";
        _awaitingDismiss = true;
    }

    private void OpenSwitchMenu(GameContext context, bool forced)
    {
        if (!context.Session.Party.HasSwitchOption())
        {
            if (forced)
            {
                ResolveDefeat(context, "더 이상 내보낼 수 있는 동료가 없다.");
            }
            else
            {
                _resultMessage = "교체할 수 있는 동료가 없습니다.";
            }

            return;
        }

        _selectingSwitch = true;
        _forcedSwitch = forced;
        _switchSelection = FindFirstSwitchOption(context);
        _resultMessage = forced ? "다음으로 보낼 동료를 고르세요." : "교체할 동료를 고르세요.";
    }

    private void EnemyTurn(GameContext context, Encounter encounter, string leadMessage)
    {
        if (encounter.OpponentCreature.IsFainted)
        {
            _resultMessage = leadMessage;
            return;
        }

        var enemy = encounter.OpponentCreature;
        var enemyMove = BattleMoveHelper.ChooseEnemyMove(enemy, _enemyMoves);
        if (enemyMove is null)
        {
            _resultMessage = $"{leadMessage} 하지만 상대는 움직이지 못했다.";
            return;
        }

        BattleMoveHelper.SpendPp(enemy, enemyMove);
        if (!BattleMoveHelper.CheckAccuracy(enemyMove))
        {
            _resultMessage = $"{leadMessage} {enemy.Nickname}의 {enemyMove.Name}! 하지만 빗나갔다.";
            return;
        }

        var player = context.Session.Party.ActiveCreature;
        var playerType = context.Definitions.Species[player.SpeciesId].PrimaryTypeId;
        var modifier = TypeChart.GetModifier(enemyMove.TypeId, playerType);
        var damage = BattleMoveHelper.CalculateDamage(enemy.Level, player.Level, enemyMove.Power, modifier);
        player.CurrentHealth = Math.Max(0, player.CurrentHealth - damage);
        AddSuperGauge(12f);
        TriggerPlayerImpact(enemyMove.TypeId);

        var attackText = $"{enemy.Nickname}의 {enemyMove.Name}! {damage} 피해. {BattleText.EffectText(modifier)}";
        if (!player.IsFainted)
        {
            _resultMessage = $"{leadMessage} {attackText}";
            return;
        }

        var faintText = $"{leadMessage} {attackText} {player.Nickname}이(가) 쓰러졌다.";
        if (context.Session.Party.HasSwitchOption())
        {
            _resultMessage = $"{faintText} 다음 동료를 골라야 한다.";
            OpenSwitchMenu(context, true);
            return;
        }

        ResolveDefeat(context, faintText);
    }

    private void ResolveDefeat(GameContext context, string message)
    {
        _returnAfterDismiss = true;
        _resultMessage = $"{message} Enter를 누르면 마지막 회복 지점으로 돌아갑니다.";
        _awaitingDismiss = true;
    }

    private string AwardExperience(GameContext context, Encounter encounter, string prefix)
    {
        var player = context.Session.Party.ActiveCreature;
        var enemy = encounter.OpponentCreature;
        var gained = Math.Max(4, (enemy.Level * (encounter.IsTrainerBattle ? 4 : 3)) + 6);
        player.Experience += gained;

        var message = $"{prefix} 경험치 {gained}을(를) 얻었다!";
        var leveledUp = false;
        while (player.Experience >= Creature.GetExperienceForNextLevel(player.Level))
        {
            player.Experience -= Creature.GetExperienceForNextLevel(player.Level);
            var previousMaxHealth = player.MaxHealth;
            player.Level++;
            player.MaxHealth = Creature.CalculateMaxHealth(player.Level);
            player.CurrentHealth = Math.Min(player.MaxHealth, player.CurrentHealth + (player.MaxHealth - previousMaxHealth) + 4);
            var healthGain = player.MaxHealth - previousMaxHealth;
            leveledUp = true;
            message += $" {player.Nickname}의 레벨이 {player.Level}이 되었고 최대 HP가 {healthGain}만큼 올랐다!";
        }

        if (leveledUp)
        {
            message += $" {player.Nickname}이(가) 더 강해졌다!";
        }

        return message;
    }

    private static string BuildAwardExperienceMessage(GameContext context, Encounter encounter, string prefix)
    {
        var player = context.Session.Party.ActiveCreature;
        var enemy = encounter.OpponentCreature;
        var gained = Math.Max(4, (enemy.Level * (encounter.IsTrainerBattle ? 4 : 3)) + 6);
        player.Experience += gained;

        var message = $"{prefix} 경험치 {gained}을(를) 얻었다!";
        var leveledUp = false;
        while (player.Experience >= Creature.GetExperienceForNextLevel(player.Level))
        {
            player.Experience -= Creature.GetExperienceForNextLevel(player.Level);
            var previousMaxHealth = player.MaxHealth;
            player.Level++;
            player.MaxHealth = Creature.CalculateMaxHealth(player.Level);
            player.CurrentHealth = Math.Min(player.MaxHealth, player.CurrentHealth + (player.MaxHealth - previousMaxHealth) + 4);
            var healthGain = player.MaxHealth - previousMaxHealth;
            leveledUp = true;
            message += $" {player.Nickname}의 레벨이 {player.Level}이 되었고 최대 HP가 {healthGain}만큼 올랐다!";
        }

        if (leveledUp)
        {
            message += $" {player.Nickname}이(가) 더 강해졌다!";
        }

        return message;
    }

    private void DrawBackdrop(GameContext context)
    {
        var viewport = context.Viewport;
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(198, 226, 218));
        context.PrimitiveRenderer.Fill(new Rectangle(0, 0, viewport.Width, 188), new Color(170, 214, 202));
        context.PrimitiveRenderer.Fill(new Rectangle(0, 188, viewport.Width, 156), new Color(122, 172, 130));
        context.PrimitiveRenderer.Fill(new Rectangle(0, 344, viewport.Width, viewport.Height - 344), new Color(100, 146, 108));
        context.PrimitiveRenderer.Fill(new Rectangle(0, 318, viewport.Width, 18), new Color(148, 196, 134, 210));
        context.PrimitiveRenderer.Fill(new Rectangle(0, 332, viewport.Width, 10), new Color(90, 122, 86, 170));

        for (var i = 0; i < 5; i++)
        {
            var x = 52 + (i * 178);
            context.PrimitiveRenderer.Fill(new Rectangle(x, 84 + ((i % 2) * 12), 80, 22), new Color(196, 230, 216, 140));
        }
    }

    private void DrawBattleHints(GameContext context)
    {
        var hints = BuildBattleHintLines();
        var rect = new Rectangle(620, 26, 312, Math.Max(36, 18 * hints.Length + 10));
        context.InputHints.DrawHints(context.SpriteBatch, rect, hints);
    }

    private string[] BuildBattleHintLines()
    {
        if (_selectingMoves)
        {
            return new[]
            {
                "↑↓ / W S : 기술 선택",
                "Enter / Space : 사용",
                "Esc : 취소"
            };
        }

        if (_selectingItems)
        {
            return new[]
            {
                "↑↓ : 아이템 선택",
                "Enter / Space : 사용",
                "Esc : 닫기"
            };
        }

        if (_selectingItemTarget)
        {
            return new[]
            {
                "↑↓ : 대상 선택",
                "Enter / Space : 사용",
                "Esc : 이전 단계"
            };
        }

        if (_selectingSwitch)
        {
            return new[]
            {
                "↑↓ : 교체 후보",
                "Enter / Space : 확정",
                "Esc : 취소"
            };
        }

        return new[]
        {
            "↑↓ / W S : 행동 선택",
            "Enter / Space : 실행",
            "Esc : 도주 / 대화 취소"
        };
    }

    private void DrawCreatureShadow(GameContext context, Rectangle rect, float impactTimer)
    {
        var alpha = impactTimer > 0f ? 150 : 108;
        context.PrimitiveRenderer.Fill(rect, new Color(26, 40, 32, alpha));
    }

    private void DrawCreatureSprite(GameContext context, string speciesId, bool backSprite, Rectangle rect, float impactTimer)
    {
        context.Art.DrawCreature(context.SpriteBatch, speciesId, backSprite, rect);
        if (impactTimer > 0f)
        {
            context.PrimitiveRenderer.Outline(rect, 2, new Color(255, 248, 216, 180));
        }
    }

    private void DrawCard(GameContext context, Rectangle rect, Creature creature, string label, bool playerSide)
    {
        if (playerSide)
        {
            rect = new Rectangle(rect.Right + 18, rect.Y + 28, 232, rect.Height - 6);
        }

        context.UiSkin.DrawPanel(context.SpriteBatch, rect);
        var species = context.Definitions.Species[creature.SpeciesId];
        context.TextRenderer.DrawText(new Vector2(rect.X + 18, rect.Y + 12), creature.Nickname, 2, new Color(244, 242, 232));
        context.TextRenderer.DrawText(new Vector2(rect.Right - 88, rect.Y + 12), $"Lv {creature.Level}", 2, new Color(226, 232, 238));
        context.TextRenderer.DrawText(new Vector2(rect.X + 18, rect.Y + 38), $"{label}  {species.Name}", 1, new Color(210, 224, 228));

        var hpRect = new Rectangle(rect.X + 18, rect.Y + 58, playerSide ? 204 : 188, 16);
        context.TextRenderer.DrawText(new Vector2(hpRect.X, hpRect.Y - 18), "HP", 1, new Color(226, 230, 188));
        DrawHpBar(context, hpRect, creature.CurrentHealth, creature.MaxHealth);

        if (playerSide)
        {
            context.TextRenderer.DrawText(new Vector2(rect.Right - 118, rect.Y + 58), $"{creature.CurrentHealth}/{creature.MaxHealth}", 1, new Color(220, 228, 234));
            context.TextRenderer.DrawText(new Vector2(rect.X + 18, rect.Y + 78), "EXP", 1, new Color(226, 230, 188));
            context.UiSkin.DrawExperienceBar(context.SpriteBatch, new Rectangle(rect.X + 52, rect.Y + 80, 164, 10), GetExperienceRatio(creature));
            context.TextRenderer.DrawText(new Vector2(rect.X + 18, rect.Y + 96), "SP", 1, new Color(246, 228, 152));
            context.UiSkin.DrawExperienceBar(context.SpriteBatch, new Rectangle(rect.X + 52, rect.Y + 98, 164, 10), _superGauge / 100f);
        }
    }

    private void DrawMessage(GameContext context, Encounter? encounter)
    {
        var rect = new Rectangle(34, 404, 540, 132);
        context.UiSkin.DrawPanel(context.SpriteBatch, rect);
        var message = string.IsNullOrWhiteSpace(_resultMessage) ? "무엇을 할지 선택하세요." : _resultMessage;
        context.TextRenderer.DrawText(new Vector2(rect.X + 18, rect.Y + 12), message, 1, new Color(232, 236, 240));
        context.TextRenderer.DrawText(new Vector2(rect.X + 18, rect.Y + 44), BuildBattleHelperLine(encounter), 1, new Color(208, 220, 228));
        context.TextRenderer.DrawText(new Vector2(rect.X + 18, rect.Y + 68), "↑↓ / W S : 선택 · I : 상태 확인", 1, new Color(200, 210, 226));
        var instructionY = rect.Bottom - 44;
        var actionLine = _awaitingDismiss ? "Enter : 전투 종료" : "Enter : 결정 · Esc : 취소/도주";
        context.TextRenderer.DrawText(new Vector2(rect.X + 18, instructionY), actionLine, 1, new Color(246, 226, 152));
        var gaugeLine = $"필살기 게이지 {_superGauge:F0}%";
        context.TextRenderer.DrawText(new Vector2(rect.X + 18, instructionY + 20), gaugeLine, 1, new Color(246, 226, 152));
    }

    private string BuildBattleHelperLine(Encounter? encounter)
    {
        if (_selectingMoves)
        {
            return "기술을 선택하고 Enter로 사용, Esc로 뒤로 갑니다.";
        }

        if (_selectingItems)
        {
            return "회복 아이템을 고르고 Enter, Esc로 닫기.";
        }

        if (_selectingItemTarget)
        {
            return "사용 대상을 고르고 Enter, Esc로 이전 단계.";
        }

        if (_selectingSwitch)
        {
            return "교체할 동료를 고르고 Enter, Esc로 취소.";
        }

        if (encounter is not null && encounter.IsTrainerBattle)
        {
            return "정식 대결! Enter로 행동, Esc는 물러날 수 없습니다.";
        }

        return "Enter로 동작 선택, Esc는 도망을 시도합니다.";
    }

    private void DrawActions(GameContext context)
    {
        DrawListPanel(
            context,
            new Rectangle(624, 350, 238, 166),
            "행동",
            _options,
            _selected,
            option => option,
            "위아래 선택  Enter 결정");
    }

    private void DrawMoves(GameContext context)
    {
        DrawListPanel(
            context,
            new Rectangle(574, 336, 288, 180),
            "기술",
            _playerMoves,
            _moveSelection,
            move =>
            {
                var pp = BattleMoveHelper.GetCurrentPp(context.Session.Party.ActiveCreature, move);
                return $"{move.Name}  {BattleText.TypeLabel(move.TypeId)}  PP {pp}/{move.MaxPp}";
            },
            "ESC 취소");
    }

    private void DrawItems(GameContext context)
    {
        var items = BattleMoveHelper.GetBattleUsableItems(context);
        DrawListPanel(
            context,
            new Rectangle(588, 342, 274, 174),
            "가방",
            items,
            _itemSelection,
            item => $"{item.Name}  x{context.Session.Inventory.GetQuantity(item.Id)}  +{item.HealAmount}HP",
            items.Count == 0 ? "회복 아이템이 없습니다." : "대상을 골라 회복합니다.");
    }

    private void DrawTargets(GameContext context)
    {
        DrawListPanel(
            context,
            new Rectangle(560, 300, 302, 216),
            "대상 선택",
            context.Session.Party.Members,
            _itemTargetSelection,
            creature => $"{creature.Nickname}  HP {creature.CurrentHealth}/{creature.MaxHealth}",
            "회복시킬 동료를 고르세요");
    }

    private void DrawSwitches(GameContext context)
    {
        DrawListPanel(
            context,
            new Rectangle(548, 286, 314, 230),
            _forcedSwitch ? "다음 동료" : "교체",
            context.Session.Party.Members,
            _switchSelection,
            creature =>
            {
                var marker = creature == context.Session.Party.ActiveCreature ? "선두" : creature.IsFainted ? "기절" : "대기";
                return $"{creature.Nickname}  Lv {creature.Level}  {marker}";
            },
            _forcedSwitch ? "기절한 뒤에는 반드시 교체해야 합니다." : "ESC 취소");
    }

    private void DrawListPanel<T>(GameContext context, Rectangle rect, string title, IReadOnlyList<T> rows, int selectedIndex, Func<T, string> formatter, string footer)
    {
        context.UiSkin.DrawPanel(context.SpriteBatch, rect);
        context.TextRenderer.DrawText(new Vector2(rect.X + 18, rect.Y + 12), title, 2, new Color(248, 236, 182));

        if (rows.Count == 0)
        {
            context.TextRenderer.DrawText(new Vector2(rect.X + 18, rect.Y + 54), "표시할 항목이 없습니다.", 1, new Color(218, 224, 232));
            context.TextRenderer.DrawText(new Vector2(rect.X + 18, rect.Bottom - 24), footer, 1, new Color(208, 220, 228));
            return;
        }

        const int rowHeight = 24;
        var contentTop = rect.Y + 40;
        var contentBottom = rect.Bottom - 42;
        var visibleRows = Math.Max(1, (contentBottom - contentTop) / rowHeight);
        var maxStart = Math.Max(0, rows.Count - visibleRows);
        var startIndex = Math.Clamp(selectedIndex - visibleRows + 1, 0, maxStart);

        var y = contentTop;
        for (var i = 0; i < visibleRows && (startIndex + i) < rows.Count; i++)
        {
            var rowIndex = startIndex + i;
            var rowRect = new Rectangle(rect.X + 10, y - 3, rect.Width - 20, 22);
            if (rowIndex == selectedIndex)
            {
                context.UiSkin.DrawSelection(context.SpriteBatch, rowRect);
            }

            var color = rowIndex == selectedIndex ? new Color(24, 34, 48) : new Color(214, 222, 230);
            context.TextRenderer.DrawText(new Vector2(rect.X + 20, y), formatter(rows[rowIndex]), 1, color);
            y += rowHeight;
        }

        DrawScrollIndicator(context, rect, startIndex, visibleRows, rows.Count);
        context.TextRenderer.DrawText(new Vector2(rect.X + 18, rect.Bottom - 28), footer, 1, new Color(208, 220, 228));
    }

    private static void DrawScrollIndicator(GameContext context, Rectangle rect, int startIndex, int visibleRows, int totalRows)
    {
        if (totalRows <= visibleRows)
        {
            return;
        }

        if (startIndex > 0)
        {
            context.TextRenderer.DrawText(new Vector2(rect.Right - 30, rect.Y + 16), "▲", 1, new Color(246, 226, 152));
        }

        if (startIndex + visibleRows < totalRows)
        {
            context.TextRenderer.DrawText(new Vector2(rect.Right - 30, rect.Bottom - 30), "▼", 1, new Color(246, 226, 152));
        }
    }

    private static int ImpactOffset(float timer)
    {
        return timer <= 0f ? 0 : (int)MathF.Round(MathF.Sin(timer * 56f) * 6f);
    }

    private static int GetBattleBobOffset(float totalTime, float phase)
    {
        return (int)MathF.Round(MathF.Sin((totalTime * 2.3f) + phase) * 3f);
    }

    private static int GetEntryOffset(float timer, int maxOffset)
    {
        if (timer <= 0f)
        {
            return 0;
        }

        var ratio = Math.Clamp(timer / 0.36f, 0f, 1f);
        return (int)MathF.Round(maxOffset * ratio);
    }

    private void AddSuperGauge(float amount)
    {
        _superGauge = Math.Clamp(_superGauge + amount, 0f, 100f);
    }

    private static Color GetTypeFlashColor(string typeId) => typeId switch
    {
        "leaf" => new Color(164, 232, 144),
        "flame" => new Color(255, 184, 120),
        "wave" => new Color(156, 216, 255),
        _ => new Color(255, 255, 255)
    };

    private static int FindFirstSwitchOption(GameContext context)
    {
        for (var i = 0; i < context.Session.Party.Count; i++)
        {
            if (context.Session.Party.CanSwitchTo(i))
            {
                return i;
            }
        }

        return Math.Clamp(context.Session.Party.ActiveIndex, 0, Math.Max(0, context.Session.Party.Count - 1));
    }

    private void TriggerEnemyImpact(string typeId, float flashDuration = 0.08f)
    {
        _enemyImpactTimer = 0.2f;
        _effectFlashTimer = flashDuration;
        _effectFlashColor = GetTypeFlashColor(typeId);
    }

    private void TriggerPlayerImpact(string typeId, float flashDuration = 0.08f)
    {
        _playerImpactTimer = 0.2f;
        _effectFlashTimer = flashDuration;
        _effectFlashColor = GetTypeFlashColor(typeId);
    }

    private static bool IsConfirmPressed(GameContext context)
    {
        return context.Input.WasPressed(Keys.Enter) || context.Input.WasPressed(Keys.Space);
    }

    private static bool IsUpPressed(GameContext context)
    {
        return context.Input.WasRepeated(Keys.Up) || context.Input.WasRepeated(Keys.W);
    }

    private static bool IsDownPressed(GameContext context)
    {
        return context.Input.WasRepeated(Keys.Down) || context.Input.WasRepeated(Keys.S);
    }

    private static float GetExperienceRatio(Creature creature)
    {
        var next = Creature.GetExperienceForNextLevel(creature.Level);
        return next <= 0 ? 0f : (float)creature.Experience / next;
    }

    private static void DrawHpBar(GameContext context, Rectangle rect, int current, int max)
    {
        var ratio = max <= 0 ? 0f : (float)current / max;
        context.UiSkin.DrawHealthBar(context.SpriteBatch, rect, ratio);
    }

    private void ResetLocalState()
    {
        _selected = 0;
        _switchSelection = 0;
        _moveSelection = 0;
        _itemSelection = 0;
        _itemTargetSelection = 0;
        _awaitingDismiss = false;
        _selectingSwitch = false;
        _selectingMoves = false;
        _selectingItems = false;
        _selectingItemTarget = false;
        _forcedSwitch = false;
        _returnAfterDismiss = false;
        _resultMessage = string.Empty;
        _boundEncounter = null;
        _boundActiveCreature = null;
        _playerMoves = [];
        _enemyMoves = [];
        _enemyImpactTimer = 0f;
        _playerImpactTimer = 0f;
        _effectFlashTimer = 0f;
        _playerEntryTimer = 0f;
        _enemyEntryTimer = 0f;
        _superGauge = 0f;
        _effectFlashColor = Color.White;
    }
}

