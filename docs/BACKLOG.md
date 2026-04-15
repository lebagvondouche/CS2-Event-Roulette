# RandomRoundEvents — Backlog

Ideas sourced from [Kandru/cs2-roll-the-dice](https://github.com/Kandru/cs2-roll-the-dice). Only new events and improvements that don't duplicate existing functionality are listed.

---

## Implemented

- [x] Vampire Round — damage heals attacker, pistols only, configurable max HP
- [x] Jammer Round — HUD disabled via `HideHUD` bit flag
- [x] Zoom Round — random FOV per player, configurable min/max
- [x] Fog of War Round — per-player `env_fog_controller`, shotguns only
- [x] Glow Round — team-colored wall outlines via `prop_dynamic` glow entities
- [x] Size Randomizer Round — random model scale, HP scales proportionally
- [x] Chicken Leader Round — flock of chickens follows each player via `m_leader` schema
- [x] Return to Sender Round — victim teleports to spawn on hit, pistols only
- [x] Weird Grenades Round — random detonation times via `OnEntitySpawned` listener
- [x] Improvement: SpeedRandomizer uses `PrintToCenterAlert` instead of `PrintToChat`

---

## Remaining

### Loud Steps Round
Crouching and walking plays loud sounds, making stealth impossible.
- Requires `OnTick` listener (fires every frame — performance concern)
- Hook button listener for `PlayerButtons.Duck` / `PlayerButtons.Speed`
- Complexity: **Low** but has performance implications

### Play as Chicken Round
Player models replaced with giant chickens.
- Create `prop_dynamic` chicken model attached to each player
- Hide real player model via render alpha = 0
- Use `CheckTransmit` to hide the chicken prop from the owning player
- Complexity: **High**

### No Explosives Round
Grenades replaced with physics props that do contact damage.
- Hook `OnEntitySpawned` for grenade projectiles
- Replace with `prop_physics_override` using custom models
- Requires custom models or server-side assets
- Complexity: **High**

### Improvement: Invisible Round — Use `CheckTransmit`
Our current approach sets render alpha to 0 which can still show shadows/outlines. RTD uses `CheckTransmit` to fully prevent entity transmission. More reliable but requires registering a listener.
