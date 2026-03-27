using PyGame.Core;
using PyGame.Creatures;
using PyGame.Data;
using PyGame.World;

namespace PyGame.Battle;

public enum BattleMenuMode
{
    Root,
    SelectMove,
    SelectItem
}

public enum BattleOutcome
{
    None,
    Victory,
    Defeat,
    Captured,
    Ran
}

public readonly record struct BattleResolution(bool Ended, BattleOutcome Outcome)
{
    public static BattleResolution Continue() => new(false, BattleOutcome.None);
    public static BattleResolution End(BattleOutcome outcome) => new(true, outcome);
}

public sealed class BattleController
{
    private readonly GameContentDatabase _db;
    private readonly GameSession _session;
    private readonly EncounterService _encounterService;

    private CreatureInstance? _wild;

    public BattleController(GameContentDatabase db, GameSession session, EncounterService encounterService)
    {
        _db = db;
        _session = session;
        _encounterService = encounterService;
    }

    public bool IsActive => _wild is not null;
    public BattleMenuMode MenuMode { get; set; } = BattleMenuMode.Root;
    public int RootSelection { get; set; }
    public int MoveSelection { get; set; }
    public int ItemSelection { get; set; }
    public List<string> Log { get; } = [];

    public void Start(CreatureInstance wild)
    {
        _wild = wild;
        RootSelection = 0;
        MoveSelection = 0;
        ItemSelection = 0;
        MenuMode = BattleMenuMode.Root;
        Log.Clear();
        Log.Add($"A WILD {wild.Nickname.ToUpperInvariant()} APPEARED!");
    }

    public List<string> GetRootOptions() => ["ATTACK", "ITEM", "CAPTURE", "RUN"];

    public List<string> GetMoveOptions()
    {
        if (_wild is null)
        {
            return [];
        }

        return _session.ActiveCreature.EquippedMoveIds
            .Where(id => _db.Moves.ContainsKey(id))
            .Select(id => _db.Moves[id].Name.ToUpperInvariant())
            .ToList();
    }

    public List<string> GetItemOptions()
    {
        return _session.Inventory
            .Where(x => x.Quantity > 0 && _db.Items.TryGetValue(x.ItemId, out _))
            .Select(x =>
            {
                var item = _db.Items[x.ItemId];
                return $"{item.Name.ToUpperInvariant()} x{x.Quantity}";
            })
            .ToList();
    }

    public string BuildStatusLine()
    {
        if (_wild is null)
        {
            return "NO ACTIVE BATTLE";
        }

        var ally = _session.ActiveCreature;
        return $"{ally.Nickname.ToUpperInvariant()} HP {ally.CurrentVitality}/{ally.MaxVitality} | " +
               $"WILD {_wild.Nickname.ToUpperInvariant()} HP {_wild.CurrentVitality}/{_wild.MaxVitality}";
    }

    public BattleResolution UseMove(int selectedMoveIndex)
    {
        if (_wild is null)
        {
            return BattleResolution.Continue();
        }

        var ally = _session.ActiveCreature;
        var moveIds = ally.EquippedMoveIds.Where(id => _db.Moves.ContainsKey(id)).ToList();
        if (moveIds.Count == 0)
        {
            Log.Add("NO MOVES AVAILABLE.");
            return EnemyTurn();
        }

        selectedMoveIndex = Math.Clamp(selectedMoveIndex, 0, moveIds.Count - 1);
        var move = _db.Moves[moveIds[selectedMoveIndex]];
        var damage = CalculateDamage(ally, _wild, move.Power);
        _wild.CurrentVitality = Math.Max(0, _wild.CurrentVitality - damage);
        Log.Add($"{ally.Nickname.ToUpperInvariant()} USED {move.Name.ToUpperInvariant()} FOR {damage} DMG.");

        if (_wild.IsFainted)
        {
            Log.Add($"WILD {_wild.Nickname.ToUpperInvariant()} FAINTED.");
            _wild = null;
            return BattleResolution.End(BattleOutcome.Victory);
        }

        return EnemyTurn();
    }

    public BattleResolution UseItem(int selectedItemIndex)
    {
        if (_wild is null)
        {
            return BattleResolution.Continue();
        }

        var usable = _session.Inventory
            .Where(x => x.Quantity > 0 && _db.Items.ContainsKey(x.ItemId))
            .ToList();

        if (usable.Count == 0)
        {
            Log.Add("NO USABLE ITEMS.");
            return BattleResolution.Continue();
        }

        selectedItemIndex = Math.Clamp(selectedItemIndex, 0, usable.Count - 1);
        var entry = usable[selectedItemIndex];
        var item = _db.Items[entry.ItemId];

        if (!_session.ConsumeItem(entry.ItemId))
        {
            Log.Add("ITEM COULD NOT BE USED.");
            return BattleResolution.Continue();
        }

        if (item.HealAmount > 0)
        {
            var ally = _session.ActiveCreature;
            var before = ally.CurrentVitality;
            ally.CurrentVitality = Math.Min(ally.MaxVitality, ally.CurrentVitality + item.HealAmount);
            var healed = ally.CurrentVitality - before;
            Log.Add($"{ally.Nickname.ToUpperInvariant()} RECOVERED {healed} HP.");
            return EnemyTurn();
        }

        if (item.CapturePower > 0)
        {
            return ResolveCapture(item.CapturePower, item.Name);
        }

        Log.Add("NOTHING HAPPENED.");
        return EnemyTurn();
    }

