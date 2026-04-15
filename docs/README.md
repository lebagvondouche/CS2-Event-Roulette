# CS2 Event Roulette

A Counter-Strike 2 plugin that triggers random events each round. Built with CounterStrikeSharp.

## Features

26 events + Chaos Round, each announced with a description in chat:

- **Low Gravity** — Scout + Zeus, perfect accuracy, low gravity, fast zeus recharge.
- **Juan Deag** — Knife + Deagle, headshots only. Body shots deal no damage. Infinite ammo.
- **Random Weapon** — Everyone gets a random weapon.
- **Double Damage** — Glock only, all damage multiplied (configurable).
- **Team Swap** — A random pair of human players swaps teams every 30s (configurable).
- **Flashbang Spam** — Low HP (configurable), knife + flashbangs only. Knife does no damage. Auto-refill.
- **Knife-Only** — Pure melee combat. Bhop enabled.
- **Zeus-Only** — Zeus taser only, fast recharge (configurable). Bhop enabled.
- **No Reload** — One magazine, no reserve ammo. Buying enabled.
- **Gravity Switch** — Gravity flips between low and high at a configurable interval. Buying enabled.
- **Speed Randomizer** — Each player gets a random speed multiplier, shown on screen. Buying enabled.
- **Last Man Standing** — Knife + random pistol only.
- **Power-Up Round** — High HP (configurable), full armor + helmet, unlimited HE + molotov. Knife does no damage. No friendly fire.
- **Tank Round** — High HP (configurable), full armor + helmet, random shotgun, unlimited ammo.
- **Invisible Round** — All players are invisible, knife only, no friendly fire. Listen for footsteps.
- **Respawn Round** — Each team has a shared pool of respawns. Random weapon on spawn. Random spawn locations.
- **Vampire Round** — Damage dealt heals the attacker. Pistols only. Configurable max HP.
- **Jammer Round** — HUD disabled for all players. No crosshair, no health, no ammo display.
- **Zoom Round** — Everyone gets a random FOV (tunnel vision). Configurable min/max.
- **Fog of War Round** — Thick fog reduces visibility. Shotguns only. Configurable density and distance.
- **Glow Round** — All players glow through walls with team-colored outlines.
- **Size Randomizer Round** — Random player model scaling. HP scales proportionally with size.
- **Chicken Leader Round** — A flock of chickens follows each player. Configurable count and size.
- **Return to Sender Round** — Hit someone and they teleport back to spawn. Pistols only.
- **Weird Grenades Round** — All grenades have random detonation times. Configurable min/max.
- **Chaos Round** — Random mix of gravity, speed, damage, accuracy, and weapon. Every chaos round is different.

All events:
- Block purchases where appropriate (some events allow buying)
- Strip weapons where needed (no carryover between rounds)
- Clean up fully on round end (gravity, speed, nospread, timers, handlers, entities)
- Skip during warmup (unless admin forces an event)
- Announce with a title and description in chat
- Optionally give bomb back to a random T (`EnableBomb` config)

## Admin Menu

Type `!rre_menu` in chat (requires `@css/root` admin) to open an in-game menu to pick any event.

Admin commands queue the event for the next round (announced in chat).

## Installation

