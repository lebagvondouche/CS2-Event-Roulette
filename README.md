![CS2 Event Roulette logo](logo/logo.png)

# CS2 Event Roulette

`CS2 Event Roulette` turns a normal Counter-Strike 2 server into a round-by-round chaos machine.

One round you are floating through the map in `Low Gravity`, the next you are trapped in `Tank Round` with an `XM1014`, and a few rounds later the whole server is choking through `Toxic Green Smokes` or getting rattled by `Screen Shake Round`. It is built for CounterStrikeSharp and designed to keep public servers, private events, and admin nights feeling unpredictable without turning into total nonsense.

## Why It Is Fun

- One special rule set at a time, so rounds stay readable instead of becoming total mush
- Fast admin control with chat commands and an in-game menu
- A mix of silly rounds, sweaty rounds, presentation rounds, and grenade gimmicks
- Enough config to tune the chaos without editing code
- A refactored event/helper structure that is much easier to extend than the original single-file version

## Round Sampler

Some favorites in the current pool:

- `Low Gravity`
- `Tank Round`
- `Invisible Round`
- `Respawn Round`
- `Return to Sender`
- `Grenade Roulette`
- `Rainbow Smokes`
- `Toxic Green Smokes`
- `Screen Shake Round`
- `Mayhem Round`

The plugin currently includes `27` standard rounds plus `Mayhem`, with `Clown Grenades` kept around as a manual-only WIP experiment.

## Quick Start

1. Install [CounterStrikeSharp](https://docs.cssharp.dev/docs/guides/getting-started.html) and Metamod on your server.
2. Download a release or build the plugin yourself.
3. Copy the plugin output into `csgo/addons/counterstrikesharp/plugins/RandomRoundEvents`.
4. Copy [RandomRoundEvents.json](RandomRoundEvents.json) into `csgo/addons/counterstrikesharp/configs/plugins/RandomRoundEvents/`.
5. Restart the server or reload the plugin.

## Repo Layout

Public branding uses `CS2 Event Roulette`, while the current code/plugin file names still use `RandomRoundEvents`.

- [RandomRoundEvents.cs](RandomRoundEvents.cs) - main plugin orchestrator, config model, command registration, and round selection
- [events](events) - round implementations grouped by behavior family
- [helpers](helpers) - shared player, weapon, spawn, settings, diagnostics, and model utilities
- [RandomRoundEvents.json](RandomRoundEvents.json) - sample config with all supported settings
- [docs/README.md](docs/README.md) - full technical docs, commands, config reference, architecture notes, and build workflow

## Built For

- community servers that want variety without a full modpack
- admin-run event nights
- testing weird round ideas quickly
- extending with new CounterStrikeSharp-based gimmicks

## Development

```powershell
dotnet restore
dotnet build RandomRoundEvents.csproj -c Release
```

If you want the deeper operator and developer view, start with [docs/README.md](docs/README.md).
