using PyGame.Core;
using PyGame.Creatures;
using PyGame.Data;
using PyGame.World;

namespace PyGame.Battle;

public sealed class BattleController
{
    private readonly GameContentDatabase _db;
    private readonly GameSession _session;
    private readonly EncounterService _encounterService;

    public BattleController(GameContentDatabase db, GameSession session, EncounterService encounterService)
    {
        _db = db;
        _session = session;
        _encounterService = encounterService;
    }

    public CreatureInstance? WildCreature { get; private set; }
    public BattleMenuMode MenuMode { get; private set; } = BattleMenuMode.Root;
    public int RootSelection { get; set; }
    public int MoveSelection { get; set; }
    public int ItemSelection { get; set; }
    public List<string> Log { get; } = [];

    public bool IsActive => WildCreature is not null;

    public void Start(CreatureInstance wild)
    {
        WildCreature = wild;
        MenuMode = BattleMenuMode.Root;
        RootSelection = 0;
        MoveSelection = 0;
        ItemSelection = 0;
        Log.Clear();
        LogMessage($"A wild {DisplayName(wild)} appears!");
    }

    public BattleResolution UseMove(int moveIndex)
    {
        var wild = WildCreature!;
        var player = _session.ActiveCreature;
        if (moveIndex < 0 || moveIndex >= player.EquippedMoveIds.Count)
        {
            return BattleResolution.Continue();
        }

        var moveId = player.EquippedMoveIds[moveIndex];
        var move = _db.Moves[moveId];

        ResolveTurn(
            () => PerformAttack(player, wild, move),
            () => PerformWildAttack(wild, player));

        return ResolveBattleEnd();
    }

    public BattleResolution UseItem(int itemIndex)
    {
        var itemEntries = _session.Inventory.Where(x => x.Quantity > 0).ToList();
        if (itemEntries.Count == 0 || itemIndex < 0 || itemIndex >= itemEntries.Count)
        {
            LogMessage("No usable items.");
            return BattleResolution.Continue();
        }

        var entry = itemEntries[itemIndex];
        var item = _db.Items[entry.ItemId];
        var used = _session.ConsumeItem(entry.ItemId);
        if (!used)
        {
            LogMessage("Item use failed.");
            return BattleResolution.Continue();
        }

        var player = _session.ActiveCreature;
        var wild = WildCreature!;

        if (item.Category == "Healing")
        {
            player.CurrentVitality = Math.Min(player.MaxVitality, player.CurrentVitality + item.HealAmount);
            LogMessage($"{DisplayName(player)} restored vitality.");
            PerformWildAttack(wild, player);
            return ResolveBattleEnd();
        }

        LogMessage("That item can't be used now.");
        return BattleResolution.Continue();
    }

    public BattleResolution TryCapture()
    {
        var itemId = "lure_capsule";
        if (!_session.ConsumeItem(itemId))
        {
            LogMessage("No lure capsules left.");
            return BattleResolution.Continue();
        }

        var wild = WildCreature!;
        var hpRatio = Math.Clamp((float)wild.CurrentVitality / wild.MaxVitality, 0.05f, 1f);
        var item = _db.Items[itemId];
        var chance = Math.Clamp(item.CapturePower + (1f - hpRatio) * 0.55f, 0.15f, 0.92f);

        if (_encounterService.RollCapture(chance))
        {
            LogMessage($"Captured {DisplayName(wild)}!");
            var addedToParty = _session.AddCapturedCreature(wild);
            if (!addedToParty)
            {
                LogMessage("Party full. Sent to storage.");
            }

            WildCreature = null;
            return BattleResolution.End(BattleOutcome.Captured);
        }

        LogMessage("Capture failed!");
        PerformWildAttack(wild, _session.ActiveCreature);
        return ResolveBattleEnd();
    }

    public BattleResolution TryRun()
    {
        var player = _session.ActiveCreature;
        var wild = WildCreature!;

        if (_encounterService.RollRunSuccess(player.Speed, wild.Speed))
        {
            LogMessage("Escaped safely.");
            WildCreature = null;
            return BattleResolution.End(BattleOutcome.Ran);
        }

        LogMessage("Couldn't escape!");
        PerformWildAttack(wild, player);
        return ResolveBattleEnd();
    }

    public List<string> GetRootOptions() => ["ATTACK", "ITEM", "CAPTURE", "RUN"];

    public List<string> GetMoveOptions()
    {
        return _session.ActiveCreature.EquippedMoveIds.Select(id => _db.Moves[id].Name.ToUpperInvariant()).ToList();
    }

    public List<string> GetItemOptions()
    {
        return _session.Inventory
            .Where(x => x.Quantity > 0)
            .Select(x => $"{_db.Items[x.ItemId].Name.ToUpperInvariant()} x{x.Quantity}")
            .ToList();
    }

    public string BuildStatusLine()
    {
        var player = _session.ActiveCreature;
        var wild = WildCreature!;
        return $"ALLY {DisplayName(player)} {player.CurrentVitality}/{player.MaxVitality}HP | WILD {DisplayName(wild)} {wild.CurrentVitality}/{wild.MaxVitality}HP";
    }

    private void ResolveTurn(Action playerAction, Action enemyAction)
    {
        var player = _session.ActiveCreature;
        var wild = WildCreature!;

        if (player.Speed >= wild.Speed)
        {
            playerAction();
            if (wild.IsFainted) return;
            enemyAction();
            return;
        }

        enemyAction();
        if (player.IsFainted) return;
        playerAction();
    }

    private void PerformAttack(CreatureInstance attacker, CreatureInstance defender, MoveDefinition move)
    {
        var damage = Math.Max(1, move.Power + attacker.Power - (defender.Guard / 2) + _encounterService.NextDamageVariance());
        defender.CurrentVitality = Math.Max(0, defender.CurrentVitality - damage);
        LogMessage($"{DisplayName(attacker)} used {move.Name.ToUpperInvariant()} ({damage})");
    }

    private void PerformWildAttack(CreatureInstance wild, CreatureInstance player)
    {
        var moveId = wild.EquippedMoveIds.FirstOrDefault() ?? "nudge";
        var move = _db.Moves[moveId];
        PerformAttack(wild, player, move);
    }

    private BattleResolution ResolveBattleEnd()
    {
        if (WildCreature is null)
        {
            return BattleResolution.End(BattleOutcome.Captured);
        }

        if (WildCreature.IsFainted)
        {
            LogMessage("Wild creature was driven off.");
            WildCreature = null;
            return BattleResolution.End(BattleOutcome.Victory);
        }

        var active = _session.ActiveCreature;
        if (active.IsFainted)
        {
            LogMessage($"{DisplayName(active)} fainted!");
            _session.RestorePartyVitality();
            WildCreature = null;
            return BattleResolution.End(BattleOutcome.Defeat);
        }

        return BattleResolution.Continue();
    }

    private void LogMessage(string message)
    {
        Log.Add(message);
        if (Log.Count > 6)
        {
            Log.RemoveAt(0);
        }
    }

    private string DisplayName(CreatureInstance creature)
    {
        return string.IsNullOrWhiteSpace(creature.Nickname) ? _db.Species[creature.SpeciesId].Name : creature.Nickname;
    }
}

public enum BattleMenuMode
{
    Root,
    SelectMove,
    SelectItem
}

public enum BattleOutcome
{
    Victory,
    Defeat,
    Ran,
    Captured
}

public sealed record BattleResolution(bool Ended, BattleOutcome Outcome)
{
    public static BattleResolution Continue() => new(false, BattleOutcome.Victory);
    public static BattleResolution End(BattleOutcome outcome) => new(true, outcome);
}
