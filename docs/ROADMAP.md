# Quality roadmap and acceptance gates

The goal is a high-quality C# game, not an indefinitely expanding prototype. Each stage needs visible/gameplay evidence as well as code. All unapproved numeric balance remains provisional.

## 1. Combat feel and first champion
Keep the existing rules suite green. Add repeatable arena scenarios for every Q window and exact recast boundary. Improve telegraphs, hit reactions, two-strike presentation, attack cancellation and attack-move continuation. Test base purchases, every hand configuration, death, progression and shrine victory in an actual input-driven playthrough. Do not count a five-second launch smoke as a full match test.

## 2. Character production adaptation
Keep the original Art008 master immutable. Current game copy is a first-pass 29-joint skin with 28 deform bones, automatic-weight fallback and procedural poses. The inspected idle/walk/kick render confirms actual skinning, but it still shows stiff garment motion and areas requiring shoulder/hip/knee cleanup. Fix anatomical weight classification, add appropriate hand/foot IK and polished animation, and validate all extreme poses. The current character is a development visual, not a newly approved Vaelira design.

Advanced tissue must be a separately tested C# volumetric/XPBD implementation with correct skin binding, stable collision, damping, energy/error metrics and visual approval. Garment collision must be evaluated on the actual outfit. Do not substitute isolated solver tests or old-project acceptance results for integration with this mesh.

## 3. Battlefield and item production
Replace remaining rough environment/minion art through a consistent, licensed pipeline. Add proper hero/minion silhouettes, selection readability, camera occlusion handling, art LODs and material budgets. The current 20-item test catalog is not the complete proposed/approved final catalog. Preserve weapon scaling, mixed sets, eleven slots and asset attribution. No Force Staff-style ally/self launch item: the user's preferred pull concept remains subject to design approval.

## 4. Simulation depth and opponents
Add robust moving-body collision, obstacle-aware crowd movement, projectile travel/interception, attack-move resumption, stronger tactical bots, lane objectives and match-end simulations. Establish deterministic input/replay tests where useful without falsely promising cross-platform floating-point lockstep. Expand soak tests to many seeds/scenarios and full matches.

## 5. Online multiplayer
Build an actual authoritative server, command validation, snapshots/interpolation, latency/loss handling, reconnect and fog/vision rules. Validate with multiple independently running clients plus injected latency and packet loss. A local bot or a network interface stub is not multiplayer completion.

## 6. Release quality
Profile the user's GTX 1080-class target on real hardware, not Mesa software CI. Add input rebinding, audio mixing, accessibility, crash logs, safe config migration, reproducible build tooling and signed releases when signing credentials are supplied through proper secret management. Keep bundled notices and package checksums. Never put tokens, signing keys or caches in source control.

## Milestone evidence
Current automated checks cover 18 rules regressions and 12 integration/soak tests, cross-platform compilation, GLB weight/provenance auditing, actual graphical captures, and exported executable launch checks. None of those by itself establishes final visual quality, fun, balance, network readiness or retail polish.
