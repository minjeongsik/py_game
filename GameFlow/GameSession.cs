using Microsoft.Xna.Framework;
using PyGame.Domain.Battle;
using PyGame.Domain.Inventory;
using PyGame.Domain.Party;
using PyGame.Domain.Progression;
using PyGame.GameFlow.StateManager;

namespace PyGame.GameFlow;

public sealed class GameSession
{
    public required string CurrentMapId { get; set; }
    public required Point PlayerTilePosition { get; set; }
    public required string RecoveryMapId { get; set; }
    public required Point RecoveryTilePosition { get; set; }
    public Point FacingDirection { get; set; } = new Point(0, 1);
    public required Party Party { get; init; }
    public required CreatureStorage Storage { get; init; }
    public required InventoryBag Inventory { get; init; }
    public required GameProgression Progression { get; init; }
    public int Money { get; set; }
    public IReadOnlyList<string> CurrentShopItemIds { get; set; } = Array.Empty<string>();
    public GameStateId ReturnState { get; set; } = GameStateId.World;
    public DialogueScene? ActiveDialogue { get; set; }
    public Encounter? ActiveEncounter { get; set; }
    public string StatusMessage { get; set; } = "방향키나 WASD로 이동하고, Enter나 Space로 조사하세요.";
}
