# Asset provenance ledger

## Original development assets
The arena geometry, terrain/water shaders, shrine architecture, lightweight minion proxies, UI geometry, impact effects and synthesized sounds are created by this project's source. They are development assets, not a claim of final production art.

## Character01_Game001
- Source: Isaac's Character01 Art008 project asset; immutable source and exact checksums in `CHARACTER01_PROVENANCE.md`.
- Upstream body/eye topology: Blender Studio and community contributors, Human Base Meshes v1.4.1 realistic female, **CC0**. The separate Rain Rig is not used.
- Project additions: fantasy details, clothing, hair, markings, textures and shaping from the user's earlier project.
- Game changes: reduced topology/textures, first-pass biped/tail/garment rig, skin weights, consolidated mesh, procedural C# poses.
- Actual binary and manifest are committed under `src/Yokai.Client/Assets/Characters/`.
- Not final approved Vaelira likeness, final animation, cloth simulation or tissue physics. This is a reusable development visual.

## Dependencies
- Godot Engine 4.7.2, MIT: https://godotengine.org/license/
- .NET runtime/SDK: distributed components retain their own licenses.

Before any new external asset is committed, record exact name, author, source URL, acquisition date, license text/reference, required attribution, source-redistribution permission and modifications. Free download is not evidence of use rights. No copied artwork from League, Dota or RuneScape is included.
