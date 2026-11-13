# Playing Yokai Dominion — development vertical slice

## Windows build
Extract the ENTIRE ZIP into a folder, then run `YokaiDominion.exe`. Keep the `.pck` and `data_*` directory beside the executable. Do not run the executable directly from inside the ZIP or move it away from its supporting files. This is an unsigned development build, not a signed retail release. Only run a build from this repository or the package supplied by the developer; do not disable antivirus protections.

The Windows target is x86-64. The default renderer is Forward+. For graphics-driver problems, start `YokaiDominion.exe --rendering-method gl_compatibility` from a terminal. F10 switches effects quality, not the rendering backend.

## Linux build
Extract all files, run `chmod +x YokaiDominion.x86_64`, then launch `./YokaiDominion.x86_64`. Keep all supporting files. The compatibility-renderer argument above also works here.

## Match
Select ENTER THE BATTLEFIELD or press Enter. You control Vaelira for Dawn. Destroy the Dusk shrine while protecting your shrine. Three lanes produce minion waves; towers attack enemies, a bot controls the opposing champion, and your champion respawns after death. Equipment is bought at your shrine. Replaced items go into shrine storage.

## Controls
- Right-click ground: move. Right-click an enemy: attack/chase.
- A, then click: attack-move. S: stop.
- Q: timed fist combo, or melee kick with a non-fist weapon. Pre-hit Q queues a second strike on the next basic attack; Q immediately after that empowered hit is a free kick.
- W: start a fresh attack windup and refresh Q. E: trip nearby enemies. R: leap to the cursor and slam, with bonus damage against knocked-down enemies.
- Hold Q/W/E/R: repeat on readiness, with the free Q kick taking priority. Holding Q does NOT optimize its timing for you.
- B: open/close shrine armory. You must be alive and at base to purchase or swap stored gear. Use the hand-target button for off-hand weapons.
- 1/2/3: allocate Strength/Dexterity/Intelligence points. Ctrl+Q/W/E/R: spend an ability-upgrade point.
- Arrow keys: pan. Space: follow Vaelira. Mouse wheel: zoom. Minimap left-click: pan; right-click: move.
- Escape: pause/close menu. F1: field guide. F10: visual quality. F11: fullscreen. M: mute.

## Development limitations
This is a FIRST LOCAL VERTICAL SLICE, not a finished or online MOBA. Hero meshes are articulated proxies, not final Vaelira art. Balance values, bot tactics, ability rank progression, map layout, sound, and equipment visuals are provisional. No online multiplayer, fog of war, advanced tissue physics, final animations, full item catalog, or true projectile interception is included yet.

Build provenance is recorded in BUILD-COMMIT.txt. Automated launch tests are not a substitute for a full playthrough on your GPU.
