# AGENTS.md

## Build and verification rules
- Always read the current main branch state before making changes.
- For any C# code change, run `dotnet build` before claiming success.
- Do not create a PR unless the build succeeds.
- If build cannot run in the environment, explicitly say so and do not claim the fix is verified.
- Fix one problem at a time with the smallest safe change.
- Do not widen access modifiers unless necessary and justified.

## Commands
- Restore: `dotnet restore PyGame.sln`
- Build: `dotnet build PyGame.sln`
- If needed, use the specific `.csproj` when the solution is unavailable.

## Reporting format
- Root cause
- Files changed
- Build command run
- Build result
- Remaining issues, if any
