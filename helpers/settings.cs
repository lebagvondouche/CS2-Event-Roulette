using CounterStrikeSharp.API.Modules.Cvars;
using System.Globalization;

namespace RandomRoundEvents;

internal sealed class Settings
{
    private static readonly string[] ManagedConVars =
    {
        "sv_gravity",
        "weapon_accuracy_nospread",
        "mp_respawn_on_death_t",
        "mp_respawn_on_death_ct",
        "mp_respawnwavetime_ct",
        "mp_respawnwavetime_t",
        "mp_randomspawn",
        "mp_randomspawn_los",
        "mp_buytime",
        "mp_taser_recharge_time",
        "mp_friendlyfire",
        "sv_infinite_ammo",
        "mp_death_drop_gun",
        "mp_buy_allow_guns",
        "mp_buy_allow_grenades",
        "mp_free_armor",
        "mp_max_armor",
        "mp_buy_anywhere",
        "mp_c4timer",
        "mp_weapons_allow_zeus",
        "sv_autobunnyhopping",
        "sv_enablebunnyhopping"
    };

    private readonly RandomRoundEvents _plugin;
    private readonly Dictionary<string, string> _savedConVars = new(StringComparer.OrdinalIgnoreCase);

    public Settings(RandomRoundEvents plugin)
    {
        _plugin = plugin;
    }

    internal void CaptureManagedConVars()
    {
        foreach (var name in ManagedConVars)
        {
            if (_savedConVars.ContainsKey(name))
                continue;

            var conVar = ConVar.Find(name);
            if (conVar != null)
                _savedConVars[name] = conVar.StringValue;
        }
    }

    internal void RestoreManagedConVars()
    {
        foreach (var entry in _savedConVars)
        {
            var conVar = ConVar.Find(entry.Key);
            if (conVar == null)
                continue;

            try
            {
                conVar.StringValue = entry.Value;
            }
            catch (Exception ex)
            {
                _plugin.LogPluginWarning("[RandomRoundEvents] Failed to restore {ConVar}: {Error}", entry.Key, ex.Message);
            }
        }
    }

    internal void SetConVar(string name, int value) =>
        SetConVar(name, value.ToString(CultureInfo.InvariantCulture));

    internal void SetConVar(string name, float value) =>
        SetConVar(name, value.ToString(CultureInfo.InvariantCulture));

    internal void SetConVar(string name, string value)
    {
        var conVar = ConVar.Find(name);
        if (conVar == null)
        {
            _plugin.LogPluginWarning("[RandomRoundEvents] {ConVar} not found.", name);
            return;
        }

        try
        {
            conVar.StringValue = value;
        }
        catch (Exception ex)
        {
            _plugin.LogPluginWarning("[RandomRoundEvents] Failed to set {ConVar}: {Error}", name, ex.Message);
        }
    }

    internal void SetNospread(bool enabled)
    {
        SetConVar("weapon_accuracy_nospread", enabled ? 1 : 0);
    }

    internal void SetBhop(bool enabled)
    {
        SetConVar("sv_autobunnyhopping", enabled ? 1 : 0);
        SetConVar("sv_enablebunnyhopping", enabled ? 1 : 0);
    }

    internal void EnableBuying()
    {
        SetConVar("mp_buy_allow_guns", 255);
        SetConVar("mp_buy_allow_grenades", 1);
        SetConVar("mp_max_armor", 2);
        SetConVar("mp_buy_anywhere", 0);
        SetConVar("mp_weapons_allow_zeus", -1);
    }

    internal void DisableBuying()
    {
        SetConVar("mp_buy_allow_guns", 0);
        SetConVar("mp_buy_allow_grenades", 0);
        SetConVar("mp_free_armor", 0);
        SetConVar("mp_max_armor", 0);
        SetConVar("mp_buy_anywhere", 0);
        SetConVar("mp_weapons_allow_zeus", 0);
    }
}
