using PyGame.Domain.Battle;
using PyGame.Domain.Creatures;
using PyGame.Domain.Inventory;
using PyGame.Domain.Npc;
using PyGame.Domain.World;

namespace PyGame.Infrastructure.Content;

public sealed class GameDefinitions
{
    public GameDefinitions(
        IReadOnlyDictionary<string, WorldMap> maps,
        IReadOnlyDictionary<string, SpeciesDefinition> species,
        IReadOnlyDictionary<string, NpcDefinition> npcs,
        IReadOnlyDictionary<string, MoveDefinition> moves,
        IReadOnlyDictionary<string, ItemDefinition> items)
    {
        Maps = maps;
        Species = species;
        Npcs = npcs;
        Moves = moves;
        Items = items;
    }

    public IReadOnlyDictionary<string, WorldMap> Maps { get; }
    public IReadOnlyDictionary<string, SpeciesDefinition> Species { get; }
    public IReadOnlyDictionary<string, NpcDefinition> Npcs { get; }
    public IReadOnlyDictionary<string, MoveDefinition> Moves { get; }
    public IReadOnlyDictionary<string, ItemDefinition> Items { get; }
}
