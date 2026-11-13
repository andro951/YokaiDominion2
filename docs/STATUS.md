# Yokai Dominion — verified milestone 0.1.0-dev

Updated 2026-11-13 UTC. This is a first local playable C# match, not a finished production MOBA.

## Exact verified source
Game/build source commit: `d60cb42aebcd04c840943e8e680b3ef63df8f975`.

The final status/evidence checkpoint only changes documentation. A newer documentation HEAD does not imply the packaged executable was rebuilt from that documentation commit. The package's own BUILD-COMMIT.txt and BUILD-MANIFEST.json are authoritative for its source revision.

Client: Godot 4.7.2 .NET. Simulation target: .NET 10, independent of Godot. The verified export bundles .NET runtime 10.0.15. The build runner selected SDK 11.0.100; the manifest records that actual SDK rather than falsely claiming the installed 10.x SDK was selected.

## Completed verification
Regression/render workflow: `43370024341`, all jobs successful.
- 18 core rules regressions and 12 match integration/soak tests passed on both Windows and Linux.
- The Godot C# client compiled on both platforms, with warnings treated as errors.
- The committed GLB passed independent embedded-resource, finite-value, joint-index, normalized-weight, triangle-count and checksum checks.
- The actual Godot runtime rendered idle/walk/kick character poses, the title screen, battlefield, shop, help and pause screens. Captured images were inspected.
- The UI review passed 18 assertions: actual HUD hit regions bought main/off-hand gloves, stored both, switched to a crossbow, and rejected invalid Strength-item removal and out-of-base unequipping. These are scripted runtime interactions, not a claim of a human playthrough.
- The rendered match completed its 5.6-second scripted smoke scenario with 28 units and a living player, without script/runtime errors.

Desktop export workflow: `43370024369`, all jobs successful.
- Both actual exported Windows and Linux executables launched without the editor and completed the headless match smoke test.
- Both loaded the 29-joint character skeleton successfully.
- Packages include the runtime, matching Godot/.NET notices, character provenance and per-file checksums.

Graphics were tested using Linux Mesa software rendering with Godot's OpenGL compatibility backend. Windows executable launch was headless. The default Forward+ renderer and performance on Isaac's GTX 1080-class hardware have NOT been visually/profile-tested here. A two-minute simulation soak and a short graphical smoke test are NOT a complete match acceptance test.

## Windows package delivered
Artifact `14011682448`, name `YokaiDominion-Windows-x64`, desktop run above.
- Conversation file: `YokaiDominion-Windows-0.1.0-dev.zip`.
- ZIP size: 87,242,391 bytes; total uncompressed file bytes: 206,061,800.
- ZIP SHA-256: `af45ca5b82c0d7e4fc86aff786cfb05e99ec316183d0233c5e5b072ea9a1a92f`.
- Independently read the downloaded ZIP and verified all 204 entries listed in SHA256SUMS.txt; archive hash matched GitHub's artifact digest.
- Extract the entire archive, keep the .pck and data directory with the executable, and launch YokaiDominion.exe. No Godot editor or .NET SDK is required to play. Launch-Compatibility.bat selects the OpenGL backend.
- This is an unsigned development build. Do not disable antivirus protections.

## Implemented game systems
Three lanes, minion waves, towers, shrine victory, one local bot champion, deaths and respawns; continuous movement with hex A* and swept terrain collision; Vaelira's weapon-dependent Q timing, free kick, W reset, E trip, R leap and Blood Scent; eleven equipment slots, weapon-specific scaling, combined hand Strength requirements, mixed 2/3/5 armor sets, shrine purchasing/storage/unequipping, progression and ability ranks; HUD, minimap, camera, pause/settings, impact effects and synthesized audio.

The map/minions/effects are source-generated development art. Champions use a separate optimized and rigged copy of Isaac's existing Character01 Art008 asset. The original master in the earlier repository remains unchanged. The game copy is 148,471 triangles, 16,778,808 bytes, 14 materials and 29 total joints. Source and output checksums and the CC0 base-topology provenance are recorded alongside it.

## Quality boundary and next acceptance gate
The Character01 visual is not established as final approved Vaelira likeness. Its procedural motion, stiff garment movement and some joint deformation need production work. There is no integrated volumetric tissue or production cloth collision. The map, bot tactics, balance and equipment catalog are also development-stage.

Online authoritative multiplayer, fog/vision, sophisticated bots, full item catalog, real projectile travel/interception, robust moving-body collision, final animation, accessibility/rebinding and target-GPU profiling remain unfinished. Exact unapproved balance choices are labeled in docs/design/IMPLEMENTATION.md and RANK_TUNING.md.

Next: an input-driven full-match playthrough, combat/camera readability and attack-move refinement, rig/animation/garment cleanup, then stronger battlefield art and a real multiplayer/vision implementation. Keep the regression suites green; do not substitute a successful compile for these acceptance gates. See docs/ROADMAP.md.

## Checkpoints and handoff
Source and actual generated asset binaries were checkpointed to GitHub repeatedly throughout active development, with remote branch/readback verification. Continue verified saves every 5–10 minutes, targeting five. No background development is implied after an active response ends. YokaiDominion2 is the active repository; earlier repositories remain read-only sources unless Isaac authorizes changes.

## Later design discussion recorded: November 13
Added [Isaac's design thoughts](design/DESIGN_THOUGHTS_2026-11-13.md), including the desired casual URF-like experience, evolving reward ideas and recapturable resource-site/bunker concept. No gameplay or build files changed. The next design step is to reconcile the map, objectives, lane orders and economy; earlier verification above applies only to the existing prototype. This documentation was checked against the voice discussion and read back from GitHub; no new runtime testing is implied.
