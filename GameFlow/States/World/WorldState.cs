using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PyGame.Domain.Battle;
using PyGame.Domain.Creatures;
using PyGame.Domain.Npc;
using PyGame.Domain.World;
using PyGame.GameFlow.Simulation;
using PyGame.GameFlow.States.Job;
using PyGame.GameFlow.StateManager;

namespace PyGame.GameFlow.States.World;

public sealed class WorldState : IGameState
{
    private static readonly Vector2 CameraOffset = new(0f, -40f);

    public GameStateId Id => GameStateId.World;

    public void Update(GameTime gameTime, GameContext context)
    {
        var session = context.Session;
        session.WorldInfoPanelTimeRemaining = Math.Max(0f, session.WorldInfoPanelTimeRemaining - (float)gameTime.ElapsedGameTime.TotalSeconds);
        var map = context.Definitions.Maps[session.CurrentMapId];
        RuntimeWorldSimulation.EnsureInitialized(session, context.Definitions);

        if (context.Input.WasPressed(Keys.Escape))
        {
            session.ReturnState = GameStateId.World;
            context.StateManager.PushState(GameStateId.Pause);
            return;
        }

        if (context.Input.WasPressed(Keys.P))
        {
            session.ReturnState = GameStateId.World;
            context.StateManager.PushState(GameStateId.Party);
            return;
        }

        if (context.Input.WasPressed(Keys.B))
        {
            session.ReturnState = GameStateId.World;
            context.StateManager.PushState(GameStateId.Bag);
            return;
        }

        var step = GetStep(context.Input);
        if (step != Point.Zero)
        {
            session.FacingDirection = step;
            TryMove(step, map, context);
            return;
        }

        var target = session.PlayerTilePosition + session.FacingDirection;
        if (context.Input.WasPressed(Keys.I))
        {
            ShowEnvironmentInfo(session, map, target);
            return;
        }

        if (!context.Input.WasPressed(Keys.Space) && !context.Input.WasPressed(Keys.Enter))
        {
            return;
        }
        if (TryOpenSurveySpot(context, map, target))
        {
            return;
        }

        if (TryOpenSecurityDuty(context, map, target))
        {
            return;
        }

        if (TryOpenIndustryDuty(context, map, target))
        {
            return;
        }

        if (map.TryGetPcTerminalAt(target, out var terminal))
        {
            session.ReturnState = GameStateId.World;
            session.StatusMessage = $"{terminal.Label}에 접속했습니다.";
            ShowWorldInfo(session, "PC 연결", session.StatusMessage, "보관함 화면으로 이동합니다.", 2.4f);
            context.StateManager.PushState(GameStateId.Storage);
            return;
        }

        if (map.TryGetBulletinBoardAt(target, out var board))
        {
            session.CurrentBoardLabel = board.Label;
            session.CurrentBoardNoticeIds = board.NoticeIds;
            session.ReturnState = GameStateId.World;
            session.StatusMessage = $"{board.Label} 공고를 펼쳐 봅니다.";
            context.StateManager.PushState(GameStateId.Board);
            return;
        }

        if (!map.TryGetNpcAt(target, out var npcPlacement))
        {
            ShowEnvironmentInfo(session, map, target);
            return;
        }

        var npc = context.Definitions.Npcs[npcPlacement.NpcId];
        if (ShouldOpenResidence(npc, session))
        {
            session.ReturnState = GameStateId.World;
            session.StatusMessage = $"{npc.Name}이(가) 길드 배정 숙소와 생활 서비스 현황을 보여 줍니다.";
            context.StateManager.PushState(GameStateId.Residence);
            return;
        }

        if (JobState.ShouldOpenJobOffice(npc.Id, session))
        {
            JobState.PrepareJobOffice(session, npc.Id);
            session.ReturnState = GameStateId.World;
            session.StatusMessage = $"{npc.Name}이(가) 오늘 근무를 안내합니다.";
            context.StateManager.PushState(GameStateId.Job);
            return;
        }

        if (npc.HealsParty)
        {
            session.Party.HealAll(context.Definitions.Moves);
            session.Party.EnsureUsableLead();
            session.RecoveryMapId = session.CurrentMapId;
            session.RecoveryTilePosition = session.PlayerTilePosition;
            session.StatusMessage = $"{npc.Name}이(가) 파티를 회복해 주었습니다.";
            ShowWorldInfo(session, npc.Name, session.StatusMessage, "다음 조사 구간으로 이동하기 전에 상태를 정비하세요.", 2.6f);
            return;
        }

        if (npc.OpensShop)
        {
            session.CurrentShopItemIds = GetShopItemIds(npc, context);
            session.ReturnState = GameStateId.World;
            session.StatusMessage = $"{npc.Name}의 보급 상점을 둘러봅니다.";
            context.StateManager.PushState(GameStateId.Shop);
            return;
        }

        if (ShouldOpenTradeHub(npc, session))
        {
            session.ReturnState = GameStateId.World;
            session.StatusMessage = $"{npc.Name}이(가) 바람개울 교역 허브 안내를 열어 줍니다.";
            context.StateManager.PushState(GameStateId.Trade);
            return;
        }

        if (npc.StartsTrainerBattle && CanStartTrainerBattle(npc, session.Progression))
        {
            StartTrainerBattle(npc, context, npc.TrainerNoticeText);
            return;
        }

        var statusLocked = false;
        if (npc.StartsTrainerBattle && session.Progression.HasFlag(npc.TrainerDefeatedFlag))
        {
            session.StatusMessage = $"{npc.Name}은(는) 이미 통과 점검을 마친 상태라고 말합니다.";
            statusLocked = true;
        }
        else if (!string.IsNullOrWhiteSpace(npc.TrainerRequiredFlag) && !session.Progression.HasFlag(npc.TrainerRequiredFlag))
        {
            session.StatusMessage = $"{npc.Name}은(는) 먼저 접수원에게 조사 목표를 받으라고 말합니다.";
            statusLocked = true;
        }

        if (session.Progression.TryAddFlag(npc.GrantsFlagOnTalk))
        {
            session.StatusMessage = $"{npc.Name}에게서 첫 조사 의뢰를 받았습니다.";
            ShowWorldInfo(
                session,
                "첫 조사 의뢰",
                session.StatusMessage,
                "해솔길에서 정찰원 리노의 통과 점검을 마치고 접수원 유나에게 보고하세요.",
                2.8f);
            statusLocked = true;
        }

        var rewardGranted = false;
        if (!string.IsNullOrWhiteSpace(npc.RewardRequiredFlag) &&
            session.Progression.HasFlag(npc.RewardRequiredFlag) &&
            session.Progression.TryAddFlag(npc.RewardFlagOnTalk))
        {
            session.Progression.BadgeCount += npc.RewardBadgeCount;
            session.Money += npc.RewardMoney;
            if (!string.IsNullOrWhiteSpace(npc.RewardItemId) && npc.RewardItemQuantity > 0)
            {
                session.Inventory.Add(npc.RewardItemId, npc.RewardItemQuantity);
            }

            session.StatusMessage = $"{npc.Name}이(가) 첫 조사 보고를 접수하고 보상을 지급했습니다.";
            ShowWorldInfo(
                session,
                "첫 조사 보고 완료",
                session.StatusMessage,
                "다음 목표: 속삭임 숲을 지나 바람개울 교역 거점까지 조사 기록을 이어가세요.",
                3f);
            rewardGranted = true;
            statusLocked = true;
        }

        var dialogueLines = rewardGranted && npc.RewardDialogueLines.Count > 0 ? npc.RewardDialogueLines : ResolveDialogueLines(npc, session.Progression);
        session.ActiveDialogue = new DialogueScene(npc.Name, dialogueLines);
        session.ReturnState = GameStateId.World;
        if (!statusLocked)
        {
            session.StatusMessage = $"{npc.Name}과(와) 대화합니다.";
        }

        context.StateManager.ChangeState(GameStateId.Dialogue);
    }

