# Character01 game adaptation

## Source and rights
The source is Isaac's existing Character01 Art008 asset in `andro951/YokaiDominion`, immutable commit `4bec672d1e7836dec2d0db141ff6236e3c722395`, path `art/checkpoints/portable008/e8a6f76486e9/Character01_Visual008.glb`.

SHA-256: `bf422fa4e363f9eb4a04fbc2bb9cb306469fdcbba60123bccdd365203e0856f9`. Original size: 40,972,612 bytes. It has no source rig or animation clips. The original art master is NOT modified by the game adaptation.

The source body/eyes derive from **Blender Studio and community contributors' Human Base Meshes v1.4.1**, specifically `GEO-body_female_realistic`, licensed **CC0**. Official distribution: https://www.blender.org/download/demo-files/ . The page's Human Base Meshes entry was checked on 2026-11-13. The separately licensed Rain Rig in that bundle is not used.

Anatomical reshaping, fantasy details, hair, clothing, markings, textures and the Art008 conversion were existing user-project work. No marketplace textures or models are introduced here. This provenance is not an open-source license for the user's game code, nor a claim that the source was wholly generated from scratch.

## Game-copy changes
`scripts/art/rig_character.py` verifies the source hash, makes a reduced topology copy, caps texture dimensions, adds a first-pass biped/tail/garment rig, assigns normalized skinning weights, consolidates mesh objects, and exports a self-contained GLB. The resulting manifest records size, triangle count, source and output hashes, rig bones, and fallback-weight count.

The asset workflow commits the actual GLB and manifest to this repository using ordinary authenticated Git, without a force push. It downloads the committed binary back at its immutable commit and compares bytes. A source-script commit alone is not treated as a saved finished model.

## Acceptance boundary
Character01 is not established as the user's final approved Vaelira appearance. Reusing it as a development hero visual does not make that relationship canonical. A first-pass generated rig is not production animation, IK acceptance, cloth simulation, or volumetric tissue integration. Check actual animated renders for deformation and clipping; numerical export success alone cannot prove art quality. Do not attribute the earlier project's old milestone002 rig/physics results to this new adaptation.
