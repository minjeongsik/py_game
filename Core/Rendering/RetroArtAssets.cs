using System.Drawing.Imaging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyGame.Domain.World;
using Bitmap = System.Drawing.Bitmap;
using Color = Microsoft.Xna.Framework.Color;

namespace PyGame.Core.Rendering;

public sealed class RetroArtAssets : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly string _assetRoot;
    private readonly Dictionary<string, Texture2D> _textures = [];
    private readonly HashSet<string> _generatedKeys = [];
    private bool _disposed;

    public RetroArtAssets(GraphicsDevice graphicsDevice, string assetRoot)
    {
        _graphicsDevice = graphicsDevice;
        _assetRoot = assetRoot;
        Directory.CreateDirectory(_assetRoot);
        Directory.CreateDirectory(Path.Combine(_assetRoot, "tiles"));
        Directory.CreateDirectory(Path.Combine(_assetRoot, "characters"));
        Directory.CreateDirectory(Path.Combine(_assetRoot, "creatures"));
        Directory.CreateDirectory(Path.Combine(_assetRoot, "world"));
        CreateTileTextures();
        CreateCharacterTextures();
        CreateCreatureTextures();
        CreateObjectTextures();
        ExportGeneratedTextures();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        _textures.Clear();
        _disposed = true;
    }

    public void DrawWorldTile(SpriteBatch spriteBatch, WorldMap map, Point point)
    {
        var destination = new Rectangle(point.X * map.TileSize, point.Y * map.TileSize, map.TileSize, map.TileSize);
        var tile = map.GetTileType(point);
        switch (tile)
        {
            case TileType.Path:
                Draw(spriteBatch, "tile_path", destination);
                return;
            case TileType.Grass:
                Draw(spriteBatch, map.Id == "whisper_grove" ? "tile_grass_forest" : "tile_grass_field", destination);
                return;
            case TileType.Tree:
                Draw(spriteBatch, "tile_tree", destination);
                return;
            case TileType.Wall:
                Draw(spriteBatch, "tile_wall_interior", destination);
                return;
            case TileType.Counter:
                Draw(spriteBatch, "tile_counter", destination);
                return;
            case TileType.Service:
                Draw(spriteBatch, map.Id == "town_clinic" ? "tile_service_clinic" : "tile_service_shop", destination);
                return;
            case TileType.Building:
                DrawBuildingTile(spriteBatch, map, point, destination);
                return;
            case TileType.Door:
                Draw(spriteBatch, "tile_door", destination);
                return;
            default:
                DrawFloorTile(spriteBatch, map, destination);
                return;
        }
    }

    public void DrawPickup(SpriteBatch spriteBatch, Rectangle destination) => Draw(spriteBatch, "world_pickup", destination);

    public void DrawTerminal(SpriteBatch spriteBatch, Rectangle destination) => Draw(spriteBatch, "world_terminal", destination);

    public void DrawSightMarker(SpriteBatch spriteBatch, Rectangle destination)
    {
        Draw(spriteBatch, "world_sight", destination, new Color(255, 255, 255, 180));
    }

    public void DrawCharacter(SpriteBatch spriteBatch, string visualStyle, Point facingDirection, Rectangle destination, bool defeated, bool alternateFrame = false)
    {
        var key = ResolveCharacterKey(visualStyle, facingDirection, alternateFrame);
        Draw(spriteBatch, key, destination, defeated ? new Color(168, 168, 176) : Color.White);
    }

    public void DrawCreature(SpriteBatch spriteBatch, string speciesId, bool backSprite, Rectangle destination)
    {
        var key = backSprite ? $"creature_back_{speciesId}" : $"creature_front_{speciesId}";
        Draw(spriteBatch, _textures.ContainsKey(key) ? key : "creature_front_sproutle", destination);
    }

    private void DrawFloorTile(SpriteBatch spriteBatch, WorldMap map, Rectangle destination)
    {
        var key = IsInterior(map)
            ? "tile_floor_interior"
            : map.Id switch
            {
                "new_bark_town" or "pine_village" => "tile_floor_town",
                "whisper_grove" => "tile_floor_forest",
                _ => "tile_floor_route"
            };
        Draw(spriteBatch, key, destination);
    }

    private void DrawBuildingTile(SpriteBatch spriteBatch, WorldMap map, Point point, Rectangle destination)
    {
        var left = IsBuildingLike(map, point + new Point(-1, 0));
        var right = IsBuildingLike(map, point + new Point(1, 0));
        var up = IsBuildingLike(map, point + new Point(0, -1));
        var down = IsBuildingLike(map, point + new Point(0, 1));

        var key = !up
            ? left && right ? "tile_roof_mid" : left ? "tile_roof_right" : right ? "tile_roof_left" : "tile_roof_mid"
            : !down
                ? left && right ? "tile_building_base_mid" : left ? "tile_building_base_right" : right ? "tile_building_base_left" : "tile_building_base_mid"
                : left ? "tile_building_wall_right" : right ? "tile_building_wall_left" : "tile_building_wall_mid";

        Draw(spriteBatch, key, destination, GetBuildingTint(map, point));
    }

    private static bool IsInterior(WorldMap map) => map.Id is "player_home" or "guide_house" or "town_mart" or "town_clinic";

    private static bool IsBuildingLike(WorldMap map, Point point)
    {
        var tile = map.GetTileType(point);
        return tile is TileType.Building or TileType.Door;
    }

    private Color GetBuildingTint(WorldMap map, Point point)
    {
        var style = ResolveBuildingStyle(map, point);
        return style switch
        {
            "town_mart" => new Color(214, 168, 88),
            "town_clinic" => new Color(132, 188, 200),
            "player_home" => new Color(182, 96, 92),
            "guide_house" => new Color(150, 116, 82),
            _ => map.Id == "pine_village" ? new Color(170, 112, 96) : new Color(176, 102, 92)
        };
    }

    private static string ResolveBuildingStyle(WorldMap map, Point point)
    {
        for (var offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (var offsetX = -1; offsetX <= 1; offsetX++)
            {
                var candidate = point + new Point(offsetX, offsetY);
                if (map.TryGetWarpAt(candidate, out var warp))
                {
                    return warp.TargetMapId;
                }
            }
        }

        return string.Empty;
    }

    private string ResolveCharacterKey(string visualStyle, Point facingDirection, bool alternateFrame)
    {
        var direction = facingDirection switch
        {
            { X: 0, Y: < 0 } => "up",
            { X: 0, Y: > 0 } => "down",
            { X: < 0 } => "left",
            _ => "right"
        };

        var style = visualStyle switch
        {
            "player" => "player",
            "guide" => "guide",
            "elder" => "elder",
            "healer" => "healer",
            "shopkeeper" => "shopkeeper",
            "scout" => "scout",
            _ => "villager"
        };

        var baseKey = $"char_{style}_{direction}";
        var alternateKey = $"{baseKey}_step";
        return alternateFrame && _textures.ContainsKey(alternateKey) ? alternateKey : baseKey;
    }

    private void Draw(SpriteBatch spriteBatch, string key, Rectangle destination, Color? tint = null)
    {
        spriteBatch.Draw(_textures[key], destination, tint ?? Color.White);
    }

    private void CreateTexture(string key, IReadOnlyList<string> rows, IReadOnlyDictionary<char, Color> palette)
    {
        if (TryLoadExternalTexture(key, out var loaded))
        {
            _textures[key] = loaded;
            return;
        }

        var width = rows[0].Length;
        var height = rows.Count;
        var data = new Color[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var symbol = rows[y][x];
                data[(y * width) + x] = palette.TryGetValue(symbol, out var color) ? color : Color.Transparent;
            }
        }

        var texture = new Texture2D(_graphicsDevice, width, height);
        texture.SetData(data);
        _textures[key] = texture;
        _generatedKeys.Add(key);
    }

    private bool TryLoadExternalTexture(string key, out Texture2D texture)
    {
        var path = ResolveTexturePath(key);
        if (!File.Exists(path))
        {
            texture = null!;
            return false;
        }

        using var stream = File.OpenRead(path);
        texture = Texture2D.FromStream(_graphicsDevice, stream);
        return true;
    }

    private void ExportGeneratedTextures()
    {
        foreach (var key in _generatedKeys)
        {
            var path = ResolveTexturePath(key);
            if (File.Exists(path))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            SaveTexture(path, _textures[key]);
        }
    }

    private string ResolveTexturePath(string key)
    {
        var folder = key switch
        {
            _ when key.StartsWith("tile_", StringComparison.Ordinal) => "tiles",
            _ when key.StartsWith("char_", StringComparison.Ordinal) => "characters",
            _ when key.StartsWith("creature_", StringComparison.Ordinal) => "creatures",
            _ => "world"
        };

        return Path.Combine(_assetRoot, folder, $"{key}.png");
    }

    private static void SaveTexture(string path, Texture2D texture)
    {
        var data = new Color[texture.Width * texture.Height];
        texture.GetData(data);
        using var bitmap = new Bitmap(texture.Width, texture.Height, PixelFormat.Format32bppArgb);
        for (var y = 0; y < texture.Height; y++)
        {
            for (var x = 0; x < texture.Width; x++)
            {
                var color = data[(y * texture.Width) + x];
                bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B));
            }
        }

        bitmap.Save(path, ImageFormat.Png);
    }

    private static IReadOnlyDictionary<char, Color> BuildPalette(Color roof, Color light, Color highlight)
    {
        return new Dictionary<char, Color>
        {
            ['r'] = roof,
            ['l'] = light,
            ['o'] = highlight
        };
    }

    private void CreateTileTextures()
    {
        CreateTexture("tile_floor_town", [
            "pppppppppppppppp","pppppppppppppppp","pp..pppppppp..pp","pppppppppppppppp",
            "pppppp..pppppppp","pppppppppppppppp","pppppppppp..pppp","pppppppppppppppp",
            "pppppppppppppppp","pp..pppppppppppp","pppppppppppppppp","pppppppp..pppppp",
            "pppppppppppppppp","pppppppppppppppp","pppp..pppppppppp","pppppppppppppppp"
        ], new Dictionary<char, Color> { ['p'] = new Color(150, 196, 126), ['.'] = new Color(164, 204, 138) });

        CreateTexture("tile_floor_route", [
            "pppppppppppppppp","pppppppppppppppp","pppppp..pppppppp","pppppppppppppppp",
            "pp..pppppppppppp","pppppppppppppppp","pppppppp..pppppp","pppppppppppppppp",
            "pppppppppppppppp","pppppppppppp..pp","pppppppppppppppp","pppp..pppppppppp",
            "pppppppppppppppp","pppppppppppppppp","pppppppp..pppppp","pppppppppppppppp"
        ], new Dictionary<char, Color> { ['p'] = new Color(142, 184, 114), ['.'] = new Color(156, 194, 124) });

        CreateTexture("tile_floor_forest", [
            "pppppppppppppppp","pppppppppppppppp","pp..pppppppppppp","pppppppppp..pppp",
            "pppppppppppppppp","pppppppppppppppp","pppppp..pppppppp","pppppppppppppppp",
            "pppppppppppppppp","pppppppp..pppppp","pppppppppppppppp","pp..pppppppppppp",
            "pppppppppppppppp","pppppppppppppppp","pppppppppp..pppp","pppppppppppppppp"
        ], new Dictionary<char, Color> { ['p'] = new Color(122, 162, 92), ['.'] = new Color(136, 176, 102) });

        CreateTexture("tile_floor_interior", [
            "llllllllllllllll","lccccccccccccccl","lccccccccccccccl","lccccccccccccccl",
            "llllllllllllllll","lccccccccccccccl","lccccccccccccccl","lccccccccccccccl",
            "llllllllllllllll","lccccccccccccccl","lccccccccccccccl","lccccccccccccccl",
            "llllllllllllllll","lccccccccccccccl","lccccccccccccccl","lccccccccccccccl"
        ], new Dictionary<char, Color> { ['l'] = new Color(204, 188, 150), ['c'] = new Color(194, 176, 138) });

        CreateTexture("tile_path", [
            "ssssssssssssssss","spppppppppppppps","spqqppppqqppppps","spppppppppppppps",
            "sppppppppppppqps","spqqppppppppppps","sppppppqqpppppps","spppppppppppppps",
            "spppqqppppppppps","sppppppppqppppps","spqpppppppppppps","sppppppqqpppppps",
            "spppppppppppppps","spqqppppppppqpps","spppppppppppppps","ssssssssssssssss"
        ], new Dictionary<char, Color>
        {
            ['s'] = new Color(168, 144, 96),
            ['p'] = new Color(212, 194, 140),
            ['q'] = new Color(192, 172, 120)
        });

        CreateTexture("tile_grass_field", [
            "gggggggggggggggg","gggllggglllggggg","gggllggglllggggg","gggggggggggggggg",
            "ggllgggggggllggg","ggllgggggggllggg","gggggggggggggggg","gggglllgggllgggg",
            "gggglllgggllgggg","gggggggggggggggg","gggllggggggggggg","gggllggggglllggg",
            "gggggggggglllggg","gggggggggggggggg","gglllggggggggggg","gggggggggggggggg"
        ], new Dictionary<char, Color> { ['g'] = new Color(96, 156, 74), ['l'] = new Color(56, 112, 46) });

        CreateTexture("tile_grass_forest", [
            "gggggggggggggggg","gggllgggglllgggg","gggllgggglllgggg","gggggggggggggggg",
            "gglllggggggggggg","gglllggggllggggg","gggggggggllggggg","gggglllggggggggg",
            "gggglllggggggggg","gggggggggggggggg","ggllgggggggllggg","ggllgggggggllggg",
            "gggggggggggggggg","ggglllgggllggggg","ggglllgggllggggg","gggggggggggggggg"
        ], new Dictionary<char, Color> { ['g'] = new Color(82, 138, 68), ['l'] = new Color(46, 94, 42) });

        CreateTexture("tile_tree", [
            "....tttttttt....","...tttttttttt...","..tttllllllttt..",".ttlllllllllltt.",
            ".ttlllllllllltt.",".ttlllllllllltt.","..ttlllllllltt..","...ttlllllltt...",
            "...ttlllllltt...","..ttttlllltttt..","..ttttlllltttt..","...tttbbbbttt...",
            "....ttbbbbtt....","....ttbbbbtt....",".....tbbbbt.....","......bbbb......"
        ], new Dictionary<char, Color>
        {
            ['.'] = Color.Transparent,
            ['t'] = new Color(44, 96, 58),
            ['l'] = new Color(72, 138, 80),
            ['b'] = new Color(96, 70, 46)
        });

        CreateTexture("tile_wall_interior", [
            "rrrrrrrrrrrrrrrr","rbbbbbbbbbbbbbbr","rccccccccccccccr","rccccccccccccccr",
            "rbbbbbbbbbbbbbbr","rccccccccccccccr","rccccccccccccccr","rbbbbbbbbbbbbbbr",
            "rccccccccccccccr","rccccccccccccccr","rbbbbbbbbbbbbbbr","rccccccccccccccr",
            "rccccccccccccccr","rbbbbbbbbbbbbbbr","rccccccccccccccr","rrrrrrrrrrrrrrrr"
        ], new Dictionary<char, Color>
        {
            ['r'] = new Color(118, 88, 74),
            ['b'] = new Color(170, 102, 86),
            ['c'] = new Color(214, 194, 166)
        });

        CreateTexture("tile_roof_left", [
            "rrrrrrrrrrrrrrrr","rllllllllllllllr","rllrrrrrrrrrrllr","rlrrrrrrrrrrrrlr",
            "rlrrrrrrrrrrrrlr","rlrrllrrllrrrllr","rlrrllrrllrrrllr","rlrrrrrrrrrrrrlr",
            "rlrrllrrllrrrllr","rlrrllrrllrrrllr","rlrrrrrrrrrrrrlr","rlrrllrrllrrrllr",
            "rlrrllrrllrrrllr","rlrrrrrrrrrrrrlr","rllllllllllllllr","rrrrrrrrrrrrrrrr"
        ], BuildPalette(new Color(112, 72, 62), new Color(192, 150, 132), new Color(230, 206, 194)));

        CreateTexture("tile_roof_mid", [
            "rrrrrrrrrrrrrrrr","rllllllllllllllr","rrllrrrrrrrrllrr","rrrrrrrrrrrrrrrr",
            "rrllrrrrrrrrllrr","rrrrllrrrrllrrrr","rrrrllrrrrllrrrr","rrrrrrrrrrrrrrrr",
            "rrllrrrrrrrrllrr","rrrrllrrrrllrrrr","rrrrllrrrrllrrrr","rrrrrrrrrrrrrrrr",
            "rrllrrrrrrrrllrr","rrrrrrrrrrrrrrrr","rllllllllllllllr","rrrrrrrrrrrrrrrr"
        ], BuildPalette(new Color(112, 72, 62), new Color(192, 150, 132), new Color(230, 206, 194)));

        CreateTexture("tile_roof_right", [
            "rrrrrrrrrrrrrrrr","rllllllllllllllr","rllrrrrrrrrrrllr","rlrrrrrrrrrrrrlr",
            "rlrrrrrrrrrrrrlr","rllrrrllrrllrrlr","rllrrrllrrllrrlr","rlrrrrrrrrrrrrlr",
            "rllrrrllrrllrrlr","rllrrrllrrllrrlr","rlrrrrrrrrrrrrlr","rllrrrllrrllrrlr",
            "rllrrrllrrllrrlr","rlrrrrrrrrrrrrlr","rllllllllllllllr","rrrrrrrrrrrrrrrr"
        ], BuildPalette(new Color(112, 72, 62), new Color(192, 150, 132), new Color(230, 206, 194)));

        CreateTexture("tile_building_wall_left", [
            "oooooooooooooooo","ollllllllllllllo","olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo",
            "olwwnnnnnnnnwwlo","olwwnnnnnnnnwwlo","olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo",
            "olwwnnnnnnnnwwlo","olwwnnnnnnnnwwlo","olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo",
            "olwwnnnnnnnnwwlo","olwwwwwwwwwwwwlo","ollllllllllllllo","oooooooooooooooo"
        ], new Dictionary<char, Color>
        {
            ['o'] = new Color(96, 74, 58),
            ['l'] = new Color(160, 110, 86),
            ['w'] = new Color(220, 202, 174),
            ['n'] = new Color(240, 226, 196)
        });

        CreateTexture("tile_building_wall_mid", [
            "oooooooooooooooo","ollllllllllllllo","olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo",
            "olwwnnnnnnnnwwlo","olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo","olwwnnnnnnnnwwlo",
            "olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo","olwwnnnnnnnnwwlo","olwwwwwwwwwwwwlo",
            "olwwnnnnnnnnwwlo","olwwnnnnnnnnwwlo","ollllllllllllllo","oooooooooooooooo"
        ], new Dictionary<char, Color>
        {
            ['o'] = new Color(96, 74, 58),
            ['l'] = new Color(160, 110, 86),
            ['w'] = new Color(220, 202, 174),
            ['n'] = new Color(240, 226, 196)
        });

        CreateTexture("tile_building_wall_right", [
            "oooooooooooooooo","ollllllllllllllo","olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo",
            "olwwnnnnnnnnwwlo","olwwnnnnnnnnwwlo","olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo",
            "olwwnnnnnnnnwwlo","olwwnnnnnnnnwwlo","olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo",
            "olwwnnnnnnnnwwlo","olwwwwwwwwwwwwlo","ollllllllllllllo","oooooooooooooooo"
        ], new Dictionary<char, Color>
        {
            ['o'] = new Color(96, 74, 58),
            ['l'] = new Color(160, 110, 86),
            ['w'] = new Color(220, 202, 174),
            ['n'] = new Color(240, 226, 196)
        });

        CreateTexture("tile_building_base_left", [
            "oooooooooooooooo","ollllllllllllllo","olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo",
            "olwwnnnnnnnnwwlo","olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo","olwwnnnnnnnnwwlo",
            "olwwwwwwwwwwwwlo","olbbbbbbbbbbbblo","olbbbbbbbbbbbblo","olcccccccccccclo",
            "olcccccccccccclo","olbbbbbbbbbbbblo","ollllllllllllllo","oooooooooooooooo"
        ], new Dictionary<char, Color>
        {
            ['o'] = new Color(96, 74, 58), ['l'] = new Color(160, 110, 86), ['w'] = new Color(220, 202, 174),
            ['n'] = new Color(240, 226, 196), ['b'] = new Color(144, 116, 90), ['c'] = new Color(116, 90, 68)
        });

        CreateTexture("tile_building_base_mid", [
            "oooooooooooooooo","ollllllllllllllo","olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo",
            "olwwnnnnnnnnwwlo","olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo","olwwnnnnnnnnwwlo",
            "olwwwwwwwwwwwwlo","olbbbbbbbbbbbblo","olbbbbbbbbbbbblo","olcccccccccccclo",
            "olcccccccccccclo","olbbbbbbbbbbbblo","ollllllllllllllo","oooooooooooooooo"
        ], new Dictionary<char, Color>
        {
            ['o'] = new Color(96, 74, 58), ['l'] = new Color(160, 110, 86), ['w'] = new Color(220, 202, 174),
            ['n'] = new Color(240, 226, 196), ['b'] = new Color(144, 116, 90), ['c'] = new Color(116, 90, 68)
        });

        CreateTexture("tile_building_base_right", [
            "oooooooooooooooo","ollllllllllllllo","olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo",
            "olwwnnnnnnnnwwlo","olwwwwwwwwwwwwlo","olwwnnnnnnnnwwlo","olwwnnnnnnnnwwlo",
            "olwwwwwwwwwwwwlo","olbbbbbbbbbbbblo","olbbbbbbbbbbbblo","olcccccccccccclo",
            "olcccccccccccclo","olbbbbbbbbbbbblo","ollllllllllllllo","oooooooooooooooo"
        ], new Dictionary<char, Color>
        {
            ['o'] = new Color(96, 74, 58), ['l'] = new Color(160, 110, 86), ['w'] = new Color(220, 202, 174),
            ['n'] = new Color(240, 226, 196), ['b'] = new Color(144, 116, 90), ['c'] = new Color(116, 90, 68)
        });

        CreateTexture("tile_door", [
            "oooooooooooooooo","ollllllllllllllo","olwwwwwwwwwwwwlo","olwwwwbbbbwwwwlo",
            "olwwwbbbbbbwwwlo","olwwwbbbbbbwwwlo","olwwwbbbbbbwwwlo","olwwwbbbbbbwwwlo",
            "olwwwbbbbbbwwwlo","olwwwbbbbbbwwwlo","olwwwbbbbdbwwwlo","olwwwbbbbbbwwwlo",
            "olbbbbbbbbbbbblo","olcccccccccccclo","ollllllllllllllo","oooooooooooooooo"
        ], new Dictionary<char, Color>
        {
            ['o'] = new Color(96, 74, 58), ['l'] = new Color(160, 110, 86), ['w'] = new Color(220, 202, 174),
            ['b'] = new Color(116, 74, 42), ['d'] = new Color(230, 210, 136), ['c'] = new Color(110, 86, 64)
        });

        CreateTexture("tile_counter", [
            "................","................","bbbbbbbbbbbbbbbb","bllllllllllllllb",
            "bllllllllllllllb","brrrrrrrrrrrrrrb","brrrrrrrrrrrrrrb","bllllllllllllllb",
            "bllllllllllllllb","brrrrrrrrrrrrrrb","brrrrrrrrrrrrrrb","bllllllllllllllb",
            "bllllllllllllllb","bbbbbbbbbbbbbbbb","................","................"
        ], new Dictionary<char, Color>
        {
            ['.'] = Color.Transparent, ['b'] = new Color(90, 66, 44), ['l'] = new Color(176, 136, 92), ['r'] = new Color(132, 98, 68)
        });

        CreateTexture("tile_service_clinic", [
            "................","...oooooooooo...","..offfffffffo...","..ofwwwwwwwfo...",
            "..ofwwppwwfo....","..ofwwppwwfo....","..ofwwwwwwwfo...","..ofwwwwwwwfo...",
            "..offfffffffo...","...oooooooooo...","....qqqqqqq.....","....qqqqqqq.....",
            "................","................","................","................"
        ], new Dictionary<char, Color>
        {
            ['.'] = Color.Transparent, ['o'] = new Color(154, 112, 120), ['f'] = new Color(244, 242, 244),
            ['w'] = new Color(228, 236, 238), ['p'] = new Color(220, 92, 120), ['q'] = new Color(168, 150, 114)
        });

        CreateTexture("tile_service_shop", [
            "................","...oooooooooo...","..offfffffffo...","..ofwwwwwwwfo...",
            "..ofwyyyyywfo...","..ofwyyyyywfo...","..ofwwwwwwwfo...","..ofwyywyywfo...",
            "..offfffffffo...","...oooooooooo...","....qqqqqqq.....","....qqqqqqq.....",
            "................","................","................","................"
        ], new Dictionary<char, Color>
        {
            ['.'] = Color.Transparent, ['o'] = new Color(126, 96, 62), ['f'] = new Color(244, 238, 220),
            ['w'] = new Color(232, 214, 172), ['y'] = new Color(206, 148, 76), ['q'] = new Color(168, 150, 114)
        });
    }

    private void CreateObjectTextures()
    {
        CreateTexture("world_pickup", [
            "................","................","......rrrr......",".....rrrrrr.....",
            "....rrrrrrrr....","....rrrrrrrr....","....wwwwwwww....","....wwwwwwww....",
            "....rrrrrrrr....","....rrrrrrrr....",".....rrrrrr.....","......rrrr......",
            "......bbbb......",".....bbbbbb.....","................","................"
        ], new Dictionary<char, Color>
        {
            ['.'] = Color.Transparent, ['r'] = new Color(206, 70, 74), ['w'] = new Color(244, 238, 220), ['b'] = new Color(96, 70, 46)
        });

        CreateTexture("world_terminal", [
            "................","....bbbbbbbb....","...bccccccccb...","...bcwwwwwwcb...",
            "...bcwwggwwcb...","...bcwwggwwcb...","...bcwwwwwwcb...","...bccccccccb...",
            "....bbbbbbbb....","......bbbb......","......bbbb......",".....bccccb.....",
            ".....bccccb.....","................","................","................"
        ], new Dictionary<char, Color>
        {
            ['.'] = Color.Transparent, ['b'] = new Color(82, 66, 52), ['c'] = new Color(152, 174, 192),
            ['w'] = new Color(210, 232, 236), ['g'] = new Color(96, 156, 188)
        });

        CreateTexture("world_sight", [
            "................","......yyyy......",".....yyyyyy.....","....yyyyyyyy....",
            "...yyyyyyyyyy...","..yyyyyyyyyyyy..",".yyyyyyyyyyyyyy.",".yyyyyyyyyyyyyy.",
            ".yyyyyyyyyyyyyy.",".yyyyyyyyyyyyyy.","..yyyyyyyyyyyy..","...yyyyyyyyyy...",
            "....yyyyyyyy....",".....yyyyyy.....","......yyyy......","................"
        ], new Dictionary<char, Color> { ['.'] = Color.Transparent, ['y'] = new Color(246, 228, 138) });
    }

    private void CreateCharacterTextures()
    {
        var directions = new Dictionary<string, string[]>
        {
            ["down"] =
            [
                "................",".....hhhhhh.....","....hssssssh....","....hssssssh....",
                "....haaaaash....",".....hsssshh....","....bbbbbbbb....","...bbbbbbbbbb...",
                "...bbbllllbbb...","...bbbllllbbb...","...bbbllllbbb...","....bbllllbb....",
                "....dd....dd....","...ddd....ddd...","...ddd....ddd...","................"
            ],
            ["up"] =
            [
                "................",".....hhhhhh.....","....haaaaash....","....haaaaash....",
                "....hssssssh....",".....hsssshh....","....bbbbbbbb....","...bbbbbbbbbb...",
                "...bbbllllbbb...","...bbbllllbbb...","...bbbllllbbb...","....bbllllbb....",
                "....dd....dd....","...ddd....ddd...","...ddd....ddd...","................"
            ],
            ["left"] =
            [
                "................",".....hhhhhh.....","....hsssssh.....","....hssssah.....",
                "....haaaaah.....",".....hsssshh....","....bbbbbbbb....","...bbbbbbbbbb...",
                "...bbbllllbbb...","...bbbllllbbb...","...bbbllllbbb...","....bbllllbb....",
                "....dd....dd....","...ddd....ddd...","...ddd....ddd...","................"
            ],
            ["right"] =
            [
                "................",".....hhhhhh.....",".....hsssssh....",".....hasssssh...",
                ".....haaaaah....","....hhsssshh....","....bbbbbbbb....","...bbbbbbbbbb...",
                "...bbbllllbbb...","...bbbllllbbb...","...bbbllllbbb...","....bbllllbb....",
                "....dd....dd....","...ddd....ddd...","...ddd....ddd...","................"
            ]
        };

        CreateCharacterStyle("player", new Dictionary<char, Color> { ['.'] = Color.Transparent, ['h'] = new Color(58, 86, 154), ['s'] = new Color(232, 204, 164), ['a'] = new Color(244, 230, 154), ['b'] = new Color(198, 82, 72), ['l'] = new Color(88, 122, 70), ['d'] = new Color(74, 68, 108) }, directions);
        CreateCharacterStyle("guide", new Dictionary<char, Color> { ['.'] = Color.Transparent, ['h'] = new Color(64, 98, 128), ['s'] = new Color(228, 204, 170), ['a'] = new Color(244, 232, 176), ['b'] = new Color(78, 150, 176), ['l'] = new Color(96, 132, 186), ['d'] = new Color(82, 78, 112) }, directions);
        CreateCharacterStyle("elder", new Dictionary<char, Color> { ['.'] = Color.Transparent, ['h'] = new Color(116, 98, 154), ['s'] = new Color(226, 206, 172), ['a'] = new Color(236, 234, 214), ['b'] = new Color(138, 114, 178), ['l'] = new Color(104, 88, 136), ['d'] = new Color(86, 78, 106) }, directions);
        CreateCharacterStyle("healer", new Dictionary<char, Color> { ['.'] = Color.Transparent, ['h'] = new Color(188, 78, 112), ['s'] = new Color(238, 214, 186), ['a'] = new Color(248, 242, 246), ['b'] = new Color(242, 244, 248), ['l'] = new Color(206, 114, 138), ['d'] = new Color(142, 104, 118) }, directions);
        CreateCharacterStyle("shopkeeper", new Dictionary<char, Color> { ['.'] = Color.Transparent, ['h'] = new Color(122, 78, 42), ['s'] = new Color(236, 208, 172), ['a'] = new Color(248, 226, 156), ['b'] = new Color(210, 154, 74), ['l'] = new Color(142, 96, 48), ['d'] = new Color(86, 70, 92) }, directions);
        CreateCharacterStyle("scout", new Dictionary<char, Color> { ['.'] = Color.Transparent, ['h'] = new Color(82, 116, 58), ['s'] = new Color(230, 208, 162), ['a'] = new Color(226, 232, 174), ['b'] = new Color(104, 154, 98), ['l'] = new Color(84, 108, 68), ['d'] = new Color(82, 78, 106) }, directions);
        CreateCharacterStyle("villager", new Dictionary<char, Color> { ['.'] = Color.Transparent, ['h'] = new Color(90, 94, 136), ['s'] = new Color(228, 204, 168), ['a'] = new Color(242, 228, 162), ['b'] = new Color(120, 148, 194), ['l'] = new Color(82, 98, 148), ['d'] = new Color(82, 78, 106) }, directions);
    }

    private void CreateCreatureTextures()
    {
        CreateTexture("creature_front_sproutle", [
            "....................","........gg..........",".......gggg.........","......gggggg........",
            ".....gllllllg.......","....gllllllllg......","...ggllllllllgg.....","...ggllwwwwllgg.....",
            "..gggllwbbbwllgg....","..gggllwwwwllggg....","..ggggllllllllgg....","...gggllggggllg.....",
            "....ggllggggllg.....",".....gllggggllg.....",".....gllggggllg.....","....ggllg..gllgg....",
            "...gggllg..gllggg...","...gg.l....l.gg.....","....g......g........","...................."
        ], new Dictionary<char, Color>
        {
            ['.'] = Color.Transparent, ['g'] = new Color(76, 138, 76), ['l'] = new Color(134, 196, 98), ['w'] = new Color(236, 242, 220), ['b'] = new Color(82, 74, 64)
        });

        CreateTexture("creature_back_sproutle", [
            "....................","........gg..........",".......gggg.........","......gggggg........",
            ".....gggggggg.......","....gggllllggg......","...gggllllllggg.....","...ggllllllllgg.....",
            "..gggllllllllggg....","..gggllllllllggg....","..ggggllllllllgg....","...gggllllllllgg....",
            "....gggllllllgg.....",".....ggllllllgg.....",".....ggllllllgg.....","....gggll..llggg....",
            "...ggggl....lgggg...","...gg........gg.....","....g........g......","...................."
        ], new Dictionary<char, Color> { ['.'] = Color.Transparent, ['g'] = new Color(68, 122, 68), ['l'] = new Color(120, 178, 88) });

        CreateTexture("creature_front_embercub", [
            "....................","........ff..........",".......ffff.........","......foooof........",
            ".....foooooff.......","....foooooooof......","...ffooooooooff.....","...fooswwssoof......",
            "..ffooswbbwsooff....","..fooooswwsoooof....","..fooooooooooooof...","...fooooyyyyooof....",
            "....foooyyyyooof....",".....fooyyyyooof....",".....fooyyyyooof....","....ffooy..yooff....",
            "...fffooo....oooff..","...ff..........ff...","....f..........f....","...................."
        ], new Dictionary<char, Color>
        {
            ['.'] = Color.Transparent, ['f'] = new Color(210, 98, 64), ['o'] = new Color(234, 148, 92), ['s'] = new Color(242, 218, 176),
            ['w'] = new Color(246, 242, 226), ['b'] = new Color(78, 64, 58), ['y'] = new Color(244, 206, 94)
        });

        CreateTexture("creature_back_embercub", [
            "....................","........ff..........",".......ffff.........","......foooof........",
            ".....foooooff.......","....foooooooof......","...ffooooooooff.....","...foooooooooof.....",
            "..ffoooooooooooff...","..fooooooooooooof...","..fooooyyyyoooof....","...foooyyyyyooof....",
            "....fooyyyyyyoo.....",".....foyyyyyyof.....",".....foyyyyyyof.....","....ffoyy..yyoff....",
            "...fffoy....yofff...","...ff..........ff...","....f..........f....","...................."
        ], new Dictionary<char, Color> { ['.'] = Color.Transparent, ['f'] = new Color(194, 90, 58), ['o'] = new Color(222, 136, 84), ['y'] = new Color(240, 198, 88) });

        CreateTexture("creature_front_brookit", [
            "....................",".........bb.........",".......bbbbbb.......","......bwwwwwwb......",
            ".....bwwwwwwwwb.....","....bbwwwwwwwwbb....","...bbwwwwwwwwwwbb...","...bwwwswwwwswwwb...",
            "..bbwwwsbbbbswwwbb..","..bbwwwwswwswwwwbb..","..bbwwwwwwwwwwwwbb..","...bbwwwwwwwwwwbb...",
            "....bbwwwwwwwwbb....",".....bbwwwwwwbb.....","....bbbwwwwwwbbb....","...bbbwwb..bwwbbb...",
            "...bbbbw....wbbbb...","....bb......bb......","....................","...................."
        ], new Dictionary<char, Color> { ['.'] = Color.Transparent, ['b'] = new Color(86, 148, 206), ['w'] = new Color(170, 218, 242), ['s'] = new Color(242, 246, 248) });

        CreateTexture("creature_back_brookit", [
            "....................",".........bb.........",".......bbbbbb.......","......bwwwwwwb......",
            ".....bbwwwwwwbb.....","....bbwwwwwwwwbb....","...bbwwwwwwwwwwbb...","...bbwwwwwwwwwwbb...",
            "..bbbwwwwwwwwwwbbb..","..bbbwwwwwwwwwwbbb..","...bbwwwwwwwwwwbb...","....bbwwwwwwwwbb....",
            ".....bbwwwwwwbb.....","....bbbwwwwwwbbb....","...bbbbwwwwwwbbbb...","...bbbwwb..bwwbbb...",
            "...bbbwb....bwbbb...","....bb........bb....","....................","...................."
        ], new Dictionary<char, Color> { ['.'] = Color.Transparent, ['b'] = new Color(78, 136, 194), ['w'] = new Color(152, 202, 230) });
    }

    private void CreateCharacterStyle(string style, IReadOnlyDictionary<char, Color> palette, IReadOnlyDictionary<string, string[]> directions)
    {
        foreach (var (direction, rows) in directions)
        {
            CreateTexture($"char_{style}_{direction}", rows, palette);
        }
    }
}
