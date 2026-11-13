# Implementation decisions — first playable milestone

Source read in full: Isaac's `Yokai_Dominion_Design_Notes(2).md`, 370 lines, retrieved from Library on 2026-11-13 UTC. This file is an implementation ledger, not a replacement for the authoritative notes.

## Preserved
- Eleven equipment slots; five armor slots contribute to sets. Fist weapons occupy weapon slots, not the Hands armor slot.
- Weapon scores multiply attribute scaling, never flat weapon damage. One fist plus a bare hand retains complete unarmed scaling.
- Q: 25/50/25 post-hit/neutral/pre-hit rhythm; pre-hit queues damage; free kick does not restart cooldown; non-fist Q is melee only. Kick has no weapon on-hit effects.
- One Spiked Knuckles stack per glove per BASIC ATTACK, not per animation strike. All existing bleeds refresh to their own original duration.
- Q bleed is 5 true damage/sec for 5 sec; spike bleed is 1 physical damage/sec for 30 sec. Fractional carry is retained.
- Brass breaks only the armor contributed by heavy plated items. Natural armor, leather, chain, and non-piece set bonuses do not get broken.
- Directional Blood Scent grants 10% toward nearby bleeding enemy champions, not minions.
- Conventional CDR caps at 50%; attack speed additionally synchronizes Q and W only.
- Continuous movement; hex coordinate derived on a separate 60 ms cadence; full swept terrain collision.

## Explicitly provisional
- 120 Hz local combat stepping, 10-unit Blood Scent radius and dot product > .25.
- All starting/derived stats, damage coefficients, prices, experience, growth and minion/tower stats.
- Q kicks reapply by refreshing the same caster's Q bleed, not stacking it. Different casters retain separate Q bleeds.
- Brass applies once per equipped glove per basic event, not again on Q's second visual strike. Duration currently lasts until death.
- Q out of range fails without spending mana/cooldown. Held Q casts immediately when ready, not buffered for optimal rhythm.
- E: 12s, 40 mana, 1.15s knockdown. R: 60s, 75 mana, .55s leap, 1.5x damage to knocked-down enemies. W: 10s and 35 mana.
- 2/3/5 set thresholds are cumulative. Full sets strongly specialize and sacrifice the opposing defense.
- Local three-lane 1v1 against a bot; 22-second waves; no fog of war or online multiplayer yet.
- Enemy basic attacks and crossbow damage resolve at the hit event; visual tracers are not independent collision projectiles yet.
- Shop and unlimited storage work near the shrine; mixed weapon categories are rejected until their attack semantics are designed.

## Not yet implemented
Real skinned hero assets, final animation/mocap, XPBD tissue, full item catalog, spell-parry active, mana-for-damage toggle, combat-staff dual damage, authoritative multiplayer, fog/vision, reconnect/replays, audio/music production, accessibility rebinding, and full visual/performance verification. These must not be represented as completed features.