    private static bool TryOpenSurveySpot(GameContext context, WorldMap map, Point target)
    {
        var session = context.Session;
        if (!SurveyRuntime.CanSurveySpot(session, map, target))
        {
            return false;
        }

        var finding = SurveyRuntime.BuildFinding(context.Definitions, session.Party.ActiveCreature, map, target);
        session.CurrentSurveyFindingTitle = finding.Title;
        session.CurrentSurveyFindingSummary = finding.Summary;
        session.CurrentSurveyFindingQuality = finding.Quality;
        session.CurrentSurveyRecommendedTag = finding.RecommendedTag;
        session.CurrentSurveyPerformanceBonus = finding.PerformanceBonus;
        session.CurrentSurveySucceeded = finding.Succeeded;
        session.ReturnState = GameStateId.World;
        session.StatusMessage = $"{finding.Title} 지점을 발견했습니다.";
        context.StateManager.PushState(GameStateId.Survey);
        return true;
    }

    private static bool TryOpenSecurityDuty(GameContext context, WorldMap map, Point target)
    {
        var session = context.Session;
        if (!SecurityDutyRuntime.CanInspect(session, map, target))
        {
            return false;
        }

        var finding = SecurityDutyRuntime.BuildFinding(context.Definitions, session.Party.ActiveCreature, map, target);
        session.CurrentSecurityDutyTitle = finding.Title;
        session.CurrentSecurityDutySummary = finding.Summary;
        session.CurrentSecurityDutyZone = finding.Zone;
        session.CurrentSecurityRecommendedTag = finding.RecommendedTag;
        session.CurrentSecurityPerformanceBonus = finding.PerformanceBonus;
        session.CurrentSecurityHazardBonus = finding.HazardBonus;
        session.CurrentSecurityThreatConfirmed = finding.ThreatConfirmed;
        session.ReturnState = GameStateId.World;
        session.StatusMessage = $"{finding.Zone} 순찰 기록을 정리합니다.";
        context.StateManager.PushState(GameStateId.SecurityDuty);
        return true;
    }

