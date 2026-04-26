# CS2 Event Roulette Docs

This document is the technical reference for `CS2 Event Roulette`.

Use the root [README.md](../README.md) for the public-facing overview. Use this page when you need to install, configure, build, debug, or extend the plugin.

## Overview

`CS2 Event Roulette` is a CounterStrikeSharp plugin that applies one special round modifier at a time. The codebase was refactored into event modules plus shared helpers so new rounds can be added without turning the main plugin back into a monolith.

Current round pool:

- `27` standard rounds
- `1` special recipe round: `Mayhem`
- `1` manual-only WIP round: `Clown Grenades`

## Runtime Model

At a high level, each round goes through the same flow:

1. choose the next event, either randomly or through an admin command
2. announce the event before round start
3. apply the round-specific behavior when the round becomes active
4. maintain timers, listeners, or entity hooks while the event is live
5. reset all round-owned state before the next event

Important implementation traits:

- The main plugin owns config, command registration, round selection, event orchestration, and cleanup.
- Event modules own event-specific behavior.
- Helper modules own shared weapon, player, spawn, settings, model, and diagnostics logic.
- Config values are clamped in `OnConfigParsed` so bad values fail soft instead of blowing up the server.

## Project Layout

Public branding uses `CS2 Event Roulette`, while the current code/plugin file names still use `RandomRoundEvents`.

```text
RandomEvents/
|-- RandomRoundEvents.cs
|-- RandomRoundEvents.csproj
|-- RandomRoundEvents.json
|-- events/
|   |-- grenaderoulette.cs
|   |-- loadoutcombat.cs
|   |-- mayhem.cs
|   |-- movementworld.cs
|   |-- rainbowsmokes.cs
|   |-- respawn.cs
|   |-- toxicsmokes.cs
|   |-- visibilityinfo.cs
|   `-- clowngrenades.cs
|-- helpers/
|   |-- diagnostics.cs
|   |-- models.cs
|   |-- players.cs
|   |-- settings.cs
|   |-- spawns.cs
|   `-- weapons.cs
`-- docs/
    `-- README.md
```

## Module Responsibilities

### Main Orchestrator

- [RandomRoundEvents.cs](../RandomRoundEvents.cs)

Owns:

- config model and validation
- chat/admin command registration
- event selection and forced event queueing
- round start/end orchestration
- shared timers owned by the core plugin
- shared utility methods used across event modules

### Event Modules

- [events/loadoutcombat.cs](../events/loadoutcombat.cs)
  Round types driven mostly by loadout, HP, armor, FOV, or repeated player-wide effects
- [events/movementworld.cs](../events/movementworld.cs)
  Gravity, speed, swap, and world-state rounds
- [events/visibilityinfo.cs](../events/visibilityinfo.cs)
  Invisible, X-Ray, and visibility-related behavior
- [events/respawn.cs](../events/respawn.cs)
  Team respawn pool management and respawn placement
- [events/grenaderoulette.cs](../events/grenaderoulette.cs)
  Weird fuse timing for HE, flash, smoke, and decoy only
- [events/rainbowsmokes.cs](../events/rainbowsmokes.cs)
  Neon-color smoke grenades
- [events/toxicsmokes.cs](../events/toxicsmokes.cs)
  Toxic smoke clouds with damage ticks and poison feedback
- [events/mayhem.cs](../events/mayhem.cs)
  Recipe-based combined rounds built from compatible modifiers
- [events/clowngrenades.cs](../events/clowngrenades.cs)
  Manual-only WIP visual grenade proxy experiment

### Helper Modules

- [helpers/weapons.cs](../helpers/weapons.cs)
  Shared weapon giving, loadout retries, ammo handling, grenade helpers
- [helpers/players.cs](../helpers/players.cs)
  Shared player enumeration and convenience checks
- [helpers/spawns.cs](../helpers/spawns.cs)
  Shared spawn relocation and round placement helpers
- [helpers/settings.cs](../helpers/settings.cs)
  Round-scoped ConVar and server setting management
- [helpers/models.cs](../helpers/models.cs)
  Model discovery and filtering used by visual rounds
- [helpers/diagnostics.cs](../helpers/diagnostics.cs)
  Debug dump helpers such as model scanning support

## Round Families

The plugin is easiest to reason about when grouped by behavior family instead of alphabetical name.

### Loadout and Combat Rounds

- Juan Deag
- Random Weapon
- Double Damage
- Flashbang Spam
- Knife-Only
- Zeus-Only
- No Reload
- Last Man Standing
- Power-Up Round
- Tank Round
- Vampire Round
- Inception Round
- Screen Shake Round

### Movement and World Rules

- Low Gravity
- Gravity Switch
- Speed Randomizer
- Team Swap
- Return to Sender
- Chicken Leader

### Visibility and Information

