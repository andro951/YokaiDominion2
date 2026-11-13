# Design authority and source provenance

The authoritative source read in full for this implementation is Isaac Richards's Library file **Yokai_Dominion_Design_Notes(2).md**, version 1, 370 text lines, last modified 2026-11-11. It was retrieved and read in full on 2026-11-13 UTC. The file is not attached to the repository as an exact archive; this document is a provenance record, not a claim that a complete copy exists here.

Future ChatGPT work can retrieve it through Files using the exact title above and Library scope. Its accessible source identifier in the creating session was `file_00000000f09081f5a90ee7cc6cdc3aeb`. If the user has a newer design version, that newer version takes precedence. Do not reconstruct missing design decisions from the old game's mechanics or assume provisional values were approved.

## Source sections and implementation status
1. Strength/Dexterity/Intelligence and independent automatic/manual/ability level progression: implemented with provisional coefficients.
2. Conventional CDR capped at 50%, Q/W additionally synchronized with attack speed: implemented.
3. Natural mana regeneration equal to 25% of baseline continuous ability demand: implemented. Full-sustain item build not yet fully supported by the small catalog.
4. Toggleable doubled mana cost / 1.5x ability damage item: proposed, not implemented.
5. Eleven OSRS-layout equipment slots and five-slot 2/3/5 armor bonuses: implemented. Exact set numbers provisional.
6. Main/off-hand strength requirements and handling scores applied to scaling only: implemented. Mixed ranged/melee pairs deliberately rejected pending design.
7. Base storage: implemented without a capacity limit as an explicit prototype rule.
8. One-second spell-parry shield active with five-second cooldown: proposed, not implemented. The current shield grants defenses only.
9. Fist weapon proc rules, physical/true bleed distinctions, fractional carry, and Q kick exclusion from weapon on-hit effects: implemented and regression-tested.
10. Combat staff melee-plus-spell behavior: proposed, not implemented. Crossbow/casting staff do not receive an automatic melee double-hit.
11. Continuous positions with separately derived hex occupancy; precise fractional combo timing: implemented. Swept terrain collision exists; swept cell-trigger enumeration and robust moving-body collision do not yet exist.
12. Vaelira Blood Scent, timed Q, W reset, E trip and R leap: implemented with explicit provisional balance and range rules in IMPLEMENTATION.md.
13. External item reference workbook: referenced by the user's full notes; it is research material, not a license to copy game assets or an already-balanced Yokai Dominion item list.

The original visual reference for Vaelira is not present in this repository. The current articulated proxy must not be represented as a faithful recreation of the user's approved appearance or proportions.
