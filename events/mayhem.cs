using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;

namespace RandomRoundEvents;

internal sealed class Mayhem
{
    private readonly RandomRoundEvents _plugin;
    private readonly GrenadeRoulette _grenadeRoulette;

    public Mayhem(RandomRoundEvents plugin, GrenadeRoulette grenadeRoulette)
    {
        _plugin = plugin;
        _grenadeRoulette = grenadeRoulette;
    }

    public bool DoubleDamageActive { get; private set; }
    public bool VampireActive { get; private set; }
    public bool ReturnToSenderActive { get; private set; }

    public void Apply()
    {
        Reset();

        var recipe = new List<string>();
        var mayhemBlocklist = new HashSet<string>(_plugin.Config.MayhemRoundBlocklist ?? [], StringComparer.OrdinalIgnoreCase);

        bool IsBlocked(params string[] keys) => keys.Any(mayhemBlocklist.Contains);

        _plugin.StripAllWeapons();
        _plugin.GiveAllPlayersKnives();

        var loadouts = new List<(string Name, string Tag, Action Apply)>
        {
            ("Random sidearms", "guns", () => _plugin.GiveAllPlayersPistols()),
            ("Juan Deags", "guns", () => RandomRoundEvents.GiveAllPlayersDeagle()),
            ("Scout + Zeus", "guns", () =>
            {
                _plugin.GiveAllPlayersScout();
                _plugin.SetConVar("mp_weapons_allow_zeus", -1);
                _plugin.SetConVar("mp_taser_recharge_time", _plugin.Config.ZeusRechargeTime);
                _plugin.GiveAllPlayersZeus();
            }),
            ("Shotguns", "guns", () => _plugin.GiveAllPlayersShotgun()),
            ("Random weapons", "guns", () => _plugin.GiveAllPlayersRandomWeapons()),
            ("Default pistols", "guns", () => RandomRoundEvents.GiveAllPlayersDefaultPistols()),
            ("Grenade roulette", "grenades", () =>
            {
                _grenadeRoulette.EnableMayhemModifier();
                _plugin.GiveAllPlayersGrenadeRouletteGrenades();
            })
        };

        loadouts = loadouts.Where(loadout => loadout.Name switch
        {
            "Random sidearms" => !IsBlocked("RandomSidearms"),
            "Juan Deags" => !IsBlocked("HeadshotOnly", "JuanDeag"),
            "Scout + Zeus" => !IsBlocked("ZeusOnly", "ScoutZeus"),
            "Shotguns" => !IsBlocked("TankRound", "Shotguns"),
            "Random weapons" => !IsBlocked("RandomWeapon"),
            "Default pistols" => !IsBlocked("DefaultPistols"),
            "Grenade roulette" => !IsBlocked("GrenadeRouletteRound", "GrenadeRoulette", "WeirdGrenadesRound", "WeirdNades"),
            _ => true
        }).ToList();

        if (loadouts.Count == 0)
        {
            _plugin.LogMayhemWarning("[RandomRoundEvents] Mayhem blocklist removed every loadout option. Falling back to Default pistols.");
            loadouts.Add(("Default pistols", "guns", () => RandomRoundEvents.GiveAllPlayersDefaultPistols()));
        }

        var loadout = loadouts[_plugin.Random.Next(loadouts.Count)];
        loadout.Apply();
        recipe.Add(loadout.Name);

        var worldMods = new List<(string Name, Action Apply)>
        {
            ("Low gravity", () =>
            {
                _plugin.SetGravity(_plugin.Config.LowGravityValue);
                _plugin.StartGravityMonitor();
            }),
            ("Gravity switching", () =>
            {
                _plugin.StartGravitySwitch();
                _plugin.StartGravityMonitor();
            }),
            ("Random speed", _plugin.RandomizeAllPlayersSpeed),
            ("Random size", _plugin.RandomizeAllPlayersSizes),
            ("Bunnyhopping", () => _plugin.SetBhop(true))
        };

        worldMods = worldMods.Where(mod => mod.Name switch
        {
            "Low gravity" => !IsBlocked("LowGravity"),
            "Gravity switching" => !IsBlocked("GravitySwitch"),
            "Random speed" => !IsBlocked("SpeedRandomizer"),
            "Random size" => !IsBlocked("SizeRound"),
            "Bunnyhopping" => !IsBlocked("KnifeOnly", "ZeusOnly", "Bunnyhopping"),
            _ => true
        }).ToList();

        if (worldMods.Count == 0)
        {
            _plugin.LogMayhemWarning("[RandomRoundEvents] Mayhem blocklist removed every world modifier. Falling back to Random speed.");
            worldMods.Add(("Random speed", _plugin.RandomizeAllPlayersSpeed));
        }

        var worldMod = worldMods[_plugin.Random.Next(worldMods.Count)];
        worldMod.Apply();
        recipe.Add(worldMod.Name);

        var combatMods = new List<(string Name, bool Allowed, Action Apply)>
        {
            ($"{_plugin.Config.DoubleDamageMultiplier}x damage", true, () =>
            {
                DoubleDamageActive = true;
                _plugin.EnsurePlayerHurtHandler();
            }),
            ("Vampire healing", true, () =>
            {
                VampireActive = true;
                _plugin.EnsurePlayerHurtHandler();
            }),
            ("Return to sender", true, () =>
            {
                ReturnToSenderActive = true;
                _plugin.EnsurePlayerHurtHandler();
            }),
            ("No reload", loadout.Tag != "grenades", () =>
            {
                _plugin.EnsureItemPickupHandler();
                _plugin.ApplyNoReload();
                _plugin.StartNoReloadTimer();
            }),
            ("Perfect accuracy", loadout.Tag != "grenades", () => _plugin.SetNospread(true)),
            ("Bonus HE", true, () =>
            {
                _plugin.GiveAllPlayersUnlimitedHE();
                _plugin.StartHERefillTimer();
            })
        };

        var availableCombatMods = combatMods.Where(mod => mod.Allowed).Where(mod => mod.Name switch
        {
            var name when name == $"{_plugin.Config.DoubleDamageMultiplier}x damage" => !IsBlocked("DoubleDamage"),
            "Vampire healing" => !IsBlocked("VampireRound"),
            "Return to sender" => !IsBlocked("ReturnToSenderRound"),
            "No reload" => !IsBlocked("NoReload"),
            "Perfect accuracy" => !IsBlocked("PerfectAccuracy"),
            "Bonus HE" => !IsBlocked("PowerUpRound", "BonusHE"),
            _ => true
        }).ToList();

        if (availableCombatMods.Count == 0)
        {
            _plugin.LogMayhemWarning("[RandomRoundEvents] Mayhem blocklist removed every combat modifier. Falling back to Double Damage.");
            availableCombatMods.Add(($"{_plugin.Config.DoubleDamageMultiplier}x damage", true, () =>
            {
                DoubleDamageActive = true;
                _plugin.EnsurePlayerHurtHandler();
            }));
        }

        var combatMod = availableCombatMods[_plugin.Random.Next(availableCombatMods.Count)];
        combatMod.Apply();
        recipe.Add(combatMod.Name);

        var infoMods = new List<(string Name, Action Apply)>
        {
            ("Tunnel vision", _plugin.RandomizeAllPlayersFOV),
            ("Wall glow", _plugin.ApplyGlowToAllPlayers)
        };

        infoMods = infoMods.Where(mod => mod.Name switch
        {
            "Tunnel vision" => !IsBlocked("ZoomRound"),
            "Wall glow" => !IsBlocked("GlowRound"),
            _ => true
        }).ToList();

        if (infoMods.Count == 0)
        {
            _plugin.LogMayhemWarning("[RandomRoundEvents] Mayhem blocklist removed every info modifier. Falling back to Tunnel vision.");
            infoMods.Add(("Tunnel vision", _plugin.RandomizeAllPlayersFOV));
        }

        var infoMod = infoMods[_plugin.Random.Next(infoMods.Count)];
        infoMod.Apply();
        recipe.Add(infoMod.Name);

        string desc = string.Join(", ", recipe);
        _plugin.LogMayhemInfo("[RandomRoundEvents] Mayhem recipe: {Recipe}", desc);
        Server.PrintToChatAll($" {ChatColors.Red}[MAYHEM ROUND]{ChatColors.White} Buckle up!");
        Server.PrintToChatAll($" {ChatColors.Grey}» {desc}");
    }

    public void Reset()
    {
        DoubleDamageActive = false;
        VampireActive = false;
        ReturnToSenderActive = false;
        _grenadeRoulette.Reset();
    }
}