1. Install [CounterStrikeSharp](https://docs.cssharp.dev/docs/guides/getting-started.html) + Metamod on your CS2 server.
2. Download the latest release from the [Releases page](https://github.com/lebagvondouche/CS2-Event-Roulette/releases).
3. Extract the `RandomRoundEvents` folder into your server's plugin directory.
4. Restart the server. Config auto-generates on first load.

### Server folder structure

```
csgo/
└── addons/
    └── counterstrikesharp/
        ├── plugins/
        │   └── RandomRoundEvents/          ← plugin DLL goes here
        │       ├── RandomRoundEvents.dll
        │       └── RandomRoundEvents.deps.json
        └── configs/
            └── plugins/
                └── RandomRoundEvents/
                    └── RandomRoundEvents.json  ← config (auto-generated on first load)
```

## Configuration

Edit `addons/counterstrikesharp/configs/plugins/RandomRoundEvents/RandomRoundEvents.json`:

```json
{
  "Version": 1,
  "Debug": false,

  "EnableLowGravity": true,
  "EnableHeadshotOnly": true,
  "EnableRandomWeapon": true,
  "EnableDoubleDamage": true,
  "EnableSwapTeams": true,
  "EnableFlashbangSpam": true,
  "EnableKnifeOnly": true,
  "EnableZeusOnly": true,
  "EnableNoReload": true,
  "EnableGravitySwitch": true,
  "EnableSpeedRandomizer": true,
  "EnableLastManStanding": true,
  "EnablePowerUpRound": true,
  "EnableTankRound": true,
  "EnableInvisibleRound": true,
  "EnableRespawnRound": true,
  "EnableVampireRound": true,
  "EnableJammerRound": true,
  "EnableZoomRound": true,
  "EnableFogRound": true,
  "EnableGlowRound": true,
  "EnableSizeRound": true,
  "EnableChickenRound": true,
  "EnableReturnToSenderRound": true,
  "EnableWeirdGrenadesRound": true,

  "LowGravityValue": 400,
  "GravitySwitchLow": 400,
  "GravitySwitchHigh": 1200,
  "GravitySwitchInterval": 5,
  "SpeedMin": 0.5,
  "SpeedMax": 2.0,
  "SwapInterval": 30,
  "FlashbangStartHP": 1,
  "FlashbangRefillInterval": 3,
  "PowerUpHP": 300,
  "DoubleDamageMultiplier": 2,
  "ZeusRechargeTime": 5,
  "EnableBomb": false,
  "TankHP": 500,
  "RespawnPool": 10,
  "VampireMaxHP": 300,
  "ZoomMinFOV": 30,
  "ZoomMaxFOV": 70,
  "FogDensity": 0.99,
  "FogEndDistance": 600,
  "SizeMin": 0.5,
  "SizeMax": 2.0,
  "ChickenCount": 5,
  "ChickenSize": 2.0,
  "WeirdGrenadeMinTime": 0.1,
  "WeirdGrenadeMaxTime": 5.0,
  "ChaosRoundChance": 15
}
```

### Event Toggles

Set any `Enable*` option to `false` to remove that event from the random pool.

### Event Settings

| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| LowGravityValue | 400 | 50–800 | Gravity for Low Gravity round |
| GravitySwitchLow | 400 | 50–800 | Low gravity for Gravity Switch |
| GravitySwitchHigh | 1200 | 800–2000 | High gravity for Gravity Switch |
| GravitySwitchInterval | 5 | 1–60 | Seconds between gravity flips |
| SpeedMin | 0.5 | 0.1–3.0 | Minimum speed multiplier |
| SpeedMax | 2.0 | SpeedMin–5.0 | Maximum speed multiplier |
| SwapInterval | 30 | 5–120 | Seconds between team swaps |
| FlashbangStartHP | 1 | 1–100 | Starting HP for Flashbang round |
| FlashbangRefillInterval | 3 | 1–30 | Seconds between flashbang refills |
| PowerUpHP | 300 | 100–1000 | HP for Power-Up round |
| DoubleDamageMultiplier | 2 | 2–10 | Damage multiplier |
| ZeusRechargeTime | 5 | 0–30 | Zeus recharge seconds (0 = instant) |
| EnableBomb | false | true/false | Give C4 to a random T after weapon strip |
| TankHP | 500 | 200–1000 | HP for Tank round |
| RespawnPool | 10 | 1–50 | Shared respawns per team |
| VampireMaxHP | 300 | 100–1000 | Max HP for Vampire round attacker healing |
| ZoomMinFOV | 30 | — | Minimum FOV for Zoom round |
| ZoomMaxFOV | 70 | — | Maximum FOV for Zoom round |
| FogDensity | 0.99 | — | Fog density for Fog of War round |
| FogEndDistance | 600 | — | Fog end distance (visibility range) |
| SizeMin | 0.5 | — | Minimum player scale for Size round |
| SizeMax | 2.0 | — | Maximum player scale for Size round |
| ChickenCount | 5 | — | Number of chickens per player |
| ChickenSize | 2.0 | — | Chicken model scale |
| WeirdGrenadeMinTime | 0.1 | — | Minimum grenade detonation time (seconds) |
| WeirdGrenadeMaxTime | 5.0 | — | Maximum grenade detonation time (seconds) |
| ChaosRoundChance | 15 | 0–100 | Percentage chance of Chaos Round (0 to disable) |

## Commands

All commands require `@css/root` admin permission. Use in chat with `!` or `/` prefix. Commands queue the event for the next round.

| Command | Description |
|---------|-------------|
| `!rre_menu` | Open event selection menu |
| `!rre_lowgravity` | Low Gravity |
| `!rre_headshotonly` | Juan Deag |
| `!rre_randomweapon` | Random Weapon |
| `!rre_doubledamage` | Double Damage |
| `!rre_swapteams` | Team Swap |
| `!rre_flashbang` | Flashbang Spam |
| `!rre_knife` | Knife-Only |
| `!rre_zeus` | Zeus-Only |
| `!rre_noreload` | No Reload |
| `!rre_gravityswitch` | Gravity Switch |
| `!rre_speed` | Speed Randomizer |
| `!rre_lastman` | Last Man Standing |
| `!rre_powerup` | Power-Up Round |
| `!rre_tank` | Tank Round |
| `!rre_invisible` | Invisible Round |
| `!rre_respawn` | Respawn Round |
| `!rre_vampire` | Vampire Round |
| `!rre_jammer` | Jammer Round |
| `!rre_zoom` | Zoom Round |
| `!rre_fog` | Fog of War Round |
| `!rre_glow` | Glow Round |
| `!rre_size` | Size Randomizer Round |
| `!rre_chicken` | Chicken Leader Round |
| `!rre_return` | Return to Sender Round |
| `!rre_weirdnades` | Weird Grenades Round |
| `!rre_chaos` | Chaos Round |
| `!rre_reset` | Reset all events |

## Building from Source

Requires [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone https://github.com/lebagvondouche/CS2-Event-Roulette.git
cd CS2-Event-Roulette
dotnet restore
dotnet publish --configuration Release --output bin/publish
```

Copy the `bin/publish/RandomRoundEvents` folder to your server's `addons/counterstrikesharp/plugins/` directory.

## License

CC BY-NC 4.0 — free to fork and modify, no commercial use. See [LICENSE](../LICENSE).

---

**Author:** Martin Persson
**Version:** 0.6.0
**License:** CC BY-NC 4.0
