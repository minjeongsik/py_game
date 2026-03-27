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
                Name = "새싹 마을",
                SpawnX = 3,
                SpawnY = 10,
                RecoveryX = 4,
                RecoveryY = 4,
                Rows =
                [
                    "TTTTTTTTTTTTTTTTTTTTTT",
                    "T........TT....ggggg.T",
                    "T.####...TT....ggggg.T",
                    "T.#==....TT....ggggg.T",
                    "T.#==....TT....ggggg.T",
                    "T.####...TT....ggggg.T",
                    "T...===......T.......T",
                    "T...=........T.......T",
                    "T...=....===.........T",
                    "T...======.=.........T",
                    "T........=.=.........T",
                    "T........===.........T",
                    "T....................T",
                    "TTTTTTTTTTTTTTTTTTTTTT"
                ],
                Npcs =
                [
                    new NpcPlacement { NpcId = "guide_npc", X = 8, Y = 8 },
                    new NpcPlacement { NpcId = "elder_npc", X = 5, Y = 10 },
                    new NpcPlacement { NpcId = "healer_npc", X = 4, Y = 3 },
                    new NpcPlacement { NpcId = "shopkeeper_npc", X = 7, Y = 3 },
                    new NpcPlacement { NpcId = "route_scout", X = 15, Y = 9, FacingX = -1, FacingY = 0, SightRange = 4 }
                ],
                PcTerminals =
                [
                    new PcTerminal { X = 5, Y = 4, Label = "집 PC" }
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
            new SpeciesDefinition { Id = "sproutle", Name = "새싹몬", PrimaryTypeId = "leaf", MoveIds = ["tackle", "leaf_tap", "vine_slap"] },
            new SpeciesDefinition { Id = "embercub", Name = "불곰치", PrimaryTypeId = "flame", MoveIds = ["tackle", "ember_nip", "cinder_dash"] },
            new SpeciesDefinition { Id = "brookit", Name = "물방울이", PrimaryTypeId = "wave", MoveIds = ["tackle", "splash_jet", "bubble_pop"] }
        };

        var moves = new[]
        {
            new MoveDefinition { Id = "tackle", Name = "몸통박치기", TypeId = "neutral", Power = 5 },
            new MoveDefinition { Id = "leaf_tap", Name = "잎치기", TypeId = "leaf", Power = 6 },
            new MoveDefinition { Id = "vine_slap", Name = "덩굴채찍", TypeId = "leaf", Power = 7 },
            new MoveDefinition { Id = "ember_nip", Name = "불꽃물기", TypeId = "flame", Power = 6 },
            new MoveDefinition { Id = "cinder_dash", Name = "불씨돌진", TypeId = "flame", Power = 7 },
            new MoveDefinition { Id = "splash_jet", Name = "물보라분사", TypeId = "wave", Power = 6 },
            new MoveDefinition { Id = "bubble_pop", Name = "방울탄", TypeId = "wave", Power = 7 }
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
                    "새 기준 세계에 온 걸 환영해.",
                    "사람 앞에서 엔터를 누르면 말을 걸 수 있어.",
                    "동쪽 길의 정찰원 린을 이기고 돌아와."
                ],
                ConditionalDialogueFlag = "trainer_route_scout_defeated",
                ConditionalDialogueLines =
                [
                    "정찰원 린을 이겼구나.",
                    "이제 동쪽 길이 제대로 열린 셈이야.",
                    "준비되면 더 앞으로 나아가 봐."
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
                    "루트 마크를 받았어.",
                    "상금 120원과 상처약 하나를 챙겨 가."
                ],
                VisualStyle = "guide"
            },
            new NpcDefinition
            {
                Id = "elder_npc",
                Name = "장로",
                DialogueLines =
                [
                    "이 마을은 작은 길로 이어진단다.",
                    "나무가 안전한 길의 경계를 알려 주지.",
                    "풀이 무성한 곳엔 몬스터가 숨어 있어."
                ],
                ConditionalDialogueFlag = "trainer_route_scout_defeated",
                ConditionalDialogueLines =
                [
                    "길목의 정찰원은 이미 물러났단다.",
                    "이제 더 동쪽으로 가도 되겠구나."
                ],
                VisualStyle = "elder"
            },
            new NpcDefinition
            {
                Id = "healer_npc",
                Name = "미라 간호사",
                DialogueLines =
                [
                    "파티가 많이 지쳐 보이네.",
                    "잠깐 쉬고 싶을 때는 언제든 찾아와."
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
                    "길에 나갈 준비물이 필요하니?",
                    "기본 물건은 늘 준비해 두고 있단다."
                ],
                VisualStyle = "shopkeeper",
                OpensShop = true,
                ShopItemIds = ["potion", "capture-sphere"]
            },
            new NpcDefinition
            {
                Id = "route_scout",
                Name = "정찰원 린",
                DialogueLines =
                [
                    "안내인이 아직 이쪽으로 보내지 않았어."
                ],
                ConditionalDialogueFlag = "trainer_route_scout_defeated",
                ConditionalDialogueLines =
                [
                    "넌 이미 이 길에서 실력을 보여 줬어."
                ],
                VisualStyle = "scout",
                StartsTrainerBattle = true,
                TrainerDefeatedFlag = "trainer_route_scout_defeated",
                TrainerRequiredFlag = "objective_route_challenge",
                TrainerVictoryFlag = "objective_route_cleared",
                TrainerNoticeText = "정찰원 린이 길목에서 너를 발견했다!",
                TrainerCreatureId = "brookit",
                TrainerCreatureLevel = 6
            }
        };

        return new GameDefinitions(maps.ToDictionary(x => x.Id), species.ToDictionary(x => x.Id), npcs.ToDictionary(x => x.Id), moves.ToDictionary(x => x.Id), items.ToDictionary(x => x.Id));
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