    private static bool TryOpenIndustryDuty(GameContext context, WorldMap map, Point target)
    {
        var session = context.Session;
        if (!IndustryDutyRuntime.CanInspect(session, map, target))
        {
            return false;
        }

        var finding = IndustryDutyRuntime.BuildFinding(context.Definitions, session.Party.ActiveCreature, target);
        session.CurrentIndustryDutyTitle = finding.Title;
        session.CurrentIndustryDutySummary = finding.Summary;
        session.CurrentIndustryDutyZone = finding.Zone;
        session.CurrentIndustryRecommendedTag = finding.RecommendedTag;
        session.CurrentIndustryPerformanceBonus = finding.PerformanceBonus;
        session.CurrentIndustryResourceId = finding.ResourceId;
        session.CurrentIndustryResourceQuantity = finding.ResourceQuantity;
        session.ReturnState = GameStateId.World;
        session.StatusMessage = $"{finding.Title} 작업 지점을 확인했습니다.";
        context.StateManager.PushState(GameStateId.IndustryDuty);
        return true;
    }

    public void Draw(GameTime gameTime, GameContext context)
    {
        _ = gameTime;
        var session = context.Session;
        var map = context.Definitions.Maps[session.CurrentMapId];
        var tileSize = map.TileSize;
        var playerPixels = new Vector2((session.PlayerTilePosition.X * tileSize) + (tileSize / 2f), (session.PlayerTilePosition.Y * tileSize) + (tileSize / 2f));
        var transform = context.Camera.CreateWorldTransform(playerPixels, context.Viewport, map.PixelSize, CameraOffset);

        context.SpriteBatch.Begin(transformMatrix: transform, samplerState: SamplerState.PointClamp);
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                context.Art.DrawWorldTile(context.SpriteBatch, map, new Point(x, y));
            }
        }

        var animationTime = (float)gameTime.TotalGameTime.TotalSeconds;

        foreach (var pickup in map.Pickups)
        {
            if (!session.Progression.HasFlag(pickup.CollectedFlag))
            {
                var bobOffset = (int)MathF.Round(MathF.Sin((animationTime * 3.4f) + pickup.X + pickup.Y) * 2f);
                var tileRect = new Rectangle(pickup.X * tileSize, (pickup.Y * tileSize) + bobOffset, tileSize, tileSize);
                context.Art.DrawPickup(context.SpriteBatch, tileRect);
            }
        }

        foreach (var terminal in map.PcTerminals)
        {
            context.Art.DrawTerminal(context.SpriteBatch, new Rectangle(terminal.X * tileSize, terminal.Y * tileSize, tileSize, tileSize));
        }

        foreach (var board in map.BulletinBoards)
        {
            context.Art.DrawBulletinBoard(context.SpriteBatch, new Rectangle(board.X * tileSize, board.Y * tileSize, tileSize, tileSize));
        }

        foreach (var npcPlacement in map.Npcs)
        {
            var npc = context.Definitions.Npcs[npcPlacement.NpcId];
            var defeated = !string.IsNullOrWhiteSpace(npc.TrainerDefeatedFlag) && session.Progression.HasFlag(npc.TrainerDefeatedFlag);
            if (npcPlacement.SightRange > 0 && !defeated)
            {
                DrawSightHint(context, npcPlacement, tileSize);
            }

            DrawCharacter(
                context,
                new Point(npcPlacement.X, npcPlacement.Y),
                new Point(npcPlacement.FacingX, npcPlacement.FacingY),
                npc.VisualStyle,
                defeated,
                animationTime + (npcPlacement.X * 0.19f) + (npcPlacement.Y * 0.13f));
        }

        DrawCharacter(context, session.PlayerTilePosition, session.FacingDirection, "player", false, animationTime);
        context.SpriteBatch.End();

        var hintLines = new[]
        {
            "방향키 / WASD : 이동",
            "Enter / Space : 상호작용",
            "I : 주변 정보",
            "Esc : 메뉴"
        };
        var hintHeight = 16 + (hintLines.Length * 18);
        const int hintWidth = 320;
        var hintX = context.Viewport.Width - hintWidth - 24;
        context.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        context.InputHints.DrawHints(
            context.SpriteBatch,
            new Rectangle(hintX, 12, hintWidth, hintHeight),
            hintLines);
        context.SpriteBatch.End();

        if (session.WorldInfoPanelTimeRemaining > 0f)
        {
            context.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawWorldInfoPanel(context, map, session);
            context.SpriteBatch.End();
        }
    }

    private static Point GetStep(PyGame.Core.Input.InputSnapshot input)
    {
        if (input.WasRepeated(Keys.Up) || input.WasRepeated(Keys.W)) return new Point(0, -1);
        if (input.WasRepeated(Keys.Down) || input.WasRepeated(Keys.S)) return new Point(0, 1);
        if (input.WasRepeated(Keys.Left) || input.WasRepeated(Keys.A)) return new Point(-1, 0);
        if (input.WasRepeated(Keys.Right) || input.WasRepeated(Keys.D)) return new Point(1, 0);
        return Point.Zero;
    }

    private static void TryMove(Point step, WorldMap map, GameContext context)
    {
        var session = context.Session;
        var target = session.PlayerTilePosition + step;
        session.WorldInfoPanelTimeRemaining = 0f;

        if (!map.IsWalkable(target))
        {
            session.StatusMessage = "그쪽으로는 갈 수 없습니다.";
            ShowWorldInfo(session, map.Name, session.StatusMessage, GetObjectiveText(session), 1.6f);
            return;
        }

        if (map.TryGetNpcAt(target, out _))
        {
            session.StatusMessage = "사람이 길을 막고 있습니다.";
            ShowWorldInfo(session, map.Name, session.StatusMessage, GetObjectiveText(session), 1.6f);
            return;
        }

        session.PlayerTilePosition = target;
        session.StatusMessage = $"{GetAreaName(map, target)}(으)로 이동했습니다.";
        if (map.TryGetWarpAt(target, out var warp))
        {
            session.CurrentMapId = warp.TargetMapId;
            session.PlayerTilePosition = new Point(warp.TargetX, warp.TargetY);
            RuntimeWorldSimulation.AdvanceForMovement(session, context.Definitions, 1, true);
            session.StatusMessage = string.IsNullOrWhiteSpace(warp.TransitionText) ? $"{context.Definitions.Maps[warp.TargetMapId].Name}에 도착했습니다." : warp.TransitionText;
            if (warp.TargetMapId == "pine_village")
            {
                if (session.Progression.TryAddFlag("visited_pine_village"))
                {
                    session.Resources.Add("brook_fish", 4);
                    session.StatusMessage = "바람개울 마을에 도착했습니다. 교역소 체험용 민물고기 상자를 받았습니다.";
                }
            }

            var regionSummary = RuntimeWorldSimulation.BuildRegionSummary(session, context.Definitions, warp.TargetMapId);
            ShowWorldInfo(
                session,
                context.Definitions.Maps[warp.TargetMapId].Name,
                session.StatusMessage,
                string.IsNullOrWhiteSpace(regionSummary) ? GetObjectiveText(session) : regionSummary,
                2.4f);
            JobState.TryAdvanceFieldExecution(session, context.Definitions);
            return;
        }

        RuntimeWorldSimulation.AdvanceForMovement(session, context.Definitions, 1, false);
        JobState.TryAdvanceFieldExecution(session, context.Definitions);
        TryCollectPickup(map, target, context);
        if (map.IsEncounterTile(target) && map.Encounters.Count > 0 && Random.Shared.NextDouble() < 0.18d)
        {
            StartWildEncounter(map, context);
            return;
        }

        CheckTrainerSight(map, context);
    }

    private static void StartWildEncounter(WorldMap map, GameContext context)
    {
        var session = context.Session;
        if (!session.Party.EnsureUsableLead())
        {
            session.StatusMessage = "파티를 먼저 회복해야 합니다.";
            ShowWorldInfo(session, map.Name, session.StatusMessage, "치료소나 회복 지원소를 이용하세요.", 2f);
            return;
        }

        var encounterDefinition = map.Encounters[Random.Shared.Next(map.Encounters.Count)];
        var species = context.Definitions.Species[encounterDefinition.CreatureId];
        var level = Random.Shared.Next(encounterDefinition.MinLevel, encounterDefinition.MaxLevel + 1);
        session.ActiveEncounter = new Encounter
        {
            IsTrainerBattle = false,
            OpponentName = "야생",
            OpponentParty = [MonsterWorkRuntime.CreateCreature(context.Definitions, species.Id, species.Name, level)]
        };
        session.ReturnState = GameStateId.World;
        session.StatusMessage = $"야생 {species.Name}이(가) 나타났다!";
        context.Audio.PlayBattleStart();
        context.StateManager.ChangeState(GameStateId.Battle);
    }

    private static void TryCollectPickup(WorldMap map, Point target, GameContext context)
    {
        if (!map.TryGetPickupAt(target, out var pickup) || context.Session.Progression.HasFlag(pickup.CollectedFlag))
        {
            return;
        }

        context.Session.Inventory.Add(pickup.ItemId, pickup.Quantity);
        context.Session.Progression.TryAddFlag(pickup.CollectedFlag);
        var item = context.Definitions.Items[pickup.ItemId];
        context.Session.StatusMessage = $"{item.Name} {pickup.Quantity}개를 주웠다.";
        ShowWorldInfo(context.Session, "보급품 획득", context.Session.StatusMessage, GetObjectiveText(context.Session), 2.2f);
    }

    private static void CheckTrainerSight(WorldMap map, GameContext context)
    {
        var session = context.Session;
        for (var i = 0; i < map.Npcs.Count; i++)
        {
            var placement = map.Npcs[i];
            if (placement.SightRange <= 0)
            {
                continue;
            }

            var npc = context.Definitions.Npcs[placement.NpcId];
            if (!CanStartTrainerBattle(npc, session.Progression) || !IsPlayerInSight(session.PlayerTilePosition, placement, map))
            {
                continue;
            }

            StartTrainerBattle(npc, context, string.IsNullOrWhiteSpace(npc.TrainerNoticeText) ? $"{npc.Name}이(가) 플레이어를 발견했다!" : npc.TrainerNoticeText);
            return;
        }
    }

    private static void StartTrainerBattle(NpcDefinition npc, GameContext context, string battleStartText)
    {
        if (!context.Session.Party.EnsureUsableLead())
        {
            context.Session.StatusMessage = "파티를 먼저 회복해야 합니다.";
            ShowWorldInfo(context.Session, npc.Name, context.Session.StatusMessage, "치료 후 다시 도전하세요.", 2f);
            return;
        }

        var roster = npc.TrainerRoster.Count > 0
            ? npc.TrainerRoster.Select(entry =>
            {
                var species = context.Definitions.Species[entry.CreatureId];
                return MonsterWorkRuntime.CreateCreature(context.Definitions, species.Id, species.Name, entry.Level);
            }).ToList()
            : [MonsterWorkRuntime.CreateCreature(context.Definitions, context.Definitions.Species[npc.TrainerCreatureId].Id, context.Definitions.Species[npc.TrainerCreatureId].Name, npc.TrainerCreatureLevel)];

        context.Session.ActiveEncounter = new Encounter
        {
            IsTrainerBattle = true,
            OpponentName = npc.Name,
            OpponentParty = roster,
            TrainerDefeatedFlag = npc.TrainerDefeatedFlag,
            TrainerVictoryFlag = npc.TrainerVictoryFlag
        };
        context.Session.ReturnState = GameStateId.World;
        context.Session.StatusMessage = battleStartText;
        context.Audio.PlayBattleStart();
        context.StateManager.ChangeState(GameStateId.Battle);
    }

    private static bool CanStartTrainerBattle(NpcDefinition npc, Domain.Progression.GameProgression progression)
    {
        if (!npc.StartsTrainerBattle)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(npc.TrainerDefeatedFlag) && progression.HasFlag(npc.TrainerDefeatedFlag))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(npc.TrainerRequiredFlag) || progression.HasFlag(npc.TrainerRequiredFlag);
    }

    private static List<string> ResolveDialogueLines(NpcDefinition npc, Domain.Progression.GameProgression progression)
    {
        if (!string.IsNullOrWhiteSpace(npc.ConditionalDialogueFlag) && progression.HasFlag(npc.ConditionalDialogueFlag) && npc.ConditionalDialogueLines.Count > 0)
        {
            return npc.ConditionalDialogueLines;
        }

        return npc.DialogueLines;
    }

    private static bool IsPlayerInSight(Point playerTile, NpcPlacement placement, WorldMap map)
    {
        if (placement.FacingX != 0)
        {
            if (playerTile.Y != placement.Y)
            {
                return false;
            }

            var delta = playerTile.X - placement.X;
            var inRange = placement.FacingX > 0 ? delta > 0 && delta <= placement.SightRange : delta < 0 && -delta <= placement.SightRange;
            if (!inRange)
            {
                return false;
            }

            for (var i = 1; i < Math.Abs(delta); i++)
            {
                if (!map.IsWalkable(new Point(placement.X + (placement.FacingX * i), placement.Y)))
                {
                    return false;
                }
            }

            return true;
        }

        if (playerTile.X != placement.X)
        {
            return false;
        }

        var verticalDelta = playerTile.Y - placement.Y;
        var verticalInRange = placement.FacingY > 0 ? verticalDelta > 0 && verticalDelta <= placement.SightRange : verticalDelta < 0 && -verticalDelta <= placement.SightRange;
        if (!verticalInRange)
        {
            return false;
        }

        for (var i = 1; i < Math.Abs(verticalDelta); i++)
        {
            if (!map.IsWalkable(new Point(placement.X, placement.Y + (placement.FacingY * i))))
            {
                return false;
            }
        }

        return true;
    }

    private static void DrawSightHint(GameContext context, NpcPlacement placement, int tileSize)
    {
        var step = new Point(Math.Sign(placement.FacingX), Math.Sign(placement.FacingY));
        if (step == Point.Zero)
        {
            return;
        }

        for (var i = 1; i <= placement.SightRange; i++)
        {
            var point = new Point(placement.X + (step.X * i), placement.Y + (step.Y * i));
            context.Art.DrawSightMarker(context.SpriteBatch, new Rectangle(point.X * tileSize, point.Y * tileSize, tileSize, tileSize));
        }
    }

    private static void DrawWorldInfoPanel(GameContext context, WorldMap map, GameSession session)
    {
        var rect = new Rectangle(18, 360, 924, 140);
        context.UiSkin.DrawPanel(context.SpriteBatch, rect);
        var title = string.IsNullOrWhiteSpace(session.WorldInfoTitle)
            ? $"{map.Name} | {GetAreaName(map, session.PlayerTilePosition)}"
            : session.WorldInfoTitle;
        context.TextRenderer.DrawText(new Vector2(36, rect.Y + 16), title, 2, new Color(246, 236, 192));
        context.TextRenderer.DrawText(new Vector2(36, rect.Y + 44), session.StatusMessage, 1, new Color(238, 240, 230));
        var detail = string.IsNullOrWhiteSpace(session.WorldInfoDetail) ? GetObjectiveText(session) : session.WorldInfoDetail;
        context.TextRenderer.DrawText(new Vector2(36, rect.Y + 72), detail, 1, new Color(208, 218, 226));
        context.TextRenderer.DrawText(new Vector2(36, rect.Y + 100), "I : 주변 정보 유지 · Enter : 상호작용", 1, new Color(246, 226, 152));
        context.TextRenderer.DrawText(new Vector2(36, rect.Y + 120), $"목표: {GetObjectiveText(session)}", 1, new Color(200, 212, 226));
    }

    private static string GetAreaName(WorldMap map, Point tilePosition)
    {
        return map.GetTileType(tilePosition) switch
        {
            TileType.Grass => "풀숲",
            TileType.Path => "길목",
            TileType.Building => "건물 벽면",
            TileType.Door => "출입구",
            TileType.Counter => "카운터 앞",
            TileType.Service => "서비스 구역",
            _ => string.IsNullOrWhiteSpace(map.AreaLabel) ? map.Name : map.AreaLabel
        };
    }

    private static string GetObjectiveText(GameSession session)
    {
        if (!string.IsNullOrWhiteSpace(session.CurrentJobId) && !string.IsNullOrWhiteSpace(session.CurrentJobAssignmentTitle))
        {
            return session.CurrentJobStage switch
            {
                "field_execution" => $"현재 근무: {session.CurrentJobAssignmentTitle} / 목표 지역 {session.CurrentJobTargetMapId}",
                "risk_decision" => $"현재 근무: {session.CurrentJobAssignmentTitle} / 창구로 돌아가 판단을 정리하세요.",
                "report_or_delivery" => $"현재 근무: {session.CurrentJobAssignmentTitle} / 보고 후 정산을 받으세요.",
                _ => $"현재 직군: {session.CurrentJobId}"
            };
        }

        if (!session.Progression.HasFlag("objective_route_challenge"))
        {
            return "접수원 유나에게 말을 걸어 첫 조사 의뢰를 받으세요.  B 가방  P 파티  ESC 메뉴";
        }

        if (!session.Progression.HasFlag("objective_route_cleared"))
        {
            return "해솔길 중앙의 정찰원 리노에게 통과 점검을 받고 조사 구간을 마무리하세요.";
        }

        if (!session.Progression.HasFlag("milestone_route_mark_claimed"))
        {
            return "해솔마을로 돌아가 접수원 유나에게 첫 조사 보고를 완료하고 조사 표식을 받으세요.";
        }

        if (!session.Progression.HasFlag("trainer_forest_rookie_defeated"))
        {
            return "속삭임 숲 안쪽의 견습 조사원을 지나 위험 채집 구간을 통과하세요.";
        }

        if (!session.Progression.HasFlag("visited_pine_village"))
        {
            return "속삭임 숲을 빠져나가 바람개울 마을 교역 거점까지 조사 기록을 이어가세요.";
        }

        return "바람개울 마을의 시설을 둘러보고 다음 조사와 교역 준비를 하세요.";
    }

    private static IReadOnlyList<string> GetShopItemIds(NpcDefinition npc, GameContext context)
    {
        if (!string.IsNullOrWhiteSpace(npc.ShopId) && context.Definitions.Shops.TryGetValue(npc.ShopId, out var shop))
        {
            return shop.ItemIds;
        }

        return npc.ShopItemIds;
    }

    private static bool ShouldOpenTradeHub(NpcDefinition npc, GameSession session)
    {
        return session.CurrentMapId == "pine_village" && npc.Id == "pine_guard";
    }

    private static bool ShouldOpenResidence(NpcDefinition npc, GameSession session)
    {
        return session.CurrentMapId == "player_home" && npc.Id == "home_mother";
    }

    private static void ShowEnvironmentInfo(GameSession session, WorldMap map, Point inspectTile)
    {
        var detail = map.GetTileType(inspectTile) switch
        {
            TileType.Grass => "바람에 흔들리는 풀숲입니다. 야생 개체가 튀어나올 수 있어 조사 중에는 주의가 필요합니다.",
            TileType.Path => "사람과 짐수레가 자주 오가는 길목입니다. 조사 구간과 생활권을 이어 줍니다.",
            TileType.Tree => "굵은 나무가 시야와 길을 막고 있습니다.",
            TileType.Building => "생활권 시설의 외벽입니다.",
            TileType.Door => "출입구가 보입니다. 조사 보고나 보급 정비를 맡는 시설일 수 있습니다.",
            TileType.Counter => "접수나 거래를 처리하는 카운터입니다.",
            TileType.Service => "회복이나 보급처럼 생활권 기능이 모인 구역입니다.",
            _ => "주변을 둘러보며 현재 위치와 조사 목표를 다시 정리합니다."
        };
        ShowWorldInfo(session, $"{map.Name} | {GetAreaName(map, session.PlayerTilePosition)}", detail, GetObjectiveText(session), 4f);
    }

    private static void ShowWorldInfo(GameSession session, string title, string body, string detail, float seconds)
    {
        session.WorldInfoTitle = title;
        session.StatusMessage = body;
        session.WorldInfoDetail = detail;
        session.WorldInfoPanelTimeRemaining = seconds;
    }

    private static void DrawCharacter(GameContext context, Point tilePosition, Point facingDirection, string visualStyle, bool defeated, float animationTime)
    {
        var tileSize = context.Definitions.Maps[context.Session.CurrentMapId].TileSize;
        var baseX = tilePosition.X * tileSize;
        var baseY = tilePosition.Y * tileSize;
        var alternateFrame = !defeated && ((int)(animationTime * 5.5f) % 2 == 1);
        var bobOffset = defeated ? 0 : (int)MathF.Round(MathF.Sin(animationTime * 5.5f) * 1.4f);
        context.PrimitiveRenderer.Fill(new Rectangle(baseX + 8, baseY + 25, 16, 4), new Color(0, 0, 0, alternateFrame ? 80 : 92));
        context.Art.DrawCharacter(
            context.SpriteBatch,
            visualStyle,
            facingDirection,
            new Rectangle(baseX + 8, baseY + 5 + bobOffset, 16, 16),
            defeated,
            alternateFrame);
    }
}
