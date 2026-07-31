# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A CounterStrikeSharp plugin for Counter-Strike 2 that applies one random "special round" modifier per round (low gravity, tank round, invisible round, toxic smokes, etc.). Public branding is `CS2 Event Roulette`; the code, project file, and in-game commands still use the original name `RandomRoundEvents`.

For config reference, the full command list, round-family descriptions, and per-feature technical notes, see [docs/README.md](docs/README.md) — it is kept current and should be treated as the source of truth over re-deriving that information from code.

## Build

```bash
dotnet restore
dotnet build RandomRoundEvents.csproj -c Release
dotnet publish RandomRoundEvents.csproj -c Release -o publish/RandomRoundEvents
```

There is no test suite — this is a server-side game plugin; validation happens by running it on a CounterStrikeSharp/Metamod CS2 server (see docs/README.md's install steps) and by reading `Debug`-gated log output.

Target: `net10.0`, nullable enabled, depends only on `CounterStrikeSharp.API` (referenced with `<Private>false</Private>`, i.e. resolved against the CounterStrikeSharp install at runtime, not bundled).

Releases are cut by `.github/workflows/release.yml`, triggered on `v*` tags: it publishes, strips `.pdb`, bundles `RandomRoundEvents.json`, zips, and attaches to a GitHub Release.

## Architecture

**Composition over partial classes.** `RandomRoundEvents.cs` defines the `RandomRoundEvents : BasePlugin, IPluginConfig<RandomRoundEventsConfig>` orchestrator and an `EventType` enum for every round. Each file under `events/` and `helpers/` is a plain `internal sealed class` that takes the plugin instance in its constructor (`private readonly RandomRoundEvents _plugin`) and calls back into it for shared state/utilities — they are not partial-class fragments of `RandomRoundEvents`, just grouped by the same `namespace RandomRoundEvents`. The orchestrator instantiates one of each in its constructor and holds them as fields (`_loadoutCombat`, `_movementWorld`, `_toxicSmokes`, etc.).

**Round lifecycle**, driven entirely from the orchestrator:
1. Next event chosen — random from enabled `EventType`s, or forced via an admin `!rre_*` command (`_forcedEvent`), or upgraded to `MayhemRound` per `MayhemRoundChance`.
2. Event announced before round start.
3. On round start, the orchestrator's `switch (selectedEvent)` dispatches to the owning module's `Apply(EventType)` (e.g. `_loadoutCombat.Apply(...)`, `_movementWorld.Apply(...)`), which sets `_plugin.ShowEvent(title, description)` and applies the round-specific effect.
4. Any timers/listeners/entity hooks for the live event run until round end.
5. All round-owned state is reset before the next event is chosen (see `!rre_reset` / `OnResetCommand`).

**Adding a new round** means: add an `EventType` value + `Enable<Name>` config bool in `RandomRoundEvents.cs`, add it to the enabled-events list and the dispatch `switch`, add an `AddCommand("css_rre_<name>", ...)` registration, and implement the behavior in the appropriate `events/*.cs` module (or a new one) grouped by behavior family — see docs/README.md's "Module Responsibilities" table for which file owns which family. Update `RandomRoundEvents.json` (sample config) and docs/README.md's config/command tables to match.

**Config safety**: `OnConfigParsed` clamps out-of-range values so a bad config fails soft rather than crashing the server. The config class also carries `[JsonIgnore]`-guarded legacy aliases (e.g. `EnableWeirdGrenadesRound`, `ChaosRoundChance`/`ChaosRoundBlocklist`) for backwards compatibility with older config files — preserve this pattern when renaming config keys rather than breaking existing configs.

**Helper modules** (`helpers/`) are shared, cross-round utilities, not round logic: `weapons.cs` (giving/stripping loadouts, ammo, grenades), `players.cs` (enumeration/checks), `spawns.cs` (relocation/placement), `settings.cs` (round-scoped ConVar management), `models.cs` (prop/model discovery for visual rounds), `diagnostics.cs` (debug dumps, e.g. `!rre_dumpmodels`).

## CounterStrikeSharp framework

This plugin runs on [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp), a .NET scripting layer that hooks into CS2 via a Metamod:Source plugin. As of `CounterStrikeSharp.API` 1.0.369, the framework requires .NET 10 (LTS) — this project and its host CounterStrikeSharp install must both run .NET 10. Framework docs live at [docs.cssharp.dev](https://docs.cssharp.dev/); source/API reference at the GitHub repo above.

This codebase consistently uses the **manual/`Load()`-based** registration style over the attribute-based style the docs lead with (`[GameEventHandler]`, `[ConsoleCommand]`) — match the existing style rather than introducing attributes:

- **Game events**: `RegisterEventHandler<EventFoo>(Handler, HookMode.Post)` called from `Load()`, handler signature `HookResult Handler(EventFoo @event, GameEventInfo info)`. Return `HookResult.Continue` (or `.Handled`/`.Stop` to cancel/suppress on a pre-hook). `GameEvent` instances and their fields stop being valid after the handler returns — copy any values you need into locals before deferring work into a timer or `Server.NextFrame()`.
- **Commands**: `AddCommand("css_<name>", "description", Handler)` called from `Load()`, handler signature `void Handler(CCSPlayerController? player, CommandInfo command)`. CounterStrikeSharp strips the `css_` prefix and adds the plugin's chat trigger, which is why in-game commands in this repo are `!rre_*` while registration strings are `css_rre_*`.
- **Players**: every player is a `CCSPlayerController` (identity, team, admin flags) plus a separate `CCSPlayerPawn` (the physical body — health, position, weapons) reached via `player.PlayerPawn.Value`. Always null/validity-check both (`player.IsValid`, `player.PlayerPawn.Value != null`) before touching pawn state — this repo's `Players.IsValid` / `IsPlayerValid` helpers exist for exactly this. Enumerate connected players with `Utilities.GetPlayers()`.
- **ConVars**: `ConVar.Find("name")` returns the ConVar or `null`; primitives via `GetPrimitiveValue<T>()`/`SetValue(...)`, strings via `.StringValue`. `helpers/settings.cs` wraps this to snapshot/restore server ConVars around a round so global settings (e.g. `sv_gravity`) don't leak between events.
- **Timers**: `AddTimer(seconds, callback, TimerFlags.REPEAT)` for repeating effects (e.g. toxic smoke damage ticks, screen shake pulses); a plain one-shot `AddTimer(seconds, callback)` for delayed one-off actions. Timers must be tracked and stopped/nulled out on round reset — don't let round-scoped timers outlive the round they belong to.
- **Global listeners** (entity spawn, tick, etc.) use `RegisterListener<Listeners.OnEntitySpawned>(...)` from `Load()` — there's no attribute form for these.

The `cs_script`/VScript function list at source2.wiki is a **different, unrelated scripting system** (Valve's native TypeScript-based VScript for `point_script` entities) — it does not apply to CounterStrikeSharp C# plugins and should not be used as an API reference here.
