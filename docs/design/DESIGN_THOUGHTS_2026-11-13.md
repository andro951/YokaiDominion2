# Design thoughts: a casual, objective-focused battlefield
## Isaac's voice discussion, November 13, 2026

**Status: design discussion, not implemented mechanics or a finalized balance specification.** Isaac asked to save these thoughts in the actual game repository. This records the direction and the alternatives he considered, including where his thinking changed during the conversation. Do not implement all alternatives at once or treat illustrative numbers as approved tuning.

The earlier design notes and their provenance remain in [DESIGN_SOURCE.md](DESIGN_SOURCE.md). This discussion adds newer direction at the overall game-experience level. The existing implementation ledger describes what the prototype does, not approval of its provisional map or economy.

## What this game should feel like

The starting idea is a Japanese mythology-inspired mix of League of Legends, Dota 2, Old School RuneScape and fighting games. The joking name was “Old School Japanese Rune Dota Legends Scape.” That describes an experience, not just a checklist of individual systems.

Isaac does not want to recreate the competitive pressure of League or Dota. He is not even certain PvP should be the main focus. If there is PvP, it should be deliberately casual. No ranks. The point is to jump into a match, use abilities, become powerful quickly and have fun.

> “I want it to always feel like Ultra Rapid Fire.”

- Short matches, rather than treating a 25-minute match as unusually fast.
- Frequent ability use. Missing a skill shot should mean another opportunity shortly afterward, not a punishing loss of scarce mana.
- A smaller map with shorter travel times, so players can respond to something happening elsewhere before it is already over.
- Longer back-and-forth fights rather than very fast champion deaths. Whether to achieve that through lower damage, more durability, or both is unresolved.
- More emphasis on managing lanes and achieving objectives, less on hunting champions for kill rewards.

No exact match duration, map size, damage ratio or cooldown target was settled. The two-second spear example explains the desired forgiveness, not a universal cooldown rule.

## The map is more than three lanes

Isaac objected to the prototype's three straight parallel lanes. Referencing League and Dota should have prompted consideration of their actual map layouts and the jungle between lanes, not just literal parallel strips. A departure from that recognizable structure should be a design conversation. This does **not** mean copying either game's map exactly.

The discussion explored:
1. More lanes than players: 3v3 with five lanes or 5v5 with seven lanes.
2. Then a newer possibility: **five lanes, five players per team, and a jungle**.

The second idea is the current exploration, not a locked map specification. The assistant suggested 3v3/five lanes, but Isaac subsequently proposed five players/five lanes; do not treat the earlier suggestion as his final choice.

The jungle should be for everyone, not a territory reserved for a designated jungler. Players should choose which lanes to cover, when to help another lane, and when to enter the jungle. A lane count and team composition should not automatically force one specific role arrangement. Exact routes, lane geometry and jungle layout still need design.

## Helping lanes even when you are elsewhere

Isaac is interested in interacting with a lane without having to stand in it continuously.

- Buffs could affect the character **and nearby minions**, rather than only the character.
- A targeted buff could give its full effect to the selected target and a smaller effect to others nearby.
- Buff behavior could differ for champions and minions.
- Empowered minions could earn their supporting player gold and XP from what they accomplish, allowing a player to help a lane and then do something else.
- Staying nearby might strengthen the benefit, but that was only a possibility.

These ideas are not yet reconciled with the later possibility of removing minion rewards entirely. Attribution, duration, competing buffs and remote reward eligibility remain open.

## Supports should be allowed to participate

A healer or support-oriented character should be able to attack minions, deal damage, help push a lane and be proactive without being punished or blamed for taking the carry's income.

The design problem is not simply whether a character can heal. It is whether the reward system encourages a support to withhold useful actions. Splitting a reward whenever another player contributes can recreate that problem: the support may still be expected not to contribute.

Isaac wants to remove the all-or-nothing fixation on last hits. He explicitly clarified that **dealing damage at any point in a minion's life** is a more forgiving contribution requirement than landing the final blow. It should count even if an allied minion finishes the kill. Do not dismiss this as identical to last-hitting.

## Reward ideas considered, in order

These are alternatives and reasoning, **not one combined economy**.

### 1. Contribution or nearby rewards instead of last hits

