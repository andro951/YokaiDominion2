# Yokai Dominion

A Japanese-fantasy MOBA being built in **C#** for Isaac Richards. Active repository: `andro951/YokaiDominion2`.

**Milestone 0.1.0-dev: a local three-lane playable slice, not a finished game.** The client uses **Godot 4.7.2 .NET**. Combat, movement, equipment and match state live in a separate **.NET 10** library with no engine dependency.

## Play a packaged build
The **Standalone desktop builds** Actions workflow produces Windows x64 and Linux x64 packages after tests and exports succeed. Download the corresponding artifact from a successful run, extract the entire folder, and launch the executable. Keep the `.pck` and `data_*` directory beside it. **The packages include their .NET runtime; no editor or SDK is required to play.**

On Windows, use `YokaiDominion.exe`. `Launch-Compatibility.bat` selects the OpenGL compatibility renderer for graphics-driver troubleshooting. The default is Forward+. These are unsigned development builds, not signed retail releases; do not disable antivirus protections. Read `docs/PLAYING.md`.

## Run from source
1. Install the .NET 10 SDK and **Godot 4.7.2 .NET**, not the standard/GDScript-only editor.
2. Clone this repository. Run `dotnet build src/Yokai.Client/Yokai.Client.csproj -c Debug` from the root.
3. Import `src/Yokai.Client/project.godot` in Godot .NET and press F5.
4. Press Enter at the title screen. Right-click to move or attack; Q/W/E/R use abilities. F1 opens the controls.

## Validate
- `dotnet run --project tests/Yokai.Core.Tests -c Release` — 18 rules regressions.
- `dotnet run --project tests/Yokai.Match.Tests -c Release` — 12 match integration/soak tests.
- `dotnet build src/Yokai.Client/Yokai.Client.csproj -c Debug`
- `python scripts/audit-game-asset.py src/Yokai.Client/Assets/Characters/Character01_Game001.glb`

CI tests simulation and compiles the client on Windows and Linux. Graphical Linux tests render the actual match and close-up character poses through Mesa/Xvfb. Packaging launches the actual exported Windows and Linux binaries. Build manifests record the source commit, runtime, SDK and notices; `SHA256SUMS.txt` covers every packaged file. Test results are recorded in `docs/STATUS.md`, with precise revision boundaries rather than assuming every new commit already passed.

## Implemented
Three lanes, waves, towers, shrine victory, bot champion, death/respawn; continuous movement, hex A*, swept terrain collision; Vaelira's timed weapon-dependent Q, W reset, E trip, R leap and directional Blood Scent; eleven equipment slots, weapon scaling, strength requirements, 2/3/5 mixed armor sets, shop/storage, levels and ability ranks; HUD, minimap, camera, effects, synthesized audio and pause/settings.

## Art and rights
No paid assets are required. The map, minion proxies, effects and sound are generated from project source. Champions use a separately optimized/rigged copy of the user's earlier **Character01 Art008** model, derived from a **CC0 Blender human base mesh**. The visual master remains unchanged in the earlier repository. Exact provenance is in `assets/CHARACTER01_PROVENANCE.md`.

The character's procedural motion and garment bones are development animation, **not final approved Vaelira likeness, production cloth or tissue physics**. The current map is also development art. No League, Dota or RuneScape artwork is imported.

No open-source license is assigned to Isaac's original code or original project additions. Third-party components retain their licenses. Exported builds include Godot and .NET license/notices plus character provenance.

## Engineering and next production work
`src/Yokai.Core` simulation • `src/Yokai.Client` rendering/input/UI • `tests` regressions • `docs/design` approved mechanics versus provisional choices • `assets` provenance.

The next quality work is combat feel and animation/rig cleanup, stronger bots and body/path collision, consistent environment art, then a real authoritative multiplayer/vision layer. Final character animation, volumetric tissue, cloth collision, full item roster, projectile interception, balance, accessibility and target-GPU profiling remain unfinished. See `docs/ROADMAP.md` for acceptance criteria.

During active work, save and verify remote GitHub checkpoints every **5–10 minutes**, targeting five. This applies to code and actual generated art assets, not just promises or scripts. See `AGENTS.md`. No background development is implied.

## New design discussion: November 13
Isaac's latest [design thoughts](docs/design/DESIGN_THOUGHTS_2026-11-13.md) explore a casual, short-match, objective-focused direction: shared jungle, team XP and recapturable resource sites with lane offense/defense choices. They distinguish preferences from unresolved alternatives. This is documentation only; none of these new concepts is claimed implemented. Resolve the open map/economy questions before implementing a redesign.
