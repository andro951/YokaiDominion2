# Playing Yokai Dominion — 0.1.0-dev

## Windows
Extract the ENTIRE ZIP, then run `YokaiDominion.exe`. Keep the `.pck` and `data_*` directory beside it. Do not run the executable from inside the ZIP or move it away from its supporting files. No Godot editor or .NET SDK installation is needed: the package includes its runtime.

This is an unsigned development build, not a signed retail release. Only run builds from this project; do not disable antivirus protections. The default renderer is Forward+. For graphics-driver trouble, use `Launch-Compatibility.bat`, which selects OpenGL without changing your system settings. F10 changes effect quality, not the rendering backend.

## Linux
Extract all files, run `chmod +x YokaiDominion.x86_64`, then launch `./YokaiDominion.x86_64`. The optional argument `--rendering-method gl_compatibility` selects OpenGL. Keep all supporting files.

## Match
Press Enter or choose ENTER THE BATTLEFIELD. Control Vaelira for Dawn and destroy the Dusk shrine. Three lanes produce minion waves, towers attack enemies, a bot controls the opposing champion, and your champion respawns after death. Buy or swap equipment at your shrine. Replaced and unequipped items are stored rather than destroyed.

## Controls
- Right-click ground: move. Right-click enemy: attack/chase. A then click: attack-move. S: stop.
- Q: timed fist combo, or a melee kick with other weapons. Pre-hit Q queues extra damage on the next basic attack; Q immediately after that empowered hit gives a free kick.
- W: start a new attack windup and refresh Q. E: trip nearby enemies. R: leap to the cursor and slam, with bonus damage against knocked-down enemies.
- Hold Q/W/E/R: repeat on readiness. This does not optimize combo timing for you.
- B: shrine armory. Use HAND TARGET for off-hand weapons. **Right-click an equipped item in the armory's bottom equipment list to move it into storage.** Store both gloves before changing to another weapon category. A Strength item cannot be removed while held weapons require its bonus.
- 1/2/3: spend Strength/Dexterity/Intelligence points. Ctrl+Q/W/E/R: upgrade an ability. Each rank reduces that ability's base cooldown; exact W/E/R curves are provisional.
- Arrows: pan. Space: follow. Wheel: zoom. Minimap left-click: pan; right-click: move.
- Escape: pause/close menu. F1: field guide. F10: quality. F11: fullscreen. M: mute.

## Verification and limits
Build provenance is in BUILD-MANIFEST.json and BUILD-COMMIT.txt; SHA256SUMS.txt covers packaged files. Component notices and character provenance are under licenses/.

This is a first local playable slice, NOT a finished or online MOBA. The skinned Character01 development visual is not final approved Vaelira art; its movement, cloth and deformation need production work. The map, bots, balance, sound and catalog are also provisional. No online multiplayer, fog of war, volumetric tissue, production cloth physics, complete item roster or true projectile interception is included. Automated launches and HUD checks are not a full playthrough on your GPU.
