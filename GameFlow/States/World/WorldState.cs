using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PyGame.Domain.Battle;
using PyGame.Domain.Creatures;
using PyGame.Domain.Npc;
using PyGame.Domain.World;
using PyGame.GameFlow.StateManager;

namespace PyGame.GameFlow.States.World;

public sealed class WorldState : IGameState
{
    public GameStateId Id => GameStateId.World;

    public void Update(GameTime gameTime, GameContext context)
    {
        _ = gameTime;

        var session = context.Session;
        var map = context.Definitions.Maps[session.CurrentMapId];

        if (context.Input.WasPressed(Keys.Escape))
        {
            session.ReturnState = GameStateId.World;
            context.StateManager.ChangeState(GameStateId.Pause);
            return;
        }

        if (context.Input.WasPressed(Keys.P))
        {
            session.ReturnState = GameStateId.World;
            context.StateManager.ChangeState(GameStateId.Party);
            return;
        }

        if (context.Input.WasPressed(Keys.B))
        {
            session.ReturnState = GameStateId.World;
            context.StateManager.ChangeState(GameStateId.Bag);
            return;
        }

        var step = GetStep(context.Input);
        if (step != Point.Zero)
        {
            session.FacingDirection = step;
            TryMove(step, map, context);
            return;
        }

        if (!context.Input.WasPressed(Keys.Space) && !context.Input.WasPressed(Keys.Enter))
        {
            return;
        }

        var target = session.PlayerTilePosition + session.FacingDirection;
        if (map.TryGetPcTerminalAt(target, out var terminal))
        {
            session.ReturnState = GameStateId.World;
            session.StatusMessage = $"{terminal.Label}를 열었습니다.";
            context.StateManager.ChangeState(GameStateId.Storage);
            return;
        }

        if (!map.TryGetNpcAt(target, out var npcPlacement))
        {
            session.StatusMessage = "앞에는 조사할 것이 없습니다.";
            return;
        }

        var npc = context.Definitions.Npcs[npcPlacement.NpcId];
        if (npc.HealsParty)
        {
            session.Party.HealAll();
            session.Party.EnsureUsableLead();
            session.StatusMessage = $"{npc.Name}가 파티를 모두 회복해 주었습니다.";
            return;
        }

        if (npc.OpensShop)
        {
            session.CurrentShopItemIds = npc.ShopItemIds;
            session.ReturnState = GameStateId.World;
            session.StatusMessage = $"{npc.Name}의 상점에 들어갔습니다.";
            context.StateManager.ChangeState(GameStateId.Shop);
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
            session.StatusMessage = $"{npc.Name}은 이미 승부를 인정했습니다.";
            statusLocked = true;
        }
        else if (!string.IsNullOrWhiteSpace(npc.TrainerRequiredFlag) && !session.Progression.HasFlag(npc.TrainerRequiredFlag))
        {
            session.StatusMessage = $"{npc.Name}은 아직 당신을 기다리고 있습니다.";
            statusLocked = true;
        }

        if (session.Progression.TryAddFlag(npc.GrantsFlagOnTalk))
        {
            session.StatusMessage = $"{npc.Name}에게서 첫 목표를 받았습니다.";
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

            session.StatusMessage = $"{npc.Name}에게서 바람길 마크와 보상을 받았습니다.";
            rewardGranted = true;
            statusLocked = true;
        }

        var dialogueLines = rewardGranted && npc.RewardDialogueLines.Count > 0
            ? npc.RewardDialogueLines
            : ResolveDialogueLines(npc, session.Progression);
        session.ActiveDialogue = new DialogueScene(npc.Name, dialogueLines);
        session.ReturnState = GameStateId.World;
        if (!statusLocked)
        {
            session.StatusMessage = $"{npc.Name}와 대화합니다.";
        }

        context.StateManager.ChangeState(GameStateId.Dialogue);
    }

