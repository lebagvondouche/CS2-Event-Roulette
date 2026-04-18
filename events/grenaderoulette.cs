using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RandomRoundEvents;

internal sealed class GrenadeRoulette
{
    private static readonly IReadOnlyList<string> SupportedProjectileNames = new List<string>
    {
        "flashbang_projectile",
        "smokegrenade_projectile",
        "hegrenade_projectile",
        "decoy_projectile",
        "molotov_projectile",
        "incgrenade_projectile"
    }.AsReadOnly();

    private readonly RandomRoundEvents _plugin;
    private bool _listenerRegistered;
    private bool _mayhemModifierActive;

    public GrenadeRoulette(RandomRoundEvents plugin)
    {
        _plugin = plugin;
    }

    public bool IsMayhemModifierActive => _mayhemModifierActive;

    public void Apply()
    {
        _plugin.ShowEvent("Grenade Roulette Round", "Normal buy round, but HE, flash, smoke, decoy, molotov, and incendiary have random detonation times!");
        _plugin.GiveAllPlayersStandardLoadout();
        _plugin.GiveAllPlayersGrenadeRouletteGrenades();
        EnsureListenerRegistered();
    }

    public void EnableMayhemModifier()
    {
        _mayhemModifierActive = true;
        EnsureListenerRegistered();
    }

    public void Reset()
    {
        _mayhemModifierActive = false;

        if (_listenerRegistered)
        {
            _plugin.RemoveListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
            _listenerRegistered = false;
        }
    }

    public void OnEntitySpawned(CEntityInstance entity)
    {
        if ((!_plugin.IsGrenadeRouletteRoundActive && !_mayhemModifierActive) ||
            !SupportedProjectileNames.Contains(entity.DesignerName))
        {
            return;
        }

        var grenade = entity.As<CBaseCSGrenadeProjectile>();
        Server.NextFrame(() =>
        {
            if (!grenade.IsValid)
                return;

            float min = _plugin.Config.WeirdGrenadeMinTime;
            float max = _plugin.Config.WeirdGrenadeMaxTime;
            float offset = (float)(_plugin.Random.NextDouble() * (max - min) + min);
            grenade.DetonateTime = Server.CurrentTime + offset;
            Utilities.SetStateChanged(grenade, "CBaseGrenade", "m_flDetonateTime");
        });
    }

    private void EnsureListenerRegistered()
    {
        if (_listenerRegistered)
            return;

        _plugin.RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        _listenerRegistered = true;
    }
}
