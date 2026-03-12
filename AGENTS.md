# AGENTS.md

## Project goal
Build an original 2D monster-collection RPG in C# with MonoGame.
The game may be inspired by classic handheld creature-collection RPG structure,
but it must avoid protected franchise-specific expression.

## Hard constraints
- Do NOT use Pokemon, Pikachu, Pokedex, Pokeball, Gym, Professor Oak, Team Rocket, or any franchise-specific names.
- Do NOT imitate copyrighted creature silhouettes, UI layouts, logos, item names, map names, or story beats.
- Keep all monsters, regions, factions, and terminology original.
- Use C# and MonoGame.
- Prefer clean, modular, object-oriented architecture.
- Prefer JSON data files for content.
- Prefer simple placeholder art and programmer art.
- Avoid adding unnecessary dependencies unless clearly justified.

## Code structure preferences
- Keep gameplay logic separated from rendering logic.
- Use folders such as:
  - Core
  - Data
  - World
  - Battle
  - Creatures
  - UI
  - Content
- Keep classes small and focused.
- Add comments only where they improve maintainability.

## Delivery expectations
- Before coding, summarize the plan.
- After coding, explain changed files and why.
- Validate buildability where possible.
- If a task is too large, implement a solid vertical slice first instead of a fake full system.

## Build and verification rules
- Do not create a PR unless the project builds successfully.
- For C# changes, run the project build after every code modification.
- If the build cannot be run in the environment, explicitly say so and do not claim success.
- Fix one problem at a time with minimal, high-confidence changes.
- Do not widen access modifiers unless necessary and justified.
- Report the exact root cause before coding.