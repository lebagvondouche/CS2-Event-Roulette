# RandomRoundEvents Plugin for Counter-Strike 2

A Counter-Strike 2 plugin that triggers random events during rounds to add variety and excitement to gameplay.

## Features

The plugin includes the following events:

- **Low Gravity**: Low gravity with Scout + Zeus only. No buying.
- **Headshot Only**: Only headshots deal damage, body shots are ignored.
- **Random Weapon**: All players are stripped of weapons and given a random one.
- **Double Damage**: All damage is multiplied (configurable). 
- **Swap Teams**: A random player from each team swaps sides every 30 seconds (configurable).
- **Flashbang Spam**: Stripped of weapons, low HP (configurable), flashbangs only.
- **Knife-Only**: All players are stripped of weapons and given only a knife.
- **Zeus-Only**: All players are stripped of weapons and given only a Zeus taser.
- **No Reload**: One magazine only, no reserve ammo.
- **Gravity Switch**: Gravity alternates between low and high at a configurable interval.
- **Speed Randomizer**: Each player gets a random movement speed (configurable range).
- **Last Man Standing**: All players are stripped of weapons and given a random pistol.
- **Power-Up Round**: High HP (configurable), full armor + helmet, and HE grenades.

## Installation

1. **Download the Plugin:**
   - Ensure you have the latest version of the plugin from the releases section.

2. **Install CounterStrikeSharp:**
   - Follow the [CounterStrikeSharp installation guide](https://docs.cssharp.dev/docs/guides/getting-started.html) to set up the framework.

3. **Add the Plugin:**
   - Place the published `RandomRoundEvents` folder into the `addons/counterstrikesharp/plugins/` directory of your CS2 server.
   - The `RandomRoundEvents.json` config will be auto-generated on first load in `addons/counterstrikesharp/configs/plugins/RandomRoundEvents/`.

4. **Restart the Server:**
   - Restart your CS2 server to load the plugin.

## Configuration

The plugin can be configured using the `RandomRoundEvents.json` file. Here is an example configuration:

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
  "DoubleDamageMultiplier": 2
}
```

### Configuration Options

#### Event Toggles

- **Version**: Configuration version (do not modify).
- **Debug**: Enable debug logging.
- **EnableLowGravity**: Enable the Low Gravity event.
- **EnableHeadshotOnly**: Enable the Headshot Only event.
- **EnableRandomWeapon**: Enable the Random Weapon event.
- **EnableDoubleDamage**: Enable the Double Damage event.
- **EnableSwapTeams**: Enable the Swap Teams event.
- **EnableFlashbangSpam**: Enable the Flashbang Spam event.
- **EnableKnifeOnly**: Enable the Knife-Only event.
- **EnableZeusOnly**: Enable the Zeus-Only event.
- **EnableNoReload**: Enable the No Reload event.
- **EnableGravitySwitch**: Enable the Gravity Switch event.
- **EnableSpeedRandomizer**: Enable the Speed Randomizer event.
- **EnableLastManStanding**: Enable the Last Man Standing event.
- **EnablePowerUpRound**: Enable the Power-Up Round event.

#### Event Settings

- **LowGravityValue**: Gravity value for Low Gravity round (default: 400, range: 50–800).
- **GravitySwitchLow**: Low gravity value for Gravity Switch (default: 400).
- **GravitySwitchHigh**: High gravity value for Gravity Switch (default: 1200).
- **GravitySwitchInterval**: Seconds between gravity flips (default: 5, range: 1–60).
- **SpeedMin**: Minimum speed multiplier (default: 0.5, range: 0.1–3.0).
- **SpeedMax**: Maximum speed multiplier (default: 2.0, range: SpeedMin–5.0).
- **SwapInterval**: Seconds between team swaps (default: 30, range: 5–120).
- **FlashbangStartHP**: Starting HP for Flashbang Spam round (default: 1, range: 1–100).
- **FlashbangRefillInterval**: Seconds between flashbang refills (default: 3, range: 1–30).
- **PowerUpHP**: HP for Power-Up round (default: 300, range: 100–1000).
- **DoubleDamageMultiplier**: Damage multiplier (default: 2, range: 2–10).

## Usage

Once installed and configured, the plugin will automatically trigger a random event at the start of each round. The active event will be announced in the chat.

Purchases are automatically blocked during most events to prevent players from overriding the event loadout. Events that give specific weapons (Knife-Only, Zeus-Only, Last Man Standing, Power-Up) allow the round to play out with the given gear.

### Manual Event Triggers

All manual commands require `@css/root` admin permission. You can trigger specific events using the following console or chat commands (chat commands use `!rre_` or `/rre_` prefix):

- `css_rre_lowgravity`: Trigger Low Gravity event.
- `css_rre_headshotonly`: Trigger Headshot Only event.
- `css_rre_randomweapon`: Trigger Random Weapon event.
- `css_rre_doubledamage`: Trigger Double Damage event.
- `css_rre_swapteams`: Trigger Swap Teams event.
- `css_rre_flashbang`: Trigger Flashbang Spam event.
- `css_rre_knife`: Trigger Knife-Only event.
- `css_rre_zeus`: Trigger Zeus-Only event.
- `css_rre_noreload`: Trigger No Reload event.
- `css_rre_gravityswitch`: Trigger Gravity Switch event.
- `css_rre_speed`: Trigger Speed Randomizer event.
- `css_rre_lastman`: Trigger Last Man Standing event.
- `css_rre_powerup`: Trigger Power-Up Round event.
- `css_rre_reset`: Reset all events.

## Building from Source

### Requirements

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Steps

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/your-repository/RandomRoundEvents.git
   cd RandomRoundEvents
   ```

2. **Restore Dependencies:**
   ```bash
   dotnet restore
   ```

3. **Build the Plugin:**
   ```bash
   dotnet build --configuration Release
   ```

4. **Publish the Plugin:**
   ```bash
   dotnet publish --configuration Release --output bin/publish
   ```

5. **Copy the Output:**
   - Copy the published `RandomRoundEvents` folder to your server's `addons/counterstrikesharp/plugins/` directory.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any bugs, features, or improvements.

## License

This project is licensed under the CC BY-NC 4.0 License. See the [LICENSE](../LICENSE) file for details.

## Support

For support or questions, please open an issue on the GitHub repository.

---

**Author:** Martin Persson
**Version:** 1.2
**License:** CC BY-NC 4.0
