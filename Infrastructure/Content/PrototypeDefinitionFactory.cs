using PyGame.Domain.Battle;
using PyGame.Domain.Creatures;
using PyGame.Domain.Inventory;
using PyGame.Domain.Npc;
using PyGame.Domain.World;

namespace PyGame.Infrastructure.Content;

internal static class PrototypeDefinitionFactory
{
    public static GameDefinitions Create()
    {
        var maps = CreateMaps().ToDictionary(map => map.Id);
        var species = CreateSpecies().ToDictionary(entry => entry.Id);
        var npcs = CreateNpcs().ToDictionary(entry => entry.Id);
        var moves = CreateMoves().ToDictionary(entry => entry.Id);
        var items = CreateItems().ToDictionary(entry => entry.Id);
        return new GameDefinitions(maps, species, npcs, moves, items);
    }

    private static IEnumerable<WorldMap> CreateMaps()
    {
        yield return new WorldMap
        {
            Id = "new_bark_town",
            Name = "해솔마을",
            AreaLabel = "마을 중심",
            SpawnX = 6,
            SpawnY = 10,
            RecoveryX = 18,
            RecoveryY = 7,
            Rows =
            [
                "TTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                "T....HHH....HHH....TT......T",
                "T....HDH====HDH====TT..gg..T",
                "T....HHH....HHH....TT..gg..T",
                "T..........................T",
                "T..HHH..............HHH....T",
                "T..HDH====........==HDH....T",
                "T..HHH....====.............T",
                "T.........=....gggg........T",
                "T..gggg...=....gggg....TT..T",
                "T..gggg===................=T",
                "T..............TTTT......===",
                "T....HHH...................T",
                "T....HDH====...............T",
                "T....HHH....====...........T",
                "T...............gggg.......T",
                "T................gggg......T",
                "TTTTTTTTTTTTTTTTTTTTTTTTTTTT"
            ],
            Warps =
            [
                new MapWarp { X = 6, Y = 2, TargetMapId = "player_home", TargetX = 4, TargetY = 6, TransitionText = "집 안으로 들어갔다." },
                new MapWarp { X = 13, Y = 2, TargetMapId = "town_mart", TargetX = 4, TargetY = 6, TransitionText = "도구점 안으로 들어갔다." },
                new MapWarp { X = 4, Y = 6, TargetMapId = "guide_house", TargetX = 4, TargetY = 6, TransitionText = "민가 안으로 들어갔다." },
                new MapWarp { X = 21, Y = 6, TargetMapId = "town_clinic", TargetX = 4, TargetY = 7, TransitionText = "치료소 안으로 들어갔다." },
                new MapWarp { X = 27, Y = 11, TargetMapId = "route_1", TargetX = 0, TargetY = 10, TransitionText = "해솔길로 나섰다." }
            ],
            Npcs =
            [
                new NpcPlacement { NpcId = "guide_npc", X = 9, Y = 8, FacingY = 1 },
                new NpcPlacement { NpcId = "town_child", X = 7, Y = 15, FacingX = 1, FacingY = 0 }
            ],
            Pickups =
            [
                new WorldPickup { Id = "town_potion_pickup", X = 17, Y = 4, ItemId = "potion", Quantity = 1, CollectedFlag = "pickup_town_potion" }
            ]
        };

        yield return new WorldMap
        {
            Id = "player_home",
            Name = "주인공의 집",
            AreaLabel = "실내",
            SpawnX = 4,
            SpawnY = 6,
            RecoveryX = 4,
            RecoveryY = 6,
            Rows =
            [
                "#########",
                "#.......#",
                "#..S....#",
                "#.......#",
                "#.......#",
                "#.......#",
                "####D####"
            ],
            Warps = [new MapWarp { X = 4, Y = 6, TargetMapId = "new_bark_town", TargetX = 5, TargetY = 4, TransitionText = "집 밖으로 나왔다." }],
            Npcs = [new NpcPlacement { NpcId = "home_mother", X = 2, Y = 2, FacingY = 1 }]
        };

        yield return new WorldMap
        {
            Id = "guide_house",
            Name = "마을 민가",
            AreaLabel = "실내",
            SpawnX = 4,
            SpawnY = 6,
            RecoveryX = 4,
            RecoveryY = 6,
            Rows =
            [
                "#########",
                "#.......#",
                "#.......#",
                "#...S...#",
                "#.......#",
                "#.......#",
                "####D####"
            ],
            Warps = [new MapWarp { X = 4, Y = 6, TargetMapId = "new_bark_town", TargetX = 4, TargetY = 8, TransitionText = "집 밖으로 나왔다." }],
            Npcs = [new NpcPlacement { NpcId = "elder_npc", X = 5, Y = 2, FacingX = -1, FacingY = 0 }]
        };

        yield return new WorldMap
        {
            Id = "town_mart",
            Name = "해솔 도구점",
            AreaLabel = "상점 실내",
            SpawnX = 4,
            SpawnY = 6,
            RecoveryX = 4,
            RecoveryY = 6,
            Rows =
            [
                "#########",
                "#..CCC..#",
                "#.......#",
                "#.......#",
                "#...S...#",
                "#.......#",
                "####D####"
            ],
            Warps = [new MapWarp { X = 4, Y = 6, TargetMapId = "new_bark_town", TargetX = 13, TargetY = 4, TransitionText = "도구점 밖으로 나왔다." }],
            Npcs = [new NpcPlacement { NpcId = "shopkeeper_npc", X = 4, Y = 2, FacingY = 1 }]
        };

        yield return new WorldMap
        {
            Id = "town_clinic",
            Name = "해솔 치료소",
            AreaLabel = "치료소 실내",
            SpawnX = 4,
            SpawnY = 7,
            RecoveryX = 4,
            RecoveryY = 7,
            Rows =
            [
                "#########",
                "#..CCC..#",
                "#.......#",
                "#..S.S..#",
                "#.......#",
                "#.......#",
                "#.......#",
                "####D####"
            ],
            Warps = [new MapWarp { X = 4, Y = 7, TargetMapId = "new_bark_town", TargetX = 21, TargetY = 8, TransitionText = "치료소 밖으로 나왔다." }],
            Npcs = [new NpcPlacement { NpcId = "healer_npc", X = 4, Y = 2, FacingY = 1 }],
            PcTerminals = [new PcTerminal { X = 2, Y = 4, Label = "치료소 PC" }]
        };

        yield return new WorldMap
        {
            Id = "route_1",
            Name = "해솔길",
            AreaLabel = "첫 번째 길",
            SpawnX = 1,
            SpawnY = 10,
            RecoveryX = 1,
            RecoveryY = 10,
            Rows =
            [
                "TTTTTTTTTTTTTTTTTTTTTTTTTT",
                "Tggg......TT.....gggg....T",
                "Tggg..===.TT.....gggg....T",
                "T.....=...........ggg....T",
                "T..TT.=....gggg..........T",
                "T..TT.====.gggg....TT....T",
                "T.........=........TT....T",
                "T....gggg.=....gggg......T",
                "T....gggg.====.gggg......T",
                "T..............gggg......T",
                "=..............TT........=",
                "T....TT..................T",
                "T....TT....gggg..........T",
                "TTTTTTTTTTTTTTTTTTTTTTTTTT"
            ],
            Warps =
            [
                new MapWarp { X = 0, Y = 10, TargetMapId = "new_bark_town", TargetX = 27, TargetY = 11, TransitionText = "해솔마을로 돌아왔다." },
                new MapWarp { X = 25, Y = 10, TargetMapId = "whisper_grove", TargetX = 1, TargetY = 9, TransitionText = "속삭임 숲으로 들어섰다." }
            ],
            Npcs =
            [
                new NpcPlacement { NpcId = "route_scout", X = 12, Y = 6, FacingX = -1, FacingY = 0, SightRange = 4 },
                new NpcPlacement { NpcId = "route_hiker", X = 20, Y = 3, FacingY = 1 }
            ],
            Pickups = [new WorldPickup { Id = "route_sphere_pickup", X = 18, Y = 8, ItemId = "capture-sphere", Quantity = 1, CollectedFlag = "pickup_route_sphere" }],
            Encounters =
            [
                new EncounterDefinition { CreatureId = "embercub", MinLevel = 3, MaxLevel = 5 },
                new EncounterDefinition { CreatureId = "sproutle", MinLevel = 3, MaxLevel = 4 },
                new EncounterDefinition { CreatureId = "brookit", MinLevel = 3, MaxLevel = 5 }
            ]
        };

        yield return new WorldMap
        {
            Id = "whisper_grove",
            Name = "속삭임 숲",
            AreaLabel = "숲 입구",
            SpawnX = 1,
            SpawnY = 9,
            RecoveryX = 1,
            RecoveryY = 9,
            Rows =
            [
                "TTTTTTTTTTTTTTTTTTTTTTTTT",
                "Tgggg....TT....gggg.....T",
                "Tgggg.==.TT.==.gggg..TT.T",
                "T.....=......=.......TT.T",
                "T.TTTT=..gg..=..TT......T",
                "T.TT..===gg===..TT......T",
                "T.....gggggggg..........T",
                "T.....gggggggg....TT....T",
                "T..TT.......gg....TT....T",
                "=..TT.............ggg...=",
                "T..........TT.....ggg...T",
                "T....gggg..TT...........T",
                "TTTTTTTTTTTTTTTTTTTTTTTTT"
            ],
            Warps =
            [
                new MapWarp { X = 0, Y = 9, TargetMapId = "route_1", TargetX = 24, TargetY = 10, TransitionText = "해솔길로 되돌아왔다." },
                new MapWarp { X = 24, Y = 9, TargetMapId = "pine_village", TargetX = 1, TargetY = 7, TransitionText = "바람개울 마을이 보인다." }
            ],
            Npcs =
            [
                new NpcPlacement { NpcId = "forest_rookie", X = 13, Y = 5, FacingX = -1, FacingY = 0, SightRange = 3 },
                new NpcPlacement { NpcId = "grove_watcher", X = 20, Y = 8, FacingX = -1, FacingY = 0 }
            ],
            Pickups = [new WorldPickup { Id = "grove_potion_pickup", X = 6, Y = 10, ItemId = "potion", Quantity = 1, CollectedFlag = "pickup_grove_potion" }],
            Encounters =
            [
                new EncounterDefinition { CreatureId = "sproutle", MinLevel = 4, MaxLevel = 6 },
                new EncounterDefinition { CreatureId = "brookit", MinLevel = 4, MaxLevel = 5 },
                new EncounterDefinition { CreatureId = "embercub", MinLevel = 4, MaxLevel = 6 }
            ]
        };

        yield return new WorldMap
        {
            Id = "pine_village",
            Name = "바람개울 마을",
            AreaLabel = "새로운 마을",
            SpawnX = 1,
            SpawnY = 7,
            RecoveryX = 1,
            RecoveryY = 7,
            Rows =
            [
                "TTTTTTTTTTTTTTTTTTTTT",
                "T....HHH.....TT.....T",
                "T....HDH===..TT.....T",
                "T....HHH............T",
                "T...................T",
                "T..gggg......HHH....T",
                "T..gggg......HHH....T",
                "====................T",
                "T...................T",
                "T....TTTT...........T",
                "T....TTTT....gggg...T",
                "TTTTTTTTTTTTTTTTTTTTT"
            ],
            Warps = [new MapWarp { X = 0, Y = 7, TargetMapId = "whisper_grove", TargetX = 23, TargetY = 9, TransitionText = "속삭임 숲으로 되돌아갔다." }],
            Npcs =
            [
                new NpcPlacement { NpcId = "pine_guard", X = 10, Y = 4, FacingY = 1 },
                new NpcPlacement { NpcId = "pine_child", X = 6, Y = 8, FacingX = 1, FacingY = 0 }
            ],
            Pickups = [new WorldPickup { Id = "pine_potion_pickup", X = 15, Y = 8, ItemId = "potion", Quantity = 1, CollectedFlag = "pickup_pine_potion" }]
        };
    }

