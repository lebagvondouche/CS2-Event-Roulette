# CS2 Event Roulette Docs

CS2 Event Roulette is a Counter-Strike 2 plugin for CounterStrikeSharp that applies one special round event at a time.

## Overview

The plugin currently includes 25 standard rounds plus Mayhem.

Standard rounds:

- Low Gravity
- Juan Deag
- Random Weapon
- Double Damage
- Team Swap
- Flashbang Spam
- Knife-Only
- Zeus-Only
- No Reload
- Gravity Switch
- Speed Randomizer
- Last Man Standing
- Power-Up Round
- Tank Round
- Invisible Round
- Respawn Round
- Vampire Round
- Inception Round
- X-Ray Goggles Round
- Size Randomizer Round
- Chicken Leader Round
- Return to Sender Round
- Grenade Roulette Round
- Rainbow Smokes Round
- Clown Grenades Round

Special round:

- Mayhem Round

## Project Layout

Public branding uses `CS2 Event Roulette`, while the current code/plugin file names still use `RandomRoundEvents`.

```text
RandomEvents/
|-- RandomRoundEvents.cs
|-- RandomRoundEvents.csproj
|-- RandomRoundEvents.json
|-- events/
|   |-- loadoutcombat.cs
|   |-- movementworld.cs
|   |-- visibilityinfo.cs
|   |-- respawn.cs
|   |-- mayhem.cs
|   |-- grenaderoulette.cs
|   |-- rainbowsmokes.cs
|   `-- clowngrenades.cs
|-- helpers/
|   |-- diagnostics.cs
|   |-- models.cs
|   |-- players.cs
|   |-- spawns.cs
|   |-- weapons.cs
|   `-- settings.cs
`-- docs/
    `-- BACKLOG.md
```

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

## Important Settings

| Setting | Description |
| --- | --- |
| `EnableBomb` | Preserves and refreshes C4 handling for bomb-enabled rounds |
| `EnableZoomRound` | Enables Inception Round |
| `EnableGlowRound` | Enables X-Ray Goggles Round |
| `EnableGrenadeRouletteRound` | Enables Grenade Roulette Round |
| `EnableRainbowSmokesRound` | Enables Rainbow Smokes Round |
| `EnableClownGrenadesRound` | Reserved for the Clown Grenades WIP round; automatic rotation is currently disabled |
| `LowGravityValue` | Gravity used in Low Gravity |
| `GravitySwitchLow` / `GravitySwitchHigh` | Gravity values used in Gravity Switch |
| `SpeedMin` / `SpeedMax` | Player speed range for Speed Randomizer |
| `FlashbangStartHP` | Starting HP for Flashbang Spam |
| `PowerUpHP` | HP for Power-Up Round |
| `TankHP` | HP for Tank Round |
| `RespawnPool` | Shared respawn count per team |
| `VampireMaxHP` | Upper heal cap for Vampire Round |
| `ZoomMinFOV` / `ZoomMaxFOV` | FOV range for Inception |
| `SizeMin` / `SizeMax` | Scale range for Size Randomizer |
| `ChickenCount` / `ChickenSize` | Chicken Leader tuning |
| `WeirdGrenadeMinTime` / `WeirdGrenadeMaxTime` | Random detonation timing range for Grenade Roulette |
| `MayhemRoundChance` | Percentage chance that a round becomes Mayhem instead of a standard event |
| `MayhemRoundBlocklist` | Rounds excluded from Mayhem recipe generation |

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
| `!rre_clowngrenades` | Clown Grenades Round |
| `!rre_mayhem` | Mayhem Round |
| `!rre_reset` | Reset all active event state |

## Build

```powershell
dotnet restore
dotnet build RandomRoundEvents.csproj -c Release
dotnet publish RandomRoundEvents.csproj -c Release -o publish/RandomRoundEvents
```

## Release Workflow

The GitHub Actions workflow in [.github/workflows/release.yml](../.github/workflows/release.yml) builds, publishes, zips the plugin output, and creates a tagged GitHub release artifact.

## Notes

- Warmup is skipped unless an admin forces a round.
- Buying is enabled only for the rounds that are designed around it.
- Mayhem automatic rotation is enabled again through `MayhemRoundChance`.
- Clown Grenades is currently WIP and kept out of automatic random rotation. Use `!rre_clowngrenades` for manual testing only.

## License

CC BY-NC 4.0. See [LICENSE](../LICENSE).
