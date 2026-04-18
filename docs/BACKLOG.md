# RandomRoundEvents - Backlog

Ideas sourced from [Kandru/cs2-roll-the-dice](https://github.com/Kandru/cs2-roll-the-dice). Only new events and improvements that do not duplicate existing functionality are listed.

---

## Implemented

- [x] Vampire Round - damage heals attacker, pistols only, configurable max HP
- [x] Inception Round - random FOV per player, configurable min/max
- [x] X-Ray Goggles Round - team-colored wall outlines via `prop_dynamic` glow entities
- [x] Size Randomizer Round - random model scale, HP scales proportionally
- [x] Chicken Leader Round - flock of chickens follows each player via `m_leader` schema
- [x] Return to Sender Round - victim teleports to spawn on hit, pistols only
- [x] Grenade Roulette Round - random detonation times via `OnEntitySpawned` listener
- [x] Improvement: SpeedRandomizer uses `PrintToCenterAlert` instead of `PrintToChat`
- [x] Tank Round now uses infinite ammo behavior instead of reserve refill only
- [x] Juan Deag no longer enables unlimited ammo
- [x] Mayhem Round redesigned into a curated recipe generator with logged loadout/world/combat/info combinations
- [x] Fog Round removed from active plugin after instability and crash issues
- [x] No-Reload Round polished to AK-only with zero reserve ammo
- [x] Jammer / No HUD Round removed from the plugin
- [x] X-Ray Goggles Round renamed and buying enabled
- [x] Grenade Roulette Round updated to be a normal buy round with weird grenade timings
- [x] Chicken Leader Round cleanup added so spawned chickens are removed on reset
- [x] Size Randomizer Round buying enabled
- [x] Power-Up Round no longer piles grenade gear onto corpses
- [x] Weapon carry-over between rounds and events has not reproduced in live testing
- [x] Respawn Round counter stability looks good in live testing
- [x] Bomb inventory selection has been stable in live testing
- [x] Return to Sender Round teleport behavior has been stable in live testing
- [x] Respawn Round loadout and relocation behavior has been stable in live testing
- [x] Mayhem Round works in live manual testing
- [x] Mayhem Round automatic rotation re-enabled

---

## Remaining

### Active Issues

#### Invisible Round - knife visibility
- Knives remain visible during Invisible Round
- Determine whether this is an engine limitation, a viewmodel/worldmodel limitation, or something `CheckTransmit` or weapon handling can improve

### Future Rework

#### Fog Round
- Investigate a stable implementation that does not rely on unsafe render-mode guesses
- Confirm whether CS2 model visibility can be softened without abrupt pop-in from `CheckTransmit`
- Only restore the round after a controlled live-server test pass

#### Improvement: Invisible Round - use `CheckTransmit`
- The current approach uses render alpha changes and still allows some visual leakage
- RTD uses `CheckTransmit` to fully prevent entity transmission
- This is more reliable but requires careful listener handling and live testing

### Config expansions
- We should expand the config file with which weapons should be used on random weapon rounds.

### New Ideas

#### Clown Grenades Round
- WIP: visual proxy prototype exists, but loaded model availability is still strongly map-dependent
- Inferno-style maps can expose usable props; Dust2 currently does not expose a reliable pool
- Keep this round disabled from automatic rotation for now
- Resume later with a better cross-map model strategy
- Complexity: **High**

#### Suicide Planter Round
- If a T successfully plants the bomb, the planter is killed immediately after the plant completes
- Prefer hooking the successful plant event so the bomb stays planted and only the planter dies
- Verify how this should behave with armor/health overrides, MVP, and killfeed attribution
- Complexity: **Medium**

#### Auto-Planted Bomb Round
- Automatically plant the bomb at a bombsite when the round starts so both teams have to race toward the site
- Prefer using a real planted bomb state instead of faking it with HUD-only tricks
- Needs careful validation of how to create or trigger a planted C4 safely in CounterStrikeSharp without corrupting round state
- If implemented, consider randomizing the site and keeping the existing planted-bomb glow and CT hint ideas
- Complexity: **High**

