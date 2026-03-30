Content/Sprites layout

- tiles: field, interior, building, path, grass, service tiles
- characters: player, NPC, trainer direction sprites
- creatures: battle front/back creature sprites
- world: pickup, PC terminal, sight marker and shared world objects

Guidelines

- Add new PNG files under the matching folder instead of the root.
- Keep file names stable because runtime loading is key-based.
- Naming prefixes:
  - tile_
  - char_
  - creature_
  - world_
- Character walk variants:
  - use `char_<style>_<direction>.png` for the base frame
  - use `char_<style>_<direction>_step.png` for the alternate walk frame
