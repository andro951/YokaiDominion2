# Yokai Dominion 2 — development contract

Owner: Isaac Richards. This is the active game repository. Do not modify the earlier YokaiDominion repositories without a specific request.

## Non-negotiable requirements
- Implement gameplay in C#.
- Save real, verified GitHub checkpoints every 5–10 minutes during active work; target five minutes and never intentionally exceed ten. A local file or unreferenced Git object is not a checkpoint. Verify the remote branch after updating it.
- Read docs/design/IMPLEMENTATION.md and docs/design/DESIGN_SOURCE.md completely before changing mechanics. They distinguish preserved decisions from provisional balance numbers. The user's full design notes remain authoritative; retrieve the identified source when a summarized rule is insufficient.
- Never describe a prototype, compile-only check, placeholder character, or untested export as a finished game.
- Never claim asynchronous progress: work occurs only while an active tool session is executing.
- Keep secrets, caches, generated builds, downloaded third-party packages, and large binary assets out of normal Git history.
- Record third-party asset source, exact license, and allowed redistribution before committing it. Do not import art from League, Dota, or RuneScape.

## Engineering direction
- Godot 4.7.2 .NET desktop 3D client; engine-independent .NET 10 C# combat simulation and automated regression tests.
- Separate visual frame rate, combat timing, and the provisional 60 ms logical grid cadence.
- Continuous world-space movement; derived hex coordinates must not determine directional speed.
- Preserve Vaelira's 25/50/25 Q windows, queued pre-hit damage, free kick that does not reset cooldown, weapon-dependent Q, and champion-only directional Blood Scent.
- No fake networking: multiplayer remains pending until an authoritative server and real multi-client tests exist.
- Verify a checkpoint's files and CI results. Use non-forced ref updates with an expected head; reconcile concurrent changes instead of overwriting them.

## Handoff
Update README.md and docs/STATUS.md with implemented features, test evidence, known limitations, provisional design choices, and the next concrete task before ending a work session. Do not substitute a README promise for implemented code.