Isaac considered earning rewards for being near a minion when it dies, or for having contributed damage earlier. He considered a larger total reward shared among nearby allies, but not a full copy for every player.

Illustrative examples included a 100-gold minion giving 100 to a solo player, around 70 each to two players, and around 40 each to five players. He also considered a five-player total between 150 and 200 rather than 500. These examples changed as he spoke and were **not finalized**.

The concern is a five-player group repeatedly clearing one lane after another and multiplying the team's total income. Another alternative was keeping total gold fixed while sharing XP more generously. None of these divisions was selected.

### 2. Voluntary gold sharing at the fountain

A shared chest at the fountain could let a player deposit gold for teammates, either generally or designated for one specific character to collect there.

Combined with a modest increase in total rewards for shared participation, this could make helping kill minions beneficial while allowing players to move gold to a carry afterward. Isaac also recognized that social pressure could persist: “support, give me your gold.” This remains a concern, not a solved outcome.

### 3. Shared team XP

Isaac liked the shared-XP model discussed in connection with Heroes of the Storm: progress helps the team rather than taking XP away from another teammate. He wants supports to be free to push lanes.

This is a preference for exploring **team-shared XP**, not a decision to copy that game's full economy or remove Yokai Dominion's equipment system. Exact XP sources, collection rules and progression rates are undecided.

### 4. Move rewards away from minions and champion kills

Later in the conversation, Isaac explored a more fundamental change:
- Possibly no direct rewards from minions.
- Gold from the jungle instead.
- Possibly no direct reward for champion kills.
- Shared XP tied to objectives.

The aim is to make fighting serve objectives instead of making kills the objective. This later direction may replace parts of the earlier minion-reward ideas, but has not been finalized.

### 5. Accessible jungle farming and capture areas

Jungle monsters should be usable by ordinary characters, not require a special role or sustain item just to survive a camp. They could have high health but deal little damage. It should feel like farming.

Isaac then proposed areas in the jungle that players capture by standing in them over time. Owning these areas could provide gold, potentially as the main income source. These are capture/control points, despite his initial “capture the flag” wording. The exact payout mechanism and timing were not settled.

## Latest direction: recapturable settlements and defended bunkers

The final idea develops the capture-point concept into a replacement for conventional towers:

- A “tower” could instead be a settlement, town, farm or other valuable resource site.
- Holding it should have continuing value, rather than only a one-time gold reward when it is destroyed.
- Sites should be **recapturable**. Losing a site should not permanently erase it from the match.
- Retaking a site should matter because it restores access to its resource/income.
- Rather than a tower that deals enormous damage by itself, a site could function like a bunker defended by minions.
- Each lane could have an **offense or defense setting**.
- On defense, minions would go to the farthest bunker and stay there to hold it instead of mindlessly advancing.

This was the final design thought before Isaac asked to save the conversation. It is a concept to develop, not a fully specified mechanic. “Farthest bunker” was not precisely defined; ownership, front-line selection and behavior when a bunker is lost remain open. Offensive minion behavior, capture rules, income ownership, control of lane settings and victory conditions also need discussion.

## Problems Isaac wants to avoid

- An early objective advantage snowballing so hard that losing the first site effectively loses the match.
- No meaningful route to catch up.
- Champion kills becoming the central purpose of every fight.
- Suicide objective attempts becoming too efficient when deaths give no direct enemy reward.
- An economy that still tells supports not to participate.
- A reward multiplier that makes grouping all five players always optimal.
- Long travel times that prevent a team from reacting to objectives.
- Treating “like League/Dota” as only a lane count while ignoring the map's interconnected play space.

The assistant suggested reliable base income and reversible capture points as possible anti-snowball measures. **Base income is an assistant suggestion, not an approved design decision.** Isaac's recapturable-settlement proposal is his own newer idea. Death downtime may still have strategic cost without a kill bounty, but no death-penalty rules were settled.

## Next design conversation, not an implementation order

Work out how the recapturable resource sites, lane offense/defense choices, shared XP and accessible jungle fit into one short, casual match. Resolve the map layout and reward model before translating these alternatives into code. Keep the older alternatives above as the reasoning trail; do not silently turn every brainstormed option into a requirement.

This update changes documentation only. It does not change the current executable, balance, map or implemented match systems.
