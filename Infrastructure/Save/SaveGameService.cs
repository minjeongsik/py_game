using System.Text.Json;
using Microsoft.Xna.Framework;
using PyGame.Domain.Creatures;
using PyGame.Domain.Inventory;
using PyGame.Domain.Party;
using PyGame.Domain.Progression;
using PyGame.GameFlow;
using PyGame.Infrastructure.Content;

namespace PyGame.Infrastructure.Save;

public sealed class SaveGameService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string SavePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PyGame", "savegame.json");

    public bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public SaveOperationResult Save(GameSession session)
    {
        try
        {
            var directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var payload = SaveGameData.FromSession(session);
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            File.WriteAllText(SavePath, json);
            return new SaveOperationResult(true, "모험을 저장했습니다.");
        }
        catch
        {
            return new SaveOperationResult(false, "저장에 실패했습니다.");
        }
    }

    public LoadOperationResult TryLoad(GameDefinitions definitions)
    {
        if (!HasSave())
        {
            return new LoadOperationResult(false, null, "저장된 모험이 없습니다.");
        }

        try
        {
            var json = File.ReadAllText(SavePath);
            var payload = JsonSerializer.Deserialize<SaveGameData>(json);
            if (payload is null)
            {
                return new LoadOperationResult(false, null, "저장 데이터를 읽을 수 없습니다.");
            }

            var session = payload.ToSession(definitions);
            session.StatusMessage = "저장한 모험을 이어갑니다.";
            return new LoadOperationResult(true, session, "저장한 모험을 불러왔습니다.");
        }
        catch
        {
            return new LoadOperationResult(false, null, "저장 데이터를 불러오지 못했습니다.");
        }
    }

    private sealed class SaveGameData
    {
        public int Version { get; init; } = 1;
        public string CurrentMapId { get; init; } = string.Empty;
        public PointData PlayerTilePosition { get; init; } = new();
        public PointData FacingDirection { get; init; } = new();
        public string RecoveryMapId { get; init; } = string.Empty;
        public PointData RecoveryTilePosition { get; init; } = new();
        public int Money { get; init; }
        public int ActivePartyIndex { get; init; }
        public List<CreatureData> Party { get; init; } = [];
        public List<CreatureData> Storage { get; init; } = [];
        public List<InventorySlotData> Inventory { get; init; } = [];
        public List<string> ProgressionFlags { get; init; } = [];
        public int BadgeCount { get; init; }

        public static SaveGameData FromSession(GameSession session)
        {
            return new SaveGameData
            {
                CurrentMapId = session.CurrentMapId,
                PlayerTilePosition = PointData.FromPoint(session.PlayerTilePosition),
                FacingDirection = PointData.FromPoint(session.FacingDirection),
                RecoveryMapId = session.RecoveryMapId,
                RecoveryTilePosition = PointData.FromPoint(session.RecoveryTilePosition),
                Money = session.Money,
                ActivePartyIndex = session.Party.ActiveIndex,
                Party = session.Party.Members.Select(CreatureData.FromCreature).ToList(),
                Storage = session.Storage.Creatures.Select(CreatureData.FromCreature).ToList(),
                Inventory = session.Inventory.Slots.Select(InventorySlotData.FromSlot).ToList(),
                ProgressionFlags = session.Progression.Flags.OrderBy(flag => flag, StringComparer.Ordinal).ToList(),
                BadgeCount = session.Progression.BadgeCount
            };
        }

        public GameSession ToSession(GameDefinitions definitions)
        {
            var currentMapId = definitions.Maps.ContainsKey(CurrentMapId) ? CurrentMapId : definitions.Maps.Keys.First();
            var recoveryMapId = definitions.Maps.ContainsKey(RecoveryMapId) ? RecoveryMapId : currentMapId;

            var party = new Party();
            party.Restore(Party.Select(entry => entry.ToCreature()).ToList(), ActivePartyIndex);

            var storage = new CreatureStorage();
            storage.Restore(Storage.Select(entry => entry.ToCreature()).ToList());

            var inventory = new InventoryBag();
            inventory.Restore(Inventory.Select(entry => entry.ToSlot()).ToList());

            var progression = new GameProgression();
            progression.Restore(ProgressionFlags, BadgeCount);

            var currentMap = definitions.Maps[currentMapId];
            var recoveryMap = definitions.Maps[recoveryMapId];
            var session = new GameSession
            {
                CurrentMapId = currentMapId,
                PlayerTilePosition = RestorePoint(currentMap, PlayerTilePosition.ToPoint()),
                FacingDirection = FacingDirection.ToFacingPoint(),
                RecoveryMapId = recoveryMapId,
                RecoveryTilePosition = RestorePoint(recoveryMap, RecoveryTilePosition.ToPoint()),
                Party = party,
                Storage = storage,
                Inventory = inventory,
                Progression = progression,
                Money = Money
            };

            if (session.Party.Count == 0)
            {
                var starterSpecies = definitions.Species["sproutle"];
                session.Party.Add(Creature.Create(starterSpecies.Id, starterSpecies.Name, 5));
            }

            return session;
        }

        private static Point RestorePoint(Domain.World.WorldMap map, Point point)
        {
            var clamped = new Point(
                Math.Clamp(point.X, 0, Math.Max(0, map.Width - 1)),
                Math.Clamp(point.Y, 0, Math.Max(0, map.Height - 1)));

            if (map.IsWalkable(clamped))
            {
                return clamped;
            }

            for (var radius = 1; radius <= Math.Max(map.Width, map.Height); radius++)
            {
                for (var offsetY = -radius; offsetY <= radius; offsetY++)
                {
                    for (var offsetX = -radius; offsetX <= radius; offsetX++)
                    {
                        var candidate = new Point(clamped.X + offsetX, clamped.Y + offsetY);
                        if (map.IsInBounds(candidate) && map.IsWalkable(candidate))
                        {
                            return candidate;
                        }
                    }
                }
            }

            return new Point(map.SpawnX, map.SpawnY);
        }
    }

    private sealed class CreatureData
    {
        public string SpeciesId { get; init; } = string.Empty;
        public string Nickname { get; init; } = string.Empty;
        public int Level { get; init; }
        public int MaxHealth { get; init; }
        public int CurrentHealth { get; init; }
        public int Experience { get; init; }
        public Dictionary<string, int> MovePp { get; init; } = new(StringComparer.Ordinal);

        public static CreatureData FromCreature(Creature creature)
        {
            return new CreatureData
            {
                SpeciesId = creature.SpeciesId,
                Nickname = creature.Nickname,
                Level = creature.Level,
                MaxHealth = creature.MaxHealth,
                CurrentHealth = creature.CurrentHealth,
                Experience = creature.Experience,
                MovePp = creature.MovePp.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal)
            };
        }

        public Creature ToCreature()
        {
            return new Creature
            {
                SpeciesId = SpeciesId,
                Nickname = Nickname,
                Level = Level,
                MaxHealth = MaxHealth,
                CurrentHealth = Math.Clamp(CurrentHealth, 0, MaxHealth),
                Experience = Math.Max(0, Experience),
                MovePp = MovePp.ToDictionary(entry => entry.Key, entry => Math.Max(0, entry.Value), StringComparer.Ordinal)
            };
        }
    }

    private sealed class InventorySlotData
    {
        public string ItemId { get; init; } = string.Empty;
        public int Quantity { get; init; }

        public static InventorySlotData FromSlot(InventorySlot slot)
        {
            return new InventorySlotData
            {
                ItemId = slot.ItemId,
                Quantity = slot.Quantity
            };
        }

        public InventorySlot ToSlot()
        {
            return new InventorySlot
            {
                ItemId = ItemId,
                Quantity = Quantity
            };
        }
    }

    private sealed class PointData
    {
        public int X { get; init; }
        public int Y { get; init; }

        public static PointData FromPoint(Point point)
        {
            return new PointData { X = point.X, Y = point.Y };
        }

        public Point ToPoint() => new(X, Y);

        public Point ToFacingPoint()
        {
            if (X == 0 && Y == 0)
            {
                return new Point(0, 1);
            }

            return new Point(Math.Clamp(X, -1, 1), Math.Clamp(Y, -1, 1));
        }
    }
}

public readonly record struct SaveOperationResult(bool Success, string Message);
public readonly record struct LoadOperationResult(bool Success, GameSession? Session, string Message);
