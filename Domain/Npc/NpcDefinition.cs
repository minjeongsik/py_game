namespace PyGame.Domain.Npc;

public sealed class NpcDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string VisualStyle { get; init; } = "villager";
    public List<string> DialogueLines { get; init; } = [];
    public List<string> ConditionalDialogueLines { get; init; } = [];
    public string ConditionalDialogueFlag { get; init; } = string.Empty;
    public string GrantsFlagOnTalk { get; init; } = string.Empty;
    public string RewardRequiredFlag { get; init; } = string.Empty;
    public string RewardFlagOnTalk { get; init; } = string.Empty;
    public int RewardMoney { get; init; }
    public int RewardBadgeCount { get; init; }
    public string RewardItemId { get; init; } = string.Empty;
    public int RewardItemQuantity { get; init; }
    public List<string> RewardDialogueLines { get; init; } = [];
    public bool HealsParty { get; init; }
    public bool OpensShop { get; init; }
    public List<string> ShopItemIds { get; init; } = [];
    public bool StartsTrainerBattle { get; init; }
    public string TrainerDefeatedFlag { get; init; } = string.Empty;
    public string TrainerRequiredFlag { get; init; } = string.Empty;
    public string TrainerVictoryFlag { get; init; } = string.Empty;
    public string TrainerNoticeText { get; init; } = string.Empty;
    public string TrainerCreatureId { get; init; } = "embercub";
    public int TrainerCreatureLevel { get; init; } = 5;
}