    public BattleResolution TryCapture()
    {
        if (_wild is null)
        {
            return BattleResolution.Continue();
        }

        var captureEntry = _session.Inventory
            .Where(x => x.Quantity > 0 && _db.Items.TryGetValue(x.ItemId, out var item) && item.CapturePower > 0)
            .Select(x => new { x.ItemId, Item = _db.Items[x.ItemId] })
            .FirstOrDefault();

        if (captureEntry is null)
        {
            Log.Add("NO CAPTURE ITEM AVAILABLE.");
            return BattleResolution.Continue();
        }

        if (!_session.ConsumeItem(captureEntry.ItemId))
        {
            Log.Add("FAILED TO USE CAPTURE ITEM.");
            return BattleResolution.Continue();
        }

        return ResolveCapture(captureEntry.Item.CapturePower, captureEntry.Item.Name);
    }

    public BattleResolution TryRun()
    {
        if (_wild is null)
        {
            return BattleResolution.Continue();
        }

        if (_encounterService.RollRunSuccess(_session.ActiveCreature.Speed, _wild.Speed))
        {
            Log.Add("GOT AWAY SAFELY.");
            _wild = null;
            return BattleResolution.End(BattleOutcome.Ran);
        }

        Log.Add("COULDN'T ESCAPE.");
        return EnemyTurn();
    }

    private BattleResolution ResolveCapture(float capturePower, string itemName)
    {
        if (_wild is null)
        {
            return BattleResolution.Continue();
        }

        var vitalityRatio = 1f - ((float)_wild.CurrentVitality / _wild.MaxVitality);
        var finalChance = Math.Clamp(capturePower + (vitalityRatio * 0.45f), 0.05f, 0.95f);
        Log.Add($"USED {itemName.ToUpperInvariant()}. CAPTURE CHANCE {(int)(finalChance * 100)}%.");

        if (_encounterService.RollCapture(finalChance))
        {
            var captured = CloneCreature(_wild);
            var addedToParty = _session.AddCapturedCreature(captured);
            Log.Add(addedToParty ? "CAPTURED! ADDED TO PARTY." : "CAPTURED! SENT TO STORAGE.");
            _wild = null;
            return BattleResolution.End(BattleOutcome.Captured);
        }

        Log.Add("THE CREATURE BROKE FREE.");
        return EnemyTurn();
    }

    private BattleResolution EnemyTurn()
    {
        if (_wild is null)
        {
            return BattleResolution.Continue();
        }

        var ally = _session.ActiveCreature;
        var movePool = _wild.EquippedMoveIds.Where(id => _db.Moves.ContainsKey(id)).ToList();
        var selectedMove = movePool.Count == 0
            ? null
            : _db.Moves[movePool[Random.Shared.Next(0, movePool.Count)]];

        var power = selectedMove?.Power ?? 7;
        var moveName = selectedMove?.Name.ToUpperInvariant() ?? "STRIKE";
        var damage = CalculateDamage(_wild, ally, power);
        ally.CurrentVitality = Math.Max(0, ally.CurrentVitality - damage);
        Log.Add($"WILD {_wild.Nickname.ToUpperInvariant()} USED {moveName} FOR {damage} DMG.");

        if (ally.IsFainted)
        {
            Log.Add($"{ally.Nickname.ToUpperInvariant()} FAINTED.");
            _session.RestorePartyVitality();
            _wild = null;
            return BattleResolution.End(BattleOutcome.Defeat);
        }

        return BattleResolution.Continue();
    }

    private int CalculateDamage(CreatureInstance attacker, CreatureInstance defender, int movePower)
    {
        var baseDamage = movePower + (attacker.Power / 2) - (defender.Guard / 3);
        var variance = _encounterService.NextDamageVariance();
        return Math.Max(1, baseDamage + variance);
    }

    private static CreatureInstance CloneCreature(CreatureInstance source)
    {
        return new CreatureInstance
        {
            SpeciesId = source.SpeciesId,
            Nickname = source.Nickname,
            Level = source.Level,
            MaxVitality = source.MaxVitality,
            CurrentVitality = source.CurrentVitality,
            Power = source.Power,
            Guard = source.Guard,
            Speed = source.Speed,
            EquippedMoveIds = source.EquippedMoveIds.ToList()
        };
    }
}