- Invisible Round
- X-Ray Goggles Round
- Size Randomizer Round

### Respawn and Round-Flow Rules

- Respawn Round

### Grenade-Based Rounds

- Grenade Roulette
- Rainbow Smokes
- Toxic Green Smokes
- Clown Grenades (manual-only WIP)

### Special Recipe Round

- Mayhem Round

## Installation

1. Install [CounterStrikeSharp](https://docs.cssharp.dev/docs/guides/getting-started.html) and Metamod on your CS2 server.
2. Download the latest release from the [GitHub Releases page](https://github.com/lebagvondouche/CS2-Event-Roulette/releases).
3. Copy the published plugin folder into `csgo/addons/counterstrikesharp/plugins/RandomRoundEvents`.
4. Copy the config into `csgo/addons/counterstrikesharp/configs/plugins/RandomRoundEvents/RandomRoundEvents.json`.
5. Restart the server or reload the plugin.

## Server Layout

```text
csgo/
`-- addons/
    `-- counterstrikesharp/
        |-- plugins/
        |   `-- RandomRoundEvents/
        |       |-- RandomRoundEvents.dll
        |       `-- RandomRoundEvents.deps.json
        `-- configs/
            `-- plugins/
                `-- RandomRoundEvents/
                    `-- RandomRoundEvents.json
```

## Sample Config

```json
{
  "ConfigVersion": 1,
  "Debug": true,
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
  "EnableZoomRound": true,
  "EnableGlowRound": true,
  "EnableSizeRound": true,
  "EnableChickenRound": true,
  "EnableReturnToSenderRound": true,
  "EnableGrenadeRouletteRound": true,
  "EnableRainbowSmokesRound": true,
  "EnableToxicSmokesRound": true,
  "EnableScreenShakeRound": true,
  "EnableClownGrenadesRound": false,
  "LowGravityValue": 300,
  "GravitySwitchLow": 200,
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
  "EnableBomb": true,
  "TankHP": 500,
  "RespawnPool": 10,
  "VampireMaxHP": 300,
  "ZoomMinFOV": 30,
  "ZoomMaxFOV": 70,
  "SizeMin": 0.5,
  "SizeMax": 2.0,
  "ChickenCount": 5,
  "ChickenSize": 2.0,
  "WeirdGrenadeMinTime": 0.1,
  "WeirdGrenadeMaxTime": 15.0,
  "ToxicSmokeDamagePerTick": 4,
  "ToxicSmokeTickInterval": 0.5,
  "ToxicSmokeRadius": 180.0,
  "ToxicSmokeDuration": 18.0,
  "ToxicSmokeDebuffCueInterval": 1.0,
  "ToxicSmokeShakeDuration": 0.35,
  "ToxicSmokeShakeAmplitude": 2.5,
  "ToxicSmokeShakeFrequency": 1.5,
  "ScreenShakeInterval": 4.0,
  "ScreenShakeDuration": 0.6,
  "ScreenShakeAmplitude": 4.0,
  "ScreenShakeFrequency": 2.0,
  "MayhemRoundChance": 15,
  "MayhemRoundBlocklist": [
    "HeadshotOnly",
    "SwapTeams",
    "FlashbangSpam",
    "KnifeOnly",
    "ZeusOnly",
    "PowerUpRound",
    "TankRound",
    "InvisibleRound",
    "RespawnRound"
  ]
}
```

## Config Reference

### Global Control

| Setting | Description |
| --- | --- |
| `Debug` | Enables debug logging for experimental or diagnostics-heavy paths |
| `EnableBomb` | Preserves and refreshes C4 handling for bomb-enabled rounds |
| `MayhemRoundChance` | Percentage chance that a round becomes Mayhem instead of a standard event |
| `MayhemRoundBlocklist` | Rounds excluded from Mayhem recipe generation |

### Event Toggles

| Setting | Description |
| --- | --- |
| `EnableZoomRound` | Enables Inception Round |
| `EnableGlowRound` | Enables X-Ray Goggles Round |
| `EnableGrenadeRouletteRound` | Enables Grenade Roulette Round |
| `EnableRainbowSmokesRound` | Enables Rainbow Smokes Round |
| `EnableToxicSmokesRound` | Enables Toxic Green Smokes Round |
| `EnableScreenShakeRound` | Enables Screen Shake Round |
| `EnableClownGrenadesRound` | Reserved for the Clown Grenades WIP round; automatic rotation is currently disabled |

### Movement and Physics Tuning

| Setting | Description |
| --- | --- |
| `LowGravityValue` | Gravity used in Low Gravity |
| `GravitySwitchLow` / `GravitySwitchHigh` | Gravity values used in Gravity Switch |
| `SpeedMin` / `SpeedMax` | Player speed range for Speed Randomizer |
| `SwapInterval` | Team swap interval used in Team Swap |

### Combat and Survival Tuning

| Setting | Description |
| --- | --- |
| `FlashbangStartHP` | Starting HP for Flashbang Spam |
| `FlashbangRefillInterval` | Flashbang refill cadence |
| `PowerUpHP` | HP for Power-Up Round |
| `DoubleDamageMultiplier` | Multiplier used in Double Damage |
| `TankHP` | HP for Tank Round |
| `VampireMaxHP` | Upper heal cap for Vampire Round |
| `RespawnPool` | Shared respawn count per team |
| `ZeusRechargeTime` | Recharge cadence for Zeus-oriented rounds |

### Vision and Scale Tuning

| Setting | Description |
| --- | --- |
| `ZoomMinFOV` / `ZoomMaxFOV` | FOV range for Inception |
| `SizeMin` / `SizeMax` | Scale range for Size Randomizer |

### Chicken and Visual Chaos

| Setting | Description |
| --- | --- |
| `ChickenCount` / `ChickenSize` | Chicken Leader tuning |
| `ScreenShakeInterval` | Time between global shake pulses during Screen Shake Round |
| `ScreenShakeDuration` / `ScreenShakeAmplitude` / `ScreenShakeFrequency` | Screen Shake Round pulse tuning |

### Grenade and Smoke Rounds

| Setting | Description |
| --- | --- |
| `WeirdGrenadeMinTime` / `WeirdGrenadeMaxTime` | Random detonation timing range for Grenade Roulette |
| `ToxicSmokeDamagePerTick` / `ToxicSmokeTickInterval` | Damage amount and tick cadence for Toxic Green Smokes |
| `ToxicSmokeRadius` / `ToxicSmokeDuration` | Toxic Green Smokes cloud radius and active duration |
| `ToxicSmokeDebuffCueInterval` | Minimum time between poison debuff cues for the same player |
| `ToxicSmokeShakeDuration` / `ToxicSmokeShakeAmplitude` / `ToxicSmokeShakeFrequency` | Toxic debuff shake tuning |

## Commands

All commands require `@css/root` and queue the event for the next round.

| Command | Round |
| --- | --- |
| `!rre_menu` | Open the admin event menu |
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
| `!rre_inception` | Inception Round |
| `!rre_xraygoggles` | X-Ray Goggles Round |
| `!rre_size` | Size Randomizer Round |
| `!rre_chicken` | Chicken Leader Round |
| `!rre_returntosender` | Return to Sender Round |
| `!rre_grenaderoulette` | Grenade Roulette Round |
| `!rre_rainbowsmokes` | Rainbow Smokes Round |
| `!rre_toxicsmokes` | Toxic Green Smokes Round |
| `!rre_screenshake` | Screen Shake Round |
| `!rre_clowngrenades` | Clown Grenades Round |
| `!rre_mayhem` | Mayhem Round |
| `!rre_reset` | Reset all active event state |

## Technical Notes By Feature

### Grenade Roulette

- Buy-enabled round
- Only HE, flash, smoke, and decoy are randomized
- Molotov/incendiary support was deliberately removed after live testing showed those projectiles ignore the exposed timer path in a reliable way

### Rainbow Smokes

- Uses smoke projectile color assignment
- Current implementation uses a curated neon palette instead of unconstrained random RGB

### Toxic Green Smokes

- Tracks activated smoke clouds as timed danger zones
- Applies periodic damage inside a configured radius
- Applies a poison cue through shake plus center alert
- Cloud stacking is currently allowed

### Invisible Round

- Hides players and weapon world models from other players using transmit filtering
- Local first-person knife/viewmodel visibility is tolerated
- Zeus is intentionally included

### Screen Shake Round

- Global shake pulses are timer-driven and fully config-controlled
- Designed as a buy round, not a restricted loadout round

### Mayhem

- Built from compatible recipe components rather than fully arbitrary modifier stacking
- Uses a blocklist to avoid combinations already known to be poor or unstable

### Clown Grenades

- Kept manual-only for now
- Works best on maps that expose a useful prop model pool to the server
- Not part of normal automatic random rotation

## Build

```powershell
dotnet restore
dotnet build RandomRoundEvents.csproj -c Release
dotnet publish RandomRoundEvents.csproj -c Release -o publish/RandomRoundEvents
```

## Release Workflow

The GitHub Actions workflow in [.github/workflows/release.yml](../.github/workflows/release.yml) builds, publishes, zips the plugin output, and creates a tagged GitHub release artifact.

## Maintenance Notes

- Warmup is skipped unless an admin forces a round.
- Buying is enabled only for the rounds that are designed around it.
- Test-server config should stay in sync with [RandomRoundEvents.json](../RandomRoundEvents.json) whenever new settings are added.
- `Clown Grenades` is intentionally parked as a WIP/manual-only round.

## License

CC BY-NC 4.0. See [LICENSE](../LICENSE).
