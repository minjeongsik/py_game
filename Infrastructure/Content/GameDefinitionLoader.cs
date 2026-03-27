using System.Text.Json;
using PyGame.Domain.Battle;
using PyGame.Domain.Creatures;
using PyGame.Domain.Inventory;
using PyGame.Domain.Npc;
using PyGame.Domain.World;

namespace PyGame.Infrastructure.Content;

public static class GameDefinitionLoader
{
    public static GameDefinitions Load(string path)
    {
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var file = JsonSerializer.Deserialize<GameDefinitionsFile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (file is not null)
            {
                return new GameDefinitions(
                    file.Maps.ToDictionary(x => x.Id),
                    file.Species.ToDictionary(x => x.Id),
                    file.Npcs.ToDictionary(x => x.Id),
                    file.Moves.ToDictionary(x => x.Id),
                    file.Items.ToDictionary(x => x.Id));
            }
        }

        return CreateFallback();
    }

    private static GameDefinitions CreateFallback()
    {
        var maps = new[]
        {
            new WorldMap
            {
                Id = "new_bark_town",
                Name = "새싹마을",
                SpawnX = 4,
                SpawnY = 14,
                RecoveryX = 8,
                RecoveryY = 3,
                Rows =
                [
                    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                    "T..###....TT....gggg....TT.......T",
                    "T..#=#....TT....gggg....TT..gg...T",
                    "T..###..=======.gggg....TT..gg...T",
                    "T......=....==..........TT.......T",
                    "T..T...=................TT.......T",
                    "T..T.======..TT....TTT...........T",
                    "T..T.....=...TT....TTT....gggg...T",
                    "T......====..............gggg....T",
                    "T..###....=..TT....T.....gggg....T",
                    "T..#=#....=..TT....T.....gggg....T",
                    "T..###..=====TT....T.............T",
                    "T.........=.........TTTTT........T",
                    "T..gggg...=...gggg..TTTT..gggg...T",
                    "T..gggg...====gggg..........gg...T",
                    "T..gggg......=gggg....TTT...gg...T",
                    "T.......TT...=.......TTTT...==...T",
                    "T.......TT...=====..........==...T",
                    "T............g..g....TT....ggg...T",
                    "T...gggg.....g..g....TT....ggg...T",
                    "T...gggg..............TT........TT",
                    "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT"
                ],
                Npcs =
                [
                    new NpcPlacement { NpcId = "guide_npc", X = 9, Y = 9 },
                    new NpcPlacement { NpcId = "elder_npc", X = 13, Y = 14 },
                    new NpcPlacement { NpcId = "healer_npc", X = 7, Y = 3 },
                    new NpcPlacement { NpcId = "shopkeeper_npc", X = 7, Y = 10 },
                    new NpcPlacement { NpcId = "town_child", X = 4, Y = 16 },
                    new NpcPlacement { NpcId = "grove_watcher", X = 29, Y = 18, FacingX = -1, FacingY = 0 },
                    new NpcPlacement { NpcId = "route_scout", X = 20, Y = 8, FacingX = -1, FacingY = 0, SightRange = 4 },
                    new NpcPlacement { NpcId = "forest_rookie", X = 27, Y = 18, FacingX = -1, FacingY = 0, SightRange = 3 }
                ],
                PcTerminals =
                [
                    new PcTerminal { X = 7, Y = 9, Label = "마을 PC" }
                ],
                Pickups =
                [
                    new WorldPickup { Id = "town_potion_pickup", X = 11, Y = 4, ItemId = "potion", Quantity = 1, CollectedFlag = "pickup_town_potion" },
                    new WorldPickup { Id = "route_potion_pickup", X = 24, Y = 11, ItemId = "potion", Quantity = 1, CollectedFlag = "pickup_route_potion" },
                    new WorldPickup { Id = "grove_sphere_pickup", X = 30, Y = 18, ItemId = "capture-sphere", Quantity = 1, CollectedFlag = "pickup_grove_sphere" }
                ],
                Encounters =
                [
                    new EncounterDefinition { CreatureId = "embercub", MinLevel = 2, MaxLevel = 4 },
                    new EncounterDefinition { CreatureId = "sproutle", MinLevel = 2, MaxLevel = 3 },
                    new EncounterDefinition { CreatureId = "brookit", MinLevel = 2, MaxLevel = 4 }
                ]
            }
        };

        var species = new[]
        {
            new SpeciesDefinition { Id = "sproutle", Name = "새싹몽", PrimaryTypeId = "leaf", MoveIds = ["tackle", "leaf_tap", "vine_slap"] },
            new SpeciesDefinition { Id = "embercub", Name = "불꼬미", PrimaryTypeId = "flame", MoveIds = ["tackle", "ember_nip", "cinder_dash"] },
            new SpeciesDefinition { Id = "brookit", Name = "물갑돌이", PrimaryTypeId = "wave", MoveIds = ["tackle", "splash_jet", "bubble_pop"] }
        };

        var moves = new[]
        {
            new MoveDefinition { Id = "tackle", Name = "몸통박치기", TypeId = "neutral", Power = 5 },
            new MoveDefinition { Id = "leaf_tap", Name = "잎치기", TypeId = "leaf", Power = 6 },
            new MoveDefinition { Id = "vine_slap", Name = "덩굴채찍", TypeId = "leaf", Power = 7 },
            new MoveDefinition { Id = "ember_nip", Name = "불꽃물기", TypeId = "flame", Power = 6 },
            new MoveDefinition { Id = "cinder_dash", Name = "불씨돌진", TypeId = "flame", Power = 7 },
            new MoveDefinition { Id = "splash_jet", Name = "물보라분사", TypeId = "wave", Power = 6 },
            new MoveDefinition { Id = "bubble_pop", Name = "방울파열", TypeId = "wave", Power = 7 }
        };

        var items = new[]
        {
            new ItemDefinition { Id = "potion", Name = "상처약", Category = "healing", Price = 60, HealAmount = 12 },
            new ItemDefinition { Id = "capture-sphere", Name = "포획구", Category = "capture", Price = 80, HealAmount = 0 }
        };

        var npcs = new[]
        {
            new NpcDefinition
            {
                Id = "guide_npc",
                Name = "안내인",
                DialogueLines =
                [
                    "새싹마을에 온 걸 환영해.",
                    "북쪽 광장에는 치료, 상점, PC가 모여 있어 준비하기 좋아.",
                    "동쪽 길의 정찰 트레이너를 이기고 돌아오면 마을 표식을 줄게."
                ],
                ConditionalDialogueFlag = "trainer_route_scout_defeated",
                ConditionalDialogueLines =
                [
                    "정찰 트레이너를 넘었구나.",
                    "이제 바람숲 입구까지 길이 한결 편해졌어.",
                    "준비가 되면 더 멀리 떠나 보자."
                ],
                GrantsFlagOnTalk = "objective_route_challenge",
                RewardRequiredFlag = "trainer_route_scout_defeated",
                RewardFlagOnTalk = "milestone_route_mark_claimed",
                RewardMoney = 120,
                RewardBadgeCount = 1,
                RewardItemId = "potion",
                RewardItemQuantity = 1,
                RewardDialogueLines =
                [
                    "바람길 마크를 받았다!",
                    "상금 120원과 상처약 1개를 챙겨 가.",
                    "숲 입구까지 둘러보면 다음 여정 준비가 더 쉬워질 거야."
                ],
                VisualStyle = "guide"
            },
            new NpcDefinition
            {
                Id = "elder_npc",
                Name = "노인",
                DialogueLines =
                [
                    "새싹마을 동쪽 길은 넓어 보여도 흐름이 하나로 이어져 있단다.",
                    "길을 따라가다 보면 풀밭과 나무길이 차례로 이어지지.",
                    "회복이 필요하면 광장의 간호사를 먼저 찾으렴."
                ],
                ConditionalDialogueFlag = "trainer_route_scout_defeated",
                ConditionalDialogueLines =
                [
                    "정찰이를 물리쳤다면 숲 입구까지는 한숨 돌릴 수 있겠구나.",
                    "풀숲 옆 반짝임도 놓치지 말아라."
                ],
                VisualStyle = "elder"
            },
            new NpcDefinition
            {
                Id = "healer_npc",
                Name = "미라 간호사",
                DialogueLines =
                [
                    "모험 전에 숨을 고르고 가요.",
                    "지친 몬스터는 언제든 내가 돌봐 줄게요."
                ],
                VisualStyle = "healer",
                HealsParty = true
            },
            new NpcDefinition
            {
                Id = "shopkeeper_npc",
                Name = "상점주인 레아",
                DialogueLines =
                [
                    "동쪽 길로 나가기 전에 기본 물건부터 챙겨요.",
                    "상처약과 포획구는 초반 여정의 필수품이죠."
                ],
                VisualStyle = "shopkeeper",
                OpensShop = true,
                ShopItemIds = ["potion", "capture-sphere"]
            },
            new NpcDefinition
            {
                Id = "town_child",
                Name = "마을아이",
                DialogueLines =
                [
                    "동쪽 길은 길지만, 광장에서 준비만 잘하면 무섭지 않아.",
                    "숲 입구 쪽 반짝이는 물건은 꼭 챙겨!"
                ],
                VisualStyle = "guide"
            },
            new NpcDefinition
            {
                Id = "grove_watcher",
                Name = "숲지기",
                DialogueLines =
                [
                    "여긴 바람숲 입구야.",
                    "조금 더 넓어진 길 끝에 다음 지역으로 이어질 숲이 기다리고 있지."
                ],
                ConditionalDialogueFlag = "milestone_route_mark_claimed",
                ConditionalDialogueLines =
                [
                    "마크를 받았다면 이제 숲 입구를 지날 자격은 충분해.",
                    "준비가 되면 더 안쪽 길도 만들 수 있을 거야."
                ],
                VisualStyle = "elder"
            },
            new NpcDefinition
            {
                Id = "route_scout",
                Name = "정찰원 리노",
                DialogueLines =
                [
                    "안내인이 아직 널 동쪽 길로 보내지 않았나 보네."
                ],
                ConditionalDialogueFlag = "trainer_route_scout_defeated",
                ConditionalDialogueLines =
                [
                    "좋아, 이제 넌 이 길에서 버틸 힘이 있다는 걸 보여 줬어."
                ],
                VisualStyle = "scout",
                StartsTrainerBattle = true,
                TrainerDefeatedFlag = "trainer_route_scout_defeated",
                TrainerRequiredFlag = "objective_route_challenge",
                TrainerVictoryFlag = "objective_route_cleared",
                TrainerNoticeText = "정찰원 리노가 길목에서 당신을 발견했다!",
                TrainerCreatureId = "brookit",
                TrainerCreatureLevel = 6
            },
            new NpcDefinition
            {
                Id = "forest_rookie",
                Name = "숲길 연수생",
                DialogueLines =
                [
                    "숲 입구는 겁먹지 않고 차근차근 보면 길이 보여.",
                    "나와 한 번 겨뤄 보면 더 준비가 될 거야."
                ],
                ConditionalDialogueFlag = "trainer_forest_rookie_defeated",
                ConditionalDialogueLines =
                [
                    "이제 숲길 바람도 견딜 만하지?",
                    "다음 길은 더 넓어질 테니 대비해 둬."
                ],
                VisualStyle = "scout",
                StartsTrainerBattle = true,
                TrainerDefeatedFlag = "trainer_forest_rookie_defeated",
                TrainerVictoryFlag = "objective_grove_cleared",
                TrainerNoticeText = "숲길 연수생이 숲 입구에서 승부를 걸어왔다!",
                TrainerCreatureId = "sproutle",
                TrainerCreatureLevel = 5
            }
        };

        return new GameDefinitions(
            maps.ToDictionary(x => x.Id),
            species.ToDictionary(x => x.Id),
            npcs.ToDictionary(x => x.Id),
            moves.ToDictionary(x => x.Id),
            items.ToDictionary(x => x.Id));
    }

    private sealed class GameDefinitionsFile
    {
        public List<WorldMap> Maps { get; init; } = [];
        public List<SpeciesDefinition> Species { get; init; } = [];
        public List<NpcDefinition> Npcs { get; init; } = [];
        public List<MoveDefinition> Moves { get; init; } = [];
        public List<ItemDefinition> Items { get; init; } = [];
    }
}
