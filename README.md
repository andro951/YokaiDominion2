# Yokai Dominion

A C# Japanese-fantasy MOBA in active development for Isaac Richards.

**Milestone status: foundational simulation; not a finished game.** The 3D client is being added in the current work session. The chosen engine is **Godot 4.7.2 .NET**, with a separate **.NET 10** simulation library. No paid assets or proprietary game assets are required.

## Project structure
- `src/Yokai.Core`: engine-independent geometry, equipment, and combat rules.
- `tests/Yokai.Core.Tests`: executable regression suite, with no external test-framework dependencies.
- `.github/workflows/ci.yml`: builds and tests on Windows and Linux.
- `AGENTS.md`: project contract including verified GitHub saves every 5–10 minutes of active work.

## Validate
Install the .NET 10 SDK and run `dotnet run --project tests/Yokai.Core.Tests -c Release` from the repository root.

## Design fidelity
Preserve the user's original design: 11 equipment slots, weapon-defined scaling, 2/3/5 armor thresholds, capped conventional CDR, a separate attack-speed interaction for Q/W, fractional bleed carry, and Vaelira's directional Blood Scent. Provisional values must be labeled, not silently treated as approved design.

## Rights
No open-source license is assigned to Isaac's original game code by this repository. Third-party dependencies and assets retain their own licenses. The engine is Godot (MIT). See the asset ledger as assets are introduced.