    private static IEnumerable<SpeciesDefinition> CreateSpecies()
    {
        yield return new SpeciesDefinition { Id = "sproutle", Name = "새싹몬", PrimaryTypeId = "leaf", MoveIds = ["tackle", "leaf_tap", "vine_slap"] };
        yield return new SpeciesDefinition { Id = "embercub", Name = "불곰이", PrimaryTypeId = "flame", MoveIds = ["tackle", "ember_nip", "cinder_dash"] };
        yield return new SpeciesDefinition { Id = "brookit", Name = "물방울이", PrimaryTypeId = "wave", MoveIds = ["tackle", "splash_jet", "bubble_pop"] };
    }

    private static IEnumerable<MoveDefinition> CreateMoves()
    {
        yield return new MoveDefinition { Id = "tackle", Name = "몸통박치기", TypeId = "neutral", Power = 5, MaxPp = 30, Accuracy = 95 };
        yield return new MoveDefinition { Id = "leaf_tap", Name = "잎치기", TypeId = "leaf", Power = 6, MaxPp = 26, Accuracy = 95 };
        yield return new MoveDefinition { Id = "vine_slap", Name = "덩굴채찍", TypeId = "leaf", Power = 7, MaxPp = 20, Accuracy = 90 };
        yield return new MoveDefinition { Id = "ember_nip", Name = "불씨물기", TypeId = "flame", Power = 6, MaxPp = 26, Accuracy = 95 };
        yield return new MoveDefinition { Id = "cinder_dash", Name = "불똥돌진", TypeId = "flame", Power = 7, MaxPp = 20, Accuracy = 90 };
        yield return new MoveDefinition { Id = "splash_jet", Name = "물보라분사", TypeId = "wave", Power = 6, MaxPp = 26, Accuracy = 95 };
        yield return new MoveDefinition { Id = "bubble_pop", Name = "방울파열", TypeId = "wave", Power = 7, MaxPp = 20, Accuracy = 90 };
    }

