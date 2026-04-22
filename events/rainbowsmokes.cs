using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RandomRoundEvents;

internal sealed class RainbowSmokes
{
    private static readonly IReadOnlyList<Vector> SmokePalette =
    [
        new Vector(255.0f, 0.0f, 255.0f),   // magenta
        new Vector(0.0f, 255.0f, 255.0f),   // cyan
        new Vector(64.0f, 96.0f, 255.0f),   // electric blue
        new Vector(128.0f, 0.0f, 255.0f),   // purple
        new Vector(255.0f, 32.0f, 128.0f),  // hot pink
        new Vector(0.0f, 255.0f, 128.0f),   // aqua green
        new Vector(64.0f, 255.0f, 64.0f),   // neon green
        new Vector(255.0f, 0.0f, 96.0f)     // punchy red-pink
    ];

    private readonly RandomRoundEvents _plugin;
    private bool _listenerRegistered;

    public RainbowSmokes(RandomRoundEvents plugin)
    {
        _plugin = plugin;
    }

    public void Apply()
    {
        _plugin.ShowEvent("Rainbow Smokes Round", "Normal buy round, but every smoke blooms in a random color!");
        _plugin.GiveAllPlayersStandardLoadout();
        _plugin.GiveAllPlayersSmokes();
        EnsureListenerRegistered();
    }

    public void Reset()
    {
        if (!_listenerRegistered)
            return;

        _plugin.RemoveListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        _listenerRegistered = false;
    }

    private void EnsureListenerRegistered()
    {
        if (_listenerRegistered)
            return;

        _plugin.RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        _listenerRegistered = true;
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (!_plugin.IsRainbowSmokesRoundActive || entity.DesignerName != "smokegrenade_projectile")
            return;

        var projectile = new CSmokeGrenadeProjectile(entity.Handle);
        Server.NextFrame(() =>
        {
            if (!projectile.IsValid)
            {
                if (_plugin.Config.Debug)
                    _plugin.LogRainbowSmokesWarning("[RandomRoundEvents] Rainbow Smokes projectile became invalid before color could be applied.");
                return;
            }

            var color = CreateVibrantSmokeColor();
            projectile.SmokeColor.X = color.X;
            projectile.SmokeColor.Y = color.Y;
            projectile.SmokeColor.Z = color.Z;

            if (_plugin.Config.Debug)
            {
                _plugin.LogRainbowSmokesInfo(
                    "[RandomRoundEvents] Rainbow Smokes applied color ({R:F0}, {G:F0}, {B:F0})",
                    color.X,
                    color.Y,
                    color.Z);
            }
        });
    }

    private Vector CreateVibrantSmokeColor()
    {
        return SmokePalette[_plugin.Random.Next(SmokePalette.Count)];
    }
}
