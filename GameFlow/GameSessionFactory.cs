using Microsoft.Xna.Framework;
using PyGame.Domain.Creatures;
using PyGame.Domain.Inventory;
using PyGame.Domain.Party;
using PyGame.Domain.Progression;
using PyGame.Infrastructure.Content;

namespace PyGame.GameFlow;

public static class GameSessionFactory
{
    public static GameSession CreateNew(GameDefinitions definitions)
    {
        var starterSpecies = definitions.Species["sproutle"];
        var starter = Creature.Create(starterSpecies.Id, starterSpecies.Name, 5);
        var firstMap = definitions.Maps["new_bark_town"];

        var party = new Party();
        party.Add(starter);
        var storage = new CreatureStorage();

        var inventory = new InventoryBag();
        inventory.Add("potion", 2);
        inventory.Add("capture-sphere", 3);

        return new GameSession
        {
            CurrentMapId = firstMap.Id,
            PlayerTilePosition = new Point(firstMap.SpawnX, firstMap.SpawnY),
            RecoveryMapId = firstMap.Id,
            RecoveryTilePosition = new Point(
                firstMap.RecoveryX == 0 ? firstMap.SpawnX : firstMap.RecoveryX,
                firstMap.RecoveryY == 0 ? firstMap.SpawnY : firstMap.RecoveryY),
            Party = party,
            Storage = storage,
            Inventory = inventory,
            Progression = new GameProgression(),
            Money = 160,
            StatusMessage = "방향키나 WASD로 이동하고 P로 파티를 엽니다."
        };
    }
}