    public void Draw(GameTime gameTime, GameContext context)
    {
        _ = gameTime;

        var session = context.Session;
        var map = context.Definitions.Maps[session.CurrentMapId];
        var tileSize = map.TileSize;
        var playerPixels = new Vector2((session.PlayerTilePosition.X * tileSize) + (tileSize / 2f), (session.PlayerTilePosition.Y * tileSize) + (tileSize / 2f));
        var transform = context.Camera.CreateWorldTransform(playerPixels, context.Viewport, map.PixelSize);

        context.SpriteBatch.Begin(transformMatrix: transform);

        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                var tile = map.GetTileType(new Point(x, y));
                var color = tile switch
                {
                    TileType.Path => new Color(204, 190, 138),
                    TileType.Wall => new Color(96, 84, 76),
                    TileType.Tree => new Color(56, 106, 66),
                    TileType.Grass => new Color(92, 156, 74),
                    _ => new Color(132, 182, 114)
                };

                var tileRect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
                context.PrimitiveRenderer.Fill(tileRect, color);
                context.PrimitiveRenderer.Outline(tileRect, 1, new Color(0, 0, 0, 50));

                if (x == map.RecoveryX && y == map.RecoveryY)
                {
                    context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 4, tileRect.Y + 4, tileRect.Width - 8, tileRect.Height - 8), new Color(238, 226, 234));
                    context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 13, tileRect.Y + 7, 6, 18), new Color(194, 64, 88));
                    context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 7, tileRect.Y + 13, 18, 6), new Color(194, 64, 88));
                }

                if (tile == TileType.Grass)
                {
                    context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 4, tileRect.Y + 6, 4, tileRect.Height - 12), new Color(62, 122, 56));
                    context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 12, tileRect.Y + 4, 4, tileRect.Height - 10), new Color(62, 122, 56));
                    context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 20, tileRect.Y + 7, 4, tileRect.Height - 12), new Color(62, 122, 56));
                }

                if (tile == TileType.Tree)
                {
                    context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 10, tileRect.Y + 18, 12, 10), new Color(88, 66, 44));
                    context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 4, tileRect.Y + 4, 24, 18), new Color(74, 134, 78));
                }

                if (tile == TileType.Wall)
                {
                    context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 2, tileRect.Y + 4, tileRect.Width - 4, tileRect.Height - 6), new Color(122, 106, 94));
                    context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 2, tileRect.Y + 2, tileRect.Width - 4, 8), new Color(160, 82, 62));
                }
            }
        }

        foreach (var pickup in map.Pickups)
        {
            if (session.Progression.HasFlag(pickup.CollectedFlag))
            {
                continue;
            }

            DrawPickup(context, pickup, tileSize);
        }

        foreach (var terminal in map.PcTerminals)
        {
            var tileRect = new Rectangle(terminal.X * tileSize, terminal.Y * tileSize, tileSize, tileSize);
            context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 4, tileRect.Y + 24, 24, 4), new Color(86, 64, 44));
            context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 6, tileRect.Y + 8, 20, 16), new Color(188, 222, 230));
            context.PrimitiveRenderer.Outline(new Rectangle(tileRect.X + 6, tileRect.Y + 8, 20, 16), 2, new Color(80, 110, 140));
            context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 11, tileRect.Y + 12, 10, 6), new Color(94, 148, 176));
        }

        foreach (var npcPlacement in map.Npcs)
        {
            var npc = context.Definitions.Npcs[npcPlacement.NpcId];
            var defeated = !string.IsNullOrWhiteSpace(npc.TrainerDefeatedFlag) && session.Progression.HasFlag(npc.TrainerDefeatedFlag);
            DrawCharacter(context, new Point(npcPlacement.X, npcPlacement.Y), new Point(npcPlacement.FacingX, npcPlacement.FacingY), GetNpcPalette(npc, defeated), false);

            if (npcPlacement.SightRange > 0 && !defeated)
            {
                DrawSightHint(context, npcPlacement, map.TileSize);
            }
        }

        DrawCharacter(context, session.PlayerTilePosition, session.FacingDirection, GetPlayerPalette(), true);
        context.SpriteBatch.End();

        context.SpriteBatch.Begin();
        DrawTopHud(context, map.Name, GetAreaName(session.PlayerTilePosition), session.StatusMessage, session);
        DrawBottomHud(context, GetObjectiveText(session), GetControlText());
        context.SpriteBatch.End();
    }

    private static Point GetStep(PyGame.Core.Input.InputSnapshot input)
    {
        if (input.WasPressed(Keys.Up) || input.WasPressed(Keys.W)) return new Point(0, -1);
        if (input.WasPressed(Keys.Down) || input.WasPressed(Keys.S)) return new Point(0, 1);
        if (input.WasPressed(Keys.Left) || input.WasPressed(Keys.A)) return new Point(-1, 0);
        if (input.WasPressed(Keys.Right) || input.WasPressed(Keys.D)) return new Point(1, 0);
        return Point.Zero;
    }

    private static void TryMove(Point step, WorldMap map, GameContext context)
    {
        var session = context.Session;
        var target = session.PlayerTilePosition + step;

        if (!map.IsWalkable(target))
        {
            session.StatusMessage = "그쪽으로는 갈 수 없습니다.";
            return;
        }

        if (map.TryGetNpcAt(target, out _))
        {
            session.StatusMessage = "누군가 길을 막고 있습니다.";
            return;
        }

        session.PlayerTilePosition = target;

        if (map.TryGetWarpAt(target, out var warp))
        {
            session.CurrentMapId = warp.TargetMapId;
            session.PlayerTilePosition = new Point(warp.TargetX, warp.TargetY);
            session.StatusMessage = $"{context.Definitions.Maps[warp.TargetMapId].Name}에 도착했습니다.";
            return;
        }

        TryCollectPickup(map, target, context);

        if (!map.IsEncounterTile(target) || map.Encounters.Count == 0 || Random.Shared.NextDouble() >= 0.18d)
        {
            CheckTrainerSight(map, context);
            return;
        }

        if (!session.Party.EnsureUsableLead())
        {
            session.StatusMessage = "파티를 먼저 회복해야 합니다.";
            return;
        }

        var encounterDefinition = map.Encounters[Random.Shared.Next(map.Encounters.Count)];
        var species = context.Definitions.Species[encounterDefinition.CreatureId];
        var level = Random.Shared.Next(encounterDefinition.MinLevel, encounterDefinition.MaxLevel + 1);

        session.ActiveEncounter = new Encounter
        {
            IsTrainerBattle = false,
            OpponentName = "야생",
            OpponentCreature = Creature.Create(species.Id, species.Name, level)
        };
        session.ReturnState = GameStateId.World;
        session.StatusMessage = $"야생 {species.Name}이 나타났다!";
        context.Audio.PlayBattleStart();
        context.StateManager.ChangeState(GameStateId.Battle);
    }

    private static void TryCollectPickup(WorldMap map, Point target, GameContext context)
    {
        if (!map.TryGetPickupAt(target, out var pickup))
        {
            return;
        }

        if (context.Session.Progression.HasFlag(pickup.CollectedFlag))
        {
            return;
        }

        context.Session.Inventory.Add(pickup.ItemId, pickup.Quantity);
        context.Session.Progression.TryAddFlag(pickup.CollectedFlag);
        var item = context.Definitions.Items[pickup.ItemId];
        context.Session.StatusMessage = $"{item.Name} {pickup.Quantity}개를 주웠다.";
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
            if (!CanStartTrainerBattle(npc, session.Progression))
            {
                continue;
            }

            if (!IsPlayerInSight(session.PlayerTilePosition, placement, map))
            {
                continue;
            }

            StartTrainerBattle(npc, context, string.IsNullOrWhiteSpace(npc.TrainerNoticeText) ? $"{npc.Name}이 당신을 발견했다!" : npc.TrainerNoticeText);
            return;
        }
    }

    private static void StartTrainerBattle(NpcDefinition npc, GameContext context, string battleStartText)
    {
        if (!context.Session.Party.EnsureUsableLead())
        {
            context.Session.StatusMessage = "파티를 먼저 회복해야 합니다.";
            return;
        }

        var species = context.Definitions.Species[npc.TrainerCreatureId];
        context.Session.ActiveEncounter = new Encounter
        {
            IsTrainerBattle = true,
            OpponentName = npc.Name,
            OpponentCreature = Creature.Create(species.Id, species.Name, npc.TrainerCreatureLevel),
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
            var inRange = placement.FacingX > 0
                ? delta > 0 && delta <= placement.SightRange
                : delta < 0 && -delta <= placement.SightRange;

            if (!inRange)
            {
                return false;
            }

            var distance = Math.Abs(delta);
            for (var i = 1; i < distance; i++)
            {
                var sightPoint = new Point(placement.X + (placement.FacingX * i), placement.Y);
                if (!map.IsWalkable(sightPoint))
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
        var verticalInRange = placement.FacingY > 0
            ? verticalDelta > 0 && verticalDelta <= placement.SightRange
            : verticalDelta < 0 && -verticalDelta <= placement.SightRange;

        if (!verticalInRange)
        {
            return false;
        }

        var verticalDistance = Math.Abs(verticalDelta);
        for (var i = 1; i < verticalDistance; i++)
        {
            var sightPoint = new Point(placement.X, placement.Y + (placement.FacingY * i));
            if (!map.IsWalkable(sightPoint))
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
            var tileRect = new Rectangle(point.X * tileSize, point.Y * tileSize, tileSize, tileSize);
            context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 10, tileRect.Y + 10, tileRect.Width - 20, tileRect.Height - 20), new Color(248, 236, 176, 36));
        }
    }

    private static void DrawPickup(GameContext context, WorldPickup pickup, int tileSize)
    {
        var tileRect = new Rectangle(pickup.X * tileSize, pickup.Y * tileSize, tileSize, tileSize);
        context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 10, tileRect.Y + 20, 12, 6), new Color(250, 246, 232));
        context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 10, tileRect.Y + 10, 12, 11), new Color(208, 64, 72));
        context.PrimitiveRenderer.Fill(new Rectangle(tileRect.X + 11, tileRect.Y + 14, 10, 4), new Color(250, 242, 212));
        context.PrimitiveRenderer.Outline(new Rectangle(tileRect.X + 10, tileRect.Y + 10, 12, 16), 1, new Color(60, 34, 34));
    }

    private static void DrawTopHud(GameContext context, string mapName, string areaName, string statusMessage, GameSession session)
    {
        context.PrimitiveRenderer.Fill(new Rectangle(16, 16, 420, 94), new Color(10, 18, 24, 220));
        context.PrimitiveRenderer.Outline(new Rectangle(16, 16, 420, 94), 2, new Color(198, 176, 96));
        context.TextRenderer.DrawText(new Vector2(30, 28), mapName, 2, new Color(246, 236, 192));
        context.TextRenderer.DrawText(new Vector2(30, 54), areaName, 2, new Color(214, 226, 212));
        context.TextRenderer.DrawText(new Vector2(30, 80), statusMessage, 2, new Color(220, 228, 210));

        context.PrimitiveRenderer.Fill(new Rectangle(708, 16, 236, 94), new Color(10, 18, 24, 220));
        context.PrimitiveRenderer.Outline(new Rectangle(708, 16, 236, 94), 2, new Color(198, 176, 96));
        context.TextRenderer.DrawText(new Vector2(724, 28), $"소지금 {session.Money}", 2, new Color(246, 236, 192));
        context.TextRenderer.DrawText(new Vector2(724, 54), $"마크 {session.Progression.BadgeCount}  진행 {session.Progression.Flags.Count}", 2, new Color(220, 228, 210));
        context.TextRenderer.DrawText(new Vector2(724, 80), $"선두 {session.Party.ActiveCreature.Nickname}", 2, new Color(220, 228, 210));
    }

    private static void DrawBottomHud(GameContext context, string objectiveText, string controlText)
    {
        context.PrimitiveRenderer.Fill(new Rectangle(16, 426, 928, 98), new Color(10, 18, 24, 220));
        context.PrimitiveRenderer.Outline(new Rectangle(16, 426, 928, 98), 2, new Color(198, 176, 96));
        context.TextRenderer.DrawText(new Vector2(34, 442), "현재 목표", 2, new Color(246, 236, 192));
        context.TextRenderer.DrawText(new Vector2(34, 470), objectiveText, 2, new Color(216, 226, 232));
        context.TextRenderer.DrawText(new Vector2(34, 496), controlText, 2, new Color(216, 226, 232));
    }

    private static string GetAreaName(Point tilePosition)
    {
        return tilePosition.X switch
        {
            <= 10 => "새싹마을 광장",
            <= 19 => "동쪽 1번길",
            _ => "바람숲 입구"
        };
    }

    private static string GetObjectiveText(GameSession session)
    {
        if (!session.Progression.HasFlag("objective_route_challenge"))
        {
            return "광장의 안내인에게 말을 걸어 첫 목표를 받으세요.";
        }

        if (!session.Progression.HasFlag("trainer_route_scout_defeated"))
        {
            return "동쪽 1번길의 정찰원 리노를 이기고 길을 여세요.";
        }

        if (!session.Progression.HasFlag("milestone_route_mark_claimed"))
        {
            return "안내인에게 돌아가 바람길 마크와 보상을 받으세요.";
        }

        if (!session.Progression.HasFlag("pickup_grove_sphere"))
        {
            return "바람숲 입구를 둘러보고 반짝이는 물건도 챙겨 보세요.";
        }

        return "마을 시설을 이용하며 다음 지역으로 떠날 준비를 하세요.";
    }

    private static string GetControlText()
    {
        return "이동 WASD / 방향키  조사 Enter / Space  가방 B  파티 P  일시정지 ESC";
    }

    private static void DrawCharacter(GameContext context, Point tilePosition, Point facingDirection, CharacterPalette palette, bool isPlayer)
    {
        var tileSize = context.Definitions.Maps[context.Session.CurrentMapId].TileSize;
        var baseX = tilePosition.X * tileSize;
        var baseY = tilePosition.Y * tileSize;

        context.PrimitiveRenderer.Fill(new Rectangle(baseX + 8, baseY + 24, 16, 4), new Color(0, 0, 0, 70));
        context.PrimitiveRenderer.Fill(new Rectangle(baseX + 11, baseY + 8, 10, 10), palette.Head);
        context.PrimitiveRenderer.Fill(new Rectangle(baseX + 10, baseY + 16, 12, 10), palette.Body);
        context.PrimitiveRenderer.Fill(new Rectangle(baseX + 10, baseY + 25, 5, 5), palette.Accent);
        context.PrimitiveRenderer.Fill(new Rectangle(baseX + 17, baseY + 25, 5, 5), palette.Accent);
        context.PrimitiveRenderer.Fill(new Rectangle(baseX + 9, baseY + 7, 14, 3), palette.Hat);
        context.PrimitiveRenderer.Fill(new Rectangle(baseX + 8, baseY + 17, 2, 8), palette.Hat);
        context.PrimitiveRenderer.Fill(new Rectangle(baseX + 22, baseY + 17, 2, 8), palette.Hat);
        context.PrimitiveRenderer.Outline(new Rectangle(baseX + 10, baseY + 8, 12, 18), 1, new Color(32, 28, 26));

        if (facingDirection.Y > 0)
        {
            context.PrimitiveRenderer.Fill(new Rectangle(baseX + 13, baseY + 18, 2, 2), new Color(32, 28, 26));
            context.PrimitiveRenderer.Fill(new Rectangle(baseX + 17, baseY + 18, 2, 2), new Color(32, 28, 26));
            if (isPlayer)
            {
                context.PrimitiveRenderer.Fill(new Rectangle(baseX + 11, baseY + 16, 10, 4), new Color(98, 124, 60));
            }
        }
        else if (facingDirection.Y < 0)
        {
            context.PrimitiveRenderer.Fill(new Rectangle(baseX + 12, baseY + 9, 8, 2), palette.Accent);
            if (isPlayer)
            {
                context.PrimitiveRenderer.Fill(new Rectangle(baseX + 11, baseY + 21, 10, 5), new Color(98, 124, 60));
            }
        }
        else if (facingDirection.X > 0)
        {
            context.PrimitiveRenderer.Fill(new Rectangle(baseX + 18, baseY + 17, 2, 2), new Color(32, 28, 26));
            if (isPlayer)
            {
                context.PrimitiveRenderer.Fill(new Rectangle(baseX + 10, baseY + 17, 3, 8), new Color(98, 124, 60));
            }
        }
        else if (facingDirection.X < 0)
        {
            context.PrimitiveRenderer.Fill(new Rectangle(baseX + 12, baseY + 17, 2, 2), new Color(32, 28, 26));
            if (isPlayer)
            {
                context.PrimitiveRenderer.Fill(new Rectangle(baseX + 19, baseY + 17, 3, 8), new Color(98, 124, 60));
            }
        }

        if (isPlayer)
        {
            context.PrimitiveRenderer.Outline(new Rectangle(baseX + 9, baseY + 7, 14, 23), 1, new Color(250, 234, 140));
        }
    }

    private static CharacterPalette GetPlayerPalette()
    {
        return new CharacterPalette(
            new Color(224, 198, 140),
            new Color(208, 82, 68),
            new Color(52, 78, 148),
            new Color(242, 236, 166));
    }

    private static CharacterPalette GetNpcPalette(NpcDefinition npc, bool defeated)
    {
        var palette = npc.VisualStyle switch
        {
            "elder" => new CharacterPalette(
                new Color(228, 206, 162),
                new Color(118, 92, 160),
                new Color(92, 70, 124),
                new Color(238, 228, 184)),
            "healer" => new CharacterPalette(
                new Color(236, 210, 182),
                new Color(248, 244, 246),
                new Color(188, 72, 96),
                new Color(242, 192, 208)),
            "shopkeeper" => new CharacterPalette(
                new Color(232, 208, 178),
                new Color(196, 146, 74),
                new Color(112, 76, 38),
                new Color(248, 230, 156)),
            "scout" => new CharacterPalette(
                new Color(226, 202, 152),
                new Color(94, 154, 96),
                new Color(74, 96, 62),
                new Color(238, 224, 154)),
            _ => new CharacterPalette(
                new Color(224, 198, 150),
                new Color(72, 138, 174),
                new Color(62, 86, 124),
                new Color(250, 238, 178))
        };

        return defeated
            ? new CharacterPalette(
                new Color(Math.Max(50, palette.Head.R - 60), Math.Max(50, palette.Head.G - 60), Math.Max(50, palette.Head.B - 60)),
                new Color(Math.Max(50, palette.Body.R - 60), Math.Max(50, palette.Body.G - 60), Math.Max(50, palette.Body.B - 60)),
                new Color(Math.Max(50, palette.Hat.R - 60), Math.Max(50, palette.Hat.G - 60), Math.Max(50, palette.Hat.B - 60)),
                new Color(Math.Max(50, palette.Accent.R - 60), Math.Max(50, palette.Accent.G - 60), Math.Max(50, palette.Accent.B - 60)))
            : palette;
    }

    private readonly record struct CharacterPalette(Color Head, Color Body, Color Hat, Color Accent);
}
