using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RandomRoundEvents;

internal sealed class RainbowSmokes
{
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
        float hue = _plugin.Random.NextSingle() * 360.0f;
        float saturation = 0.75f + (_plugin.Random.NextSingle() * 0.25f);
        float value = 0.85f + (_plugin.Random.NextSingle() * 0.15f);
        return HsvToRgb(hue, saturation, value);
    }

    private static Vector HsvToRgb(float hue, float saturation, float value)
    {
        float chroma = value * saturation;
        float huePrime = hue / 60.0f;
        float x = chroma * (1.0f - Math.Abs((huePrime % 2.0f) - 1.0f));

        float r1;
        float g1;
        float b1;

        if (huePrime < 1.0f)
        {
            r1 = chroma;
            g1 = x;
            b1 = 0.0f;
        }
        else if (huePrime < 2.0f)
        {
            r1 = x;
            g1 = chroma;
            b1 = 0.0f;
        }
        else if (huePrime < 3.0f)
        {
            r1 = 0.0f;
            g1 = chroma;
            b1 = x;
        }
        else if (huePrime < 4.0f)
        {
            r1 = 0.0f;
            g1 = x;
            b1 = chroma;
        }
        else if (huePrime < 5.0f)
        {
            r1 = x;
            g1 = 0.0f;
            b1 = chroma;
        }
        else
        {
            r1 = chroma;
            g1 = 0.0f;
            b1 = x;
        }

        float match = value - chroma;
        return new Vector(
            (r1 + match) * 255.0f,
            (g1 + match) * 255.0f,
            (b1 + match) * 255.0f);
    }
}