    private static IEnumerable<ItemDefinition> CreateItems()
    {
        yield return new ItemDefinition { Id = "potion", Name = "상처약", Category = "healing", Price = 60, HealAmount = 12 };
        yield return new ItemDefinition { Id = "capture-sphere", Name = "포획구", Category = "capture", Price = 80, HealAmount = 0 };
    }

    private static IEnumerable<NpcDefinition> CreateNpcs()
    {
        yield return new NpcDefinition
        {
            Id = "guide_npc",
            Name = "안내원 유나",
            DialogueLines = ["해솔마을에서 첫 여정을 시작하는구나.", "치료소와 도구점은 북쪽 건물들에 모여 있어.", "동쪽 길의 정찰 트레이너를 넘기면 숲과 다음 마을이 이어져."],
            ConditionalDialogueFlag = "trainer_route_scout_defeated",
            ConditionalDialogueLines = ["해솔길을 잘 넘겼네.", "이제 속삭임 숲을 지나면 바람개울 마을이 보여.", "숲의 풀숲은 넓으니 상처약을 챙겨 가."],
            GrantsFlagOnTalk = "objective_route_challenge",
            RewardRequiredFlag = "trainer_route_scout_defeated",
            RewardFlagOnTalk = "milestone_route_mark_claimed",
            RewardMoney = 120,
            RewardBadgeCount = 1,
            RewardItemId = "potion",
            RewardItemQuantity = 1,
            RewardDialogueLines = ["잘 해냈어. 길잡이 표식을 줄게.", "상금 120원과 상처약 1개도 챙겨 가.", "이제 숲을 지나 다음 마을까지 이어서 걸어 보자."],
            VisualStyle = "guide"
        };

        yield return new NpcDefinition
        {
            Id = "elder_npc",
            Name = "마을 어르신",
            DialogueLines = ["문 안쪽에서 쉬고 가도 된다.", "동쪽 길은 풀숲과 바위가 번갈아 나와서 흐름을 읽기 좋아.", "다음 마을까지는 길이 하나로 이어져 있으니 너무 걱정 말거라."],
            ConditionalDialogueFlag = "trainer_forest_rookie_defeated",
            ConditionalDialogueLines = ["숲도 지나고 나면 이제 제법 여행자 같구나.", "체력과 포획구 관리가 한결 중요해질 게다."],
            VisualStyle = "elder"
        };

        yield return new NpcDefinition
        {
            Id = "home_mother",
            Name = "엄마",
            DialogueLines = ["첫 여행이니 서두르지 말고 하나씩 익혀 가렴.", "해솔마을 집들은 작지만 필요한 준비는 다 할 수 있어."],
            VisualStyle = "elder"
        };

        yield return new NpcDefinition
        {
            Id = "healer_npc",
            Name = "간호사 미라",
            DialogueLines = ["지친 몬스터를 다시 쉬게 해 줄게요.", "치료가 끝나면 숲길도 조금 더 편해질 거예요."],
            VisualStyle = "healer",
            HealsParty = true
        };

        yield return new NpcDefinition
        {
            Id = "shopkeeper_npc",
            Name = "도구점 주인 리아",
            DialogueLines = ["상처약과 포획구만 잘 챙겨도 초반 여정은 훨씬 편해져.", "숲으로 가기 전에는 꼭 수량을 확인해."],
            VisualStyle = "shopkeeper",
            OpensShop = true,
            ShopItemIds = ["potion", "capture-sphere"]
        };

        yield return new NpcDefinition
        {
            Id = "town_child",
            Name = "마을 아이",
            DialogueLines = ["집 안도 들어가 보고 동쪽 길도 걸어 봐!", "속삭임 숲 끝에는 바람개울 마을이 있대."],
            VisualStyle = "guide"
        };

        yield return new NpcDefinition
        {
            Id = "route_hiker",
            Name = "등산객 도윤",
            DialogueLines = ["해솔길은 넓어 보여도 풀숲 위치를 잘 보면 길이 읽혀.", "체력이 닳기 전에 교체와 회복을 섞어 쓰는 게 좋아."],
            VisualStyle = "elder"
        };

        yield return new NpcDefinition
        {
            Id = "grove_watcher",
            Name = "숲지기",
            DialogueLines = ["이 숲을 지나면 바람개울 마을이 바로 나온다.", "풀숲 사이 길이 보여도 트레이너가 서 있는 자리는 조심해."],
            ConditionalDialogueFlag = "objective_grove_cleared",
            ConditionalDialogueLines = ["숲길도 정리했고 이제 다음 마을에 들를 수 있겠네.", "여기서부터는 여정이 진짜 시작이라고 보면 된다."],
            VisualStyle = "elder"
        };

        yield return new NpcDefinition
        {
            Id = "route_scout",
            Name = "정찰원 리노",
            DialogueLines = ["해솔길로 나왔다면 준비가 됐는지 보자."],
            ConditionalDialogueFlag = "trainer_route_scout_defeated",
            ConditionalDialogueLines = ["좋아, 이제 기본은 익혔네."],
            VisualStyle = "scout",
            StartsTrainerBattle = true,
            TrainerDefeatedFlag = "trainer_route_scout_defeated",
            TrainerRequiredFlag = "objective_route_challenge",
            TrainerVictoryFlag = "objective_route_cleared",
            TrainerNoticeText = "정찰원 리노가 길목에서 승부를 걸어왔다!",
            TrainerCreatureId = "brookit",
            TrainerCreatureLevel = 6,
            TrainerRoster = [new TrainerRosterSlot { CreatureId = "brookit", Level = 6 }, new TrainerRosterSlot { CreatureId = "sproutle", Level = 5 }]
        };

        yield return new NpcDefinition
        {
            Id = "forest_rookie",
            Name = "숲지기 수련생",
            DialogueLines = ["속삭임 숲은 짧지만 전투 흐름을 익히기 딱 좋아.", "두 마리 정도는 안정적으로 돌릴 수 있어야 해."],
            ConditionalDialogueFlag = "trainer_forest_rookie_defeated",
            ConditionalDialogueLines = ["좋아, 숲을 지날 준비는 끝났어."],
            VisualStyle = "scout",
            StartsTrainerBattle = true,
            TrainerDefeatedFlag = "trainer_forest_rookie_defeated",
            TrainerVictoryFlag = "objective_grove_cleared",
            TrainerNoticeText = "숲지기 수련생이 풀숲 사이에서 도전해 왔다!",
            TrainerCreatureId = "sproutle",
            TrainerCreatureLevel = 6,
            TrainerRoster = [new TrainerRosterSlot { CreatureId = "sproutle", Level = 6 }, new TrainerRosterSlot { CreatureId = "embercub", Level = 5 }]
        };

        yield return new NpcDefinition
        {
            Id = "pine_guard",
            Name = "바람개울 안내병",
            DialogueLines = ["여기까지 왔다면 첫 여정 구간은 잘 넘긴 셈이야.", "이 마을에서 숨을 고르고 다음 길을 준비하면 된다."],
            VisualStyle = "guide"
        };

        yield return new NpcDefinition
        {
            Id = "pine_child",
            Name = "바람개울 아이",
            DialogueLines = ["숲을 빠져나오면 갑자기 마을이 보여서 신기하지?", "해솔마을보다 조금 더 먼 길도 여기서부터 이어진대."],
            VisualStyle = "guide"
        };
    }
}
