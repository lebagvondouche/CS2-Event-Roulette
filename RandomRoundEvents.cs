using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Memory;
using System.Drawing;
using Microsoft.Extensions.Logging;

namespace RandomRoundEvents;

public class RandomRoundEventsConfig : IBasePluginConfig
{
    public int Version { get; set; } = 1;
    public bool Debug { get; set; } = false;

    // Event toggles
    public bool EnableLowGravity { get; set; } = true;
    public bool EnableHeadshotOnly { get; set; } = true;
    public bool EnableRandomWeapon { get; set; } = true;
    public bool EnableDoubleDamage { get; set; } = true;
    public bool EnableSwapTeams { get; set; } = true;
    public bool EnableFlashbangSpam { get; set; } = true;
    public bool EnableKnifeOnly { get; set; } = true;
    public bool EnableZeusOnly { get; set; } = true;
    public bool EnableNoReload { get; set; } = true;
    public bool EnableGravitySwitch { get; set; } = true;
    public bool EnableSpeedRandomizer { get; set; } = true;
    public bool EnableLastManStanding { get; set; } = true;
    public bool EnablePowerUpRound { get; set; } = true;
    public bool EnableTankRound { get; set; } = true;
    public bool EnableInvisibleRound { get; set; } = true;
    public bool EnableRespawnRound { get; set; } = true;
    public bool EnableVampireRound { get; set; } = true;
    public bool EnableJammerRound { get; set; } = true;
    public bool EnableZoomRound { get; set; } = true;
    public bool EnableFogRound { get; set; } = true;
    public bool EnableGlowRound { get; set; } = true;
    public bool EnableSizeRound { get; set; } = true;
    public bool EnableChickenRound { get; set; } = true;
    public bool EnableReturnToSenderRound { get; set; } = true;
    public bool EnableWeirdGrenadesRound { get; set; } = true;

    // Event settings
    public float LowGravityValue { get; set; } = 400.0f;
    public float GravitySwitchLow { get; set; } = 400.0f;
    public float GravitySwitchHigh { get; set; } = 1200.0f;
    public float GravitySwitchInterval { get; set; } = 5.0f;
    public float SpeedMin { get; set; } = 0.5f;
    public float SpeedMax { get; set; } = 2.0f;
    public int SwapInterval { get; set; } = 30;
    public int FlashbangStartHP { get; set; } = 1;
    public float FlashbangRefillInterval { get; set; } = 3.0f;
    public int PowerUpHP { get; set; } = 300;
    public int DoubleDamageMultiplier { get; set; } = 2;
    public int ZeusRechargeTime { get; set; } = 5;
    public bool EnableBomb { get; set; } = false;
    public int TankHP { get; set; } = 500;
    public int RespawnPool { get; set; } = 10;
    public int VampireMaxHP { get; set; } = 300;
    public uint ZoomMinFOV { get; set; } = 30;
    public uint ZoomMaxFOV { get; set; } = 70;
    public float FogDensity { get; set; } = 0.99f;
    public float FogEndDistance { get; set; } = 600.0f;
    public float SizeMin { get; set; } = 0.5f;
    public float SizeMax { get; set; } = 2.0f;
    public int ChickenCount { get; set; } = 5;
    public float ChickenSize { get; set; } = 2.0f;
    public float WeirdGrenadeMinTime { get; set; } = 0.1f;
    public float WeirdGrenadeMaxTime { get; set; } = 5.0f;

    // Chaos round
    public int ChaosRoundChance { get; set; } = 15; // percentage chance (0-100)
}

public class RandomRoundEvents : BasePlugin, IPluginConfig<RandomRoundEventsConfig>
{
    public override string ModuleName => "RandomRoundEvents";
    public override string ModuleVersion => "0.5";
    public override string ModuleAuthor => "Martin Persson";
    public override string ModuleDescription => "A plugin that triggers random events during rounds.";

    public RandomRoundEventsConfig Config { get; set; } = new RandomRoundEventsConfig();

    private readonly Random _random = new();
    private CounterStrikeSharp.API.Modules.Timers.Timer? _gravitySwitchTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _flashbangSpamTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _gravityMonitorTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _speedEnforceTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _swapTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _heRefillTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _noReloadTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _ammoRefillTimer;
    private readonly Dictionary<int, float> _playerSpeeds = new();
    private readonly List<CFogController> _fogEntities = new();
    private readonly List<CDynamicProp> _glowEntities = new();
    private bool _isLoaded = false;
    private bool _roundEventTriggered = false;
    private float _currentGravity = 800.0f;

    private static readonly IReadOnlyList<string> RandomWeapons = new List<string>
    {
        "weapon_ak47", "weapon_m4a1", "weapon_awp", "weapon_ssg08", "weapon_mp5sd", "weapon_ump45", "weapon_deagle", "weapon_glock"
    }.AsReadOnly();

    private static readonly IReadOnlyList<string> Pistols = new List<string>
    {
        "weapon_deagle", "weapon_glock", "weapon_p250", "weapon_usp_silencer", "weapon_fiveseven"
    }.AsReadOnly();

    private enum EventType
    {
        None,
        LowGravity,
        HeadshotOnly,
        RandomWeapon,
        DoubleDamage,
        SwapTeams,
        FlashbangSpam,
        KnifeOnly,
        ZeusOnly,
        NoReload,
        GravitySwitch,
        SpeedRandomizer,
        LastManStanding,
        PowerUpRound,
        TankRound,
        InvisibleRound,
        RespawnRound,
        ChaosRound,
        VampireRound,
        JammerRound,
        ZoomRound,
        FogRound,
        GlowRound,
        SizeRound,
        ChickenRound,
        ReturnToSenderRound,
        WeirdGrenadesRound
    }

    private EventType _activeEvent = EventType.None;
    private EventType _forcedEvent = EventType.None;
    private bool _chaosDoubleDamage = false;
    private bool _hurtHandlerRegistered = false;
    private bool _itemPickupHandlerRegistered = false;
    private bool _deathHandlerRegistered = false;
    private bool _spawnHandlerRegistered = false;
    private bool _entityListenerRegistered = false;
    private int _tRespawns = 0;
    private int _ctRespawns = 0;

    public void OnConfigParsed(RandomRoundEventsConfig config)
    {
        Config = config;

        // Validate config values
        Config.LowGravityValue = Math.Clamp(Config.LowGravityValue, 50.0f, 800.0f);
        Config.GravitySwitchLow = Math.Clamp(Config.GravitySwitchLow, 50.0f, 800.0f);
        Config.GravitySwitchHigh = Math.Clamp(Config.GravitySwitchHigh, 800.0f, 2000.0f);
        Config.GravitySwitchInterval = Math.Clamp(Config.GravitySwitchInterval, 1.0f, 60.0f);
        Config.SpeedMin = Math.Clamp(Config.SpeedMin, 0.1f, 3.0f);
        Config.SpeedMax = Math.Clamp(Config.SpeedMax, Config.SpeedMin, 5.0f);
        Config.SwapInterval = Math.Clamp(Config.SwapInterval, 5, 120);
        Config.FlashbangStartHP = Math.Clamp(Config.FlashbangStartHP, 1, 100);
        Config.FlashbangRefillInterval = Math.Clamp(Config.FlashbangRefillInterval, 1.0f, 30.0f);
        Config.PowerUpHP = Math.Clamp(Config.PowerUpHP, 100, 1000);
        Config.DoubleDamageMultiplier = Math.Clamp(Config.DoubleDamageMultiplier, 2, 10);
        Config.ZeusRechargeTime = Math.Clamp(Config.ZeusRechargeTime, 0, 30);
        Config.TankHP = Math.Clamp(Config.TankHP, 200, 1000);
        Config.RespawnPool = Math.Clamp(Config.RespawnPool, 1, 50);
        Config.VampireMaxHP = Math.Clamp(Config.VampireMaxHP, 100, 1000);
        Config.ChaosRoundChance = Math.Clamp(Config.ChaosRoundChance, 0, 100);

        Logger.LogInformation("[RandomRoundEvents] Configuration loaded.");
        if (Config.Debug)
            Logger.LogInformation("[RandomRoundEvents] Debug mode is enabled.");
    }

    public override void Load(bool hotReload)
    {
        if (_isLoaded)
        {
            Logger.LogWarning("[RandomRoundEvents] Plugin already loaded. Skipping duplicate load.");
            return;
        }

        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);

        AddCommand("css_rre_lowgravity", "Trigger Low Gravity event", OnLowGravityCommand);
        AddCommand("css_rre_headshotonly", "Trigger Headshot Only event", OnHeadshotOnlyCommand);
        AddCommand("css_rre_randomweapon", "Trigger Random Weapon event", OnRandomWeaponCommand);
        AddCommand("css_rre_doubledamage", "Trigger Double Damage event", OnDoubleDamageCommand);
        AddCommand("css_rre_swapteams", "Trigger Swap Teams event", OnSwapTeamsCommand);
        AddCommand("css_rre_flashbang", "Trigger Flashbang Spam event", OnFlashbangSpamCommand);
        AddCommand("css_rre_knife", "Trigger Knife-Only event", OnKnifeOnlyCommand);
        AddCommand("css_rre_zeus", "Trigger Zeus-Only event", OnZeusOnlyCommand);
        AddCommand("css_rre_noreload", "Trigger No Reload event", OnNoReloadCommand);
        AddCommand("css_rre_gravityswitch", "Trigger Gravity Switch event", OnGravitySwitchCommand);
        AddCommand("css_rre_speed", "Trigger Speed Randomizer event", OnSpeedRandomizerCommand);
        AddCommand("css_rre_lastman", "Trigger Last Man Standing event", OnLastManStandingCommand);
        AddCommand("css_rre_powerup", "Trigger Power-Up Round event", OnPowerUpRoundCommand);
        AddCommand("css_rre_tank", "Trigger Tank Round event", OnTankRoundCommand);
        AddCommand("css_rre_invisible", "Trigger Invisible Round event", OnInvisibleRoundCommand);
        AddCommand("css_rre_respawn", "Trigger Respawn Round event", OnRespawnRoundCommand);
        AddCommand("css_rre_vampire", "Trigger Vampire Round event", OnVampireRoundCommand);
        AddCommand("css_rre_jammer", "Trigger Jammer Round event", OnJammerRoundCommand);
        AddCommand("css_rre_zoom", "Trigger Zoom Round event", OnZoomRoundCommand);
        AddCommand("css_rre_fog", "Trigger Fog of War Round event", OnFogRoundCommand);
        AddCommand("css_rre_glow", "Trigger Glow Round event", OnGlowRoundCommand);
        AddCommand("css_rre_size", "Trigger Size Randomizer Round event", OnSizeRoundCommand);
        AddCommand("css_rre_chicken", "Trigger Chicken Leader Round event", OnChickenRoundCommand);
        AddCommand("css_rre_return", "Trigger Return to Sender Round event", OnReturnToSenderCommand);
        AddCommand("css_rre_weirdnades", "Trigger Weird Grenades Round event", OnWeirdGrenadesCommand);
        AddCommand("css_rre_reset", "Reset all events", OnResetCommand);
        AddCommand("css_rre_menu", "Open event selection menu", OnMenuCommand);
        AddCommand("css_rre_chaos", "Trigger Chaos Round", OnChaosRoundCommand);

        _isLoaded = true;
        Logger.LogInformation("[RandomRoundEvents] Plugin loaded successfully.");
    }

    public override void Unload(bool hotReload)
    {
        ResetAllState();
        _isLoaded = false;
        _roundEventTriggered = false;
        base.Unload(hotReload);
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Skip events during warmup (unless admin forced an event)
        if (_forcedEvent == EventType.None)
        {
            var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            if (gameRules?.GameRules?.WarmupPeriod == true)
                return HookResult.Continue;
        }

        if (_roundEventTriggered)
            return HookResult.Continue;

        var enabledEvents = new List<EventType>();
        if (Config.EnableLowGravity) enabledEvents.Add(EventType.LowGravity);
        if (Config.EnableHeadshotOnly) enabledEvents.Add(EventType.HeadshotOnly);
        if (Config.EnableRandomWeapon) enabledEvents.Add(EventType.RandomWeapon);
        if (Config.EnableDoubleDamage) enabledEvents.Add(EventType.DoubleDamage);
        if (Config.EnableSwapTeams) enabledEvents.Add(EventType.SwapTeams);
        if (Config.EnableFlashbangSpam) enabledEvents.Add(EventType.FlashbangSpam);
        if (Config.EnableKnifeOnly) enabledEvents.Add(EventType.KnifeOnly);
        if (Config.EnableZeusOnly) enabledEvents.Add(EventType.ZeusOnly);
        if (Config.EnableNoReload) enabledEvents.Add(EventType.NoReload);
        if (Config.EnableGravitySwitch) enabledEvents.Add(EventType.GravitySwitch);
        if (Config.EnableSpeedRandomizer) enabledEvents.Add(EventType.SpeedRandomizer);
        if (Config.EnableLastManStanding) enabledEvents.Add(EventType.LastManStanding);
        if (Config.EnablePowerUpRound) enabledEvents.Add(EventType.PowerUpRound);
        if (Config.EnableTankRound) enabledEvents.Add(EventType.TankRound);
        if (Config.EnableInvisibleRound) enabledEvents.Add(EventType.InvisibleRound);
        if (Config.EnableRespawnRound) enabledEvents.Add(EventType.RespawnRound);
        if (Config.EnableVampireRound) enabledEvents.Add(EventType.VampireRound);
        if (Config.EnableJammerRound) enabledEvents.Add(EventType.JammerRound);
        if (Config.EnableZoomRound) enabledEvents.Add(EventType.ZoomRound);
        if (Config.EnableFogRound) enabledEvents.Add(EventType.FogRound);
        if (Config.EnableGlowRound) enabledEvents.Add(EventType.GlowRound);
        if (Config.EnableSizeRound) enabledEvents.Add(EventType.SizeRound);
        if (Config.EnableChickenRound) enabledEvents.Add(EventType.ChickenRound);
        if (Config.EnableReturnToSenderRound) enabledEvents.Add(EventType.ReturnToSenderRound);
        if (Config.EnableWeirdGrenadesRound) enabledEvents.Add(EventType.WeirdGrenadesRound);

        if (enabledEvents.Count == 0)
        {
            Logger.LogWarning("[RandomRoundEvents] No events enabled in configuration.");
            return HookResult.Continue;
        }

        // Use forced event if set by admin command, otherwise pick randomly
        EventType selectedEvent;
        if (_forcedEvent != EventType.None)
        {
            selectedEvent = _forcedEvent;
            // Don't clear _forcedEvent yet — mp_restartgame can fire multiple cycles
        }
        else if (Config.ChaosRoundChance > 0 && _random.Next(0, 100) < Config.ChaosRoundChance)
            selectedEvent = EventType.ChaosRound;
        else
            selectedEvent = enabledEvents[_random.Next(0, enabledEvents.Count)];

        Logger.LogInformation("[RandomRoundEvents] Selected event: {Event}", selectedEvent);
        ResetAllState();  // resets based on previous _activeEvent, then sets it to None
        _activeEvent = selectedEvent;

        // Enable buying only for events that allow it
        if (selectedEvent == EventType.SwapTeams || selectedEvent == EventType.SpeedRandomizer || selectedEvent == EventType.GravitySwitch || selectedEvent == EventType.NoReload || selectedEvent == EventType.RespawnRound)
            EnableBuying();
        else
            DisableBuying();

        switch (selectedEvent)
        {
            case EventType.LowGravity:
                AnnounceEvent("Low Gravity Round", "Float around with a Scout and Zeus. Perfect accuracy!");
                SetGravity(Config.LowGravityValue);
                SetNospread(true);
                StartGravityMonitor();
                Server.ExecuteCommand($"mp_taser_recharge_time {Config.ZeusRechargeTime}");
                StripAllWeapons(); GiveAllPlayersScout(); GiveAllPlayersZeus();
                break;
            case EventType.HeadshotOnly:
                AnnounceEvent("Juan Deag Round", "Deagle only, headshots only. One tap or nothing!");
                RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post); _hurtHandlerRegistered = true;
                StripAllWeapons(); GiveAllPlayersKnives(); GiveAllPlayersDeagle();
                var svInfiniteAmmo = ConVar.Find("sv_infinite_ammo");
                if (svInfiniteAmmo != null) svInfiniteAmmo.SetValue(1);
                break;
            case EventType.RandomWeapon:
                AnnounceEvent("Random Weapon Round", "Everyone gets a random weapon. Good luck!");
                Server.ExecuteCommand("mp_death_drop_gun 1");
                StripAllWeapons(); GiveAllPlayersKnives(); GiveAllPlayersRandomWeapons();
                break;
            case EventType.DoubleDamage:
                AnnounceEvent("Double Damage Round", "All damage is doubled. Play it safe!");
                RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post); _hurtHandlerRegistered = true;
                StripAllWeapons(); GiveAllPlayersGlock();
                break;
            case EventType.SwapTeams:
                AnnounceEvent("Team Swap Round", "A random pair swaps teams every 30 seconds!");
                StartSwapTimer();
                break;
            case EventType.FlashbangSpam:
                AnnounceEvent("Flashbang Spam Round", "1 HP, flashbangs only. Knife does no damage!");
                StartFlashbangSpamRound();
                RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post); _hurtHandlerRegistered = true;
                StripAllWeapons(); GiveAllPlayersKnives();
                // Remove armor so knife heal-back works correctly at 1 HP
                foreach (var player in Utilities.GetPlayers())
                {
                    if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
                    var pawn = player.PlayerPawn.Value;
                    pawn.ArmorValue = 0;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                }
                SetAllPlayersHealth(Config.FlashbangStartHP); GiveAllPlayersFlashbangs();
                break;
            case EventType.KnifeOnly:
                AnnounceEvent("Knife-Only Round", "Knives out! Bhop enabled!");
                SetBhop(true);
                StripAllWeapons(); GiveAllPlayersKnives();
                break;
            case EventType.ZeusOnly:
                AnnounceEvent("Zeus-Only Round", "Zeus only. Bhop enabled!");
                SetBhop(true);
                Server.ExecuteCommand($"mp_taser_recharge_time {Config.ZeusRechargeTime}");
                StripAllWeapons(); GiveAllPlayersZeus();
                break;
            case EventType.NoReload:
                AnnounceEvent("No Reload Round", "One magazine only. Make every bullet count!");
                RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Post); _itemPickupHandlerRegistered = true;
                ApplyNoReload();
                StartNoReloadTimer();
                break;
            case EventType.GravitySwitch:
                AnnounceEvent("Gravity Switch Round", "Gravity flips between low and high every 5 seconds!");
                StartGravitySwitch();
                StartGravityMonitor();
                break;
            case EventType.SpeedRandomizer:
                AnnounceEvent("Speed Randomizer Round", "Everyone moves at a different random speed!");
                GiveAllPlayersKnives(); GiveAllPlayersGlock(); RandomizeAllPlayersSpeed();
                break;
            case EventType.LastManStanding:
                AnnounceEvent("Last Man Standing Round", "Random pistol only. Survive!");
                StripAllWeapons(); GiveAllPlayersKnives(); GiveAllPlayersPistols();
                break;
            case EventType.PowerUpRound:
                AnnounceEvent("Power-Up Round", $"{Config.PowerUpHP} HP, full armor, unlimited HE. Knife does no damage!");
                Server.ExecuteCommand("mp_friendlyfire 0");
                StartHERefillTimer();
                RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post); _hurtHandlerRegistered = true;
                StripAllWeapons(); GiveAllPlayersKnives(); SetAllPlayersHealth(Config.PowerUpHP); GiveAllPlayersFullArmor(); GiveAllPlayersUnlimitedHE(); GiveAllPlayersMolotov();
                break;
            case EventType.TankRound:
                AnnounceEvent("Tank Round", $"{Config.TankHP} HP, full armor, shotguns only. Unlimited ammo!");
                StripAllWeapons(); GiveAllPlayersKnives();
                SetAllPlayersHealth(Config.TankHP);
                GiveAllPlayersFullArmor();
                GiveAllPlayersShotgun();
                StartAmmoRefillTimer();
                break;
            case EventType.InvisibleRound:
                AnnounceEvent("Invisible Round", "Everyone is invisible! Knife only, no friendly fire!");
                Server.ExecuteCommand("mp_friendlyfire 0");
                StripAllWeapons(); GiveAllPlayersKnives();
                SetAllPlayersInvisible();
                break;
            case EventType.RespawnRound:
                AnnounceEvent("Respawn Round", $"Each team has {Config.RespawnPool} shared respawns!");
                _tRespawns = Config.RespawnPool;
                _ctRespawns = Config.RespawnPool;
                Server.ExecuteCommand("mp_respawn_on_death_t 1; mp_respawn_on_death_ct 1; mp_respawnwavetime_ct 0; mp_respawnwavetime_t 0; mp_randomspawn 1; mp_randomspawn_los 1; mp_buytime 9999");
                RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post); _deathHandlerRegistered = true;
                RegisterEventHandler<EventPlayerSpawn>(OnRespawnSpawn, HookMode.Post); _spawnHandlerRegistered = true;
                break;
            case EventType.VampireRound:
                AnnounceEvent("Vampire Round", $"Damage dealt heals you! Max {Config.VampireMaxHP} HP. Pistols only!");
                RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post); _hurtHandlerRegistered = true;
                StripAllWeapons(); GiveAllPlayersKnives(); GiveAllPlayersPistols();
                break;
            case EventType.JammerRound:
                AnnounceEvent("Jammer Round", "No HUD! No crosshair, no health, no ammo display!");
                HideAllPlayersHUD();
                break;
            case EventType.ZoomRound:
                AnnounceEvent("Zoom Round", "Tunnel vision! Everyone has a random FOV!");
                RandomizeAllPlayersFOV();
                break;
            case EventType.FogRound:
                AnnounceEvent("Fog of War Round", "Thick fog! Shotguns only, watch your corners!");
                ApplyFog();
                StripAllWeapons(); GiveAllPlayersKnives(); GiveAllPlayersShotgun();
                break;
            case EventType.GlowRound:
                AnnounceEvent("Glow Round", "Everyone glows through walls! No hiding!");
                ApplyGlowToAllPlayers();
                break;
            case EventType.SizeRound:
                AnnounceEvent("Size Randomizer Round", "Everyone is a random size! HP scales with size!");
                RandomizeAllPlayersSizes();
                break;
            case EventType.ChickenRound:
                AnnounceEvent("Chicken Leader Round", "A flock of chickens follows each player!");
                SpawnChickensForAllPlayers();
                break;
            case EventType.ReturnToSenderRound:
                AnnounceEvent("Return to Sender Round", "Hit someone and they teleport back to spawn! Pistols only!");
                RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post); _hurtHandlerRegistered = true;
                StripAllWeapons(); GiveAllPlayersKnives(); GiveAllPlayersPistols();
                break;
            case EventType.WeirdGrenadesRound:
                AnnounceEvent("Weird Grenades Round", "All grenades have random detonation times!");
                RegisterListener<Listeners.OnEntitySpawned>(OnWeirdGrenadeEntitySpawned); _entityListenerRegistered = true;
                break;
            case EventType.ChaosRound:
                ApplyChaosRound();
                break;
        }

        _roundEventTriggered = true;
        _forcedEvent = EventType.None;
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        ResetAllState();
        _roundEventTriggered = false;
        return HookResult.Continue;
    }

    // player_hurt fires AFTER damage is applied in Source 2, so we use Post hook
    // and directly manipulate pawn health for our effects
    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        var victim = @event.Userid;
        var attacker = @event.Attacker;

        if (attacker == null || !IsPlayerValid(attacker)) return HookResult.Continue;
        if (victim == null || !victim.IsValid || victim.PlayerPawn.Value == null) return HookResult.Continue;

        var pawn = victim.PlayerPawn.Value;

        // Heal back knife damage on PowerUp/Flashbang rounds FIRST (before death check)
        if ((_activeEvent == EventType.PowerUpRound || _activeEvent == EventType.FlashbangSpam) && 
            (@event.Weapon.Contains("knife") || @event.Weapon.Contains("bayonet")))
        {
            int dmg = @event.DmgHealth;
            int maxHp = _activeEvent == EventType.PowerUpRound ? Config.PowerUpHP : Config.FlashbangStartHP;
            pawn.Health = Math.Min(pawn.Health + dmg, maxHp);
            // If the engine already killed the player (health was <= 0), revive them
            if (pawn.LifeState != (byte)0)
            {
                pawn.LifeState = (byte)0; // LIFE_ALIVE
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_lifeState");
            }
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            return HookResult.Continue; // Don't process further
        }

        // Skip if player is already dead
        if (pawn.Health <= 0) return HookResult.Continue;

        if (_activeEvent == EventType.HeadshotOnly && @event.Hitgroup != 1)
        {
            // Not a headshot — heal back the damage
            int dmg = @event.DmgHealth;
            pawn.Health = Math.Min(pawn.Health + dmg, 100);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }

        if (_activeEvent == EventType.DoubleDamage || _chaosDoubleDamage)
        {
            int extraDamage = @event.DmgHealth * (Config.DoubleDamageMultiplier - 1);
            int newHealth = pawn.Health - extraDamage;
            if (newHealth > 0)
            {
                pawn.Health = newHealth;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }
            // If newHealth <= 0, don't touch health — let the engine handle death naturally
        }

        if (_activeEvent == EventType.VampireRound && attacker.PlayerPawn.Value != null)
        {
            var attackerPawn = attacker.PlayerPawn.Value;
            int healAmount = @event.DmgHealth;
            attackerPawn.Health = Math.Min(attackerPawn.Health + healAmount, Config.VampireMaxHP);
            attackerPawn.MaxHealth = attackerPawn.Health;
            Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iMaxHealth");
            if (!attacker.IsBot)
                attacker.PrintToCenterAlert($"+{healAmount} HP");
        }

        if (_activeEvent == EventType.ReturnToSenderRound && pawn.Health > 0)
        {
            TeleportToSpawn(victim);
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (_activeEvent != EventType.RespawnRound) return HookResult.Continue;

        var victim = @event.Userid;
        if (victim == null || !victim.IsValid) return HookResult.Continue;

        if (victim.Team == CsTeam.Terrorist)
        {
            _tRespawns--;
            if (_tRespawns <= 0)
            {
                _tRespawns = 0;
                Server.ExecuteCommand("mp_respawn_on_death_t 0");
                Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.Red} T team has no respawns left!");
            }
            else
            {
                Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.White} T respawns remaining: {ChatColors.Green}{_tRespawns}");
            }
        }
        else if (victim.Team == CsTeam.CounterTerrorist)
        {
            _ctRespawns--;
            if (_ctRespawns <= 0)
            {
                _ctRespawns = 0;
                Server.ExecuteCommand("mp_respawn_on_death_ct 0");
                Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.Red} CT team has no respawns left!");
            }
            else
            {
                Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.White} CT respawns remaining: {ChatColors.Green}{_ctRespawns}");
            }
        }

        return HookResult.Continue;
    }

    private HookResult OnRespawnSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (_activeEvent != EventType.RespawnRound) return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid) return HookResult.Continue;

        AddTimer(1.0f, () =>
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive || player.PlayerPawn.Value == null) return;
            string weapon = RandomWeapons[_random.Next(RandomWeapons.Count)];
            try { player.GiveNamedItem(weapon); }
            catch { /* ignore */ }
        });

        return HookResult.Continue;
    }

    private void StartHERefillTimer()
    {
        _heRefillTimer?.Kill();
        _heRefillTimer = AddTimer(1.0f, () =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!IsPlayerValid(player)) continue;
                if (GetPlayerGrenadeCount(player, "weapon_hegrenade") < 1)
                {
                    try { player.GiveNamedItem("weapon_hegrenade"); }
                    catch { /* ignore */ }
                }
                if (GetPlayerGrenadeCount(player, "weapon_molotov") < 1 && GetPlayerGrenadeCount(player, "weapon_incgrenade") < 1)
                {
                    try { player.GiveNamedItem("weapon_molotov"); }
                    catch { /* ignore */ }
                }
            }
        }, TimerFlags.REPEAT);
    }


    private static int GetPlayerGrenadeCount(CCSPlayerController player, string grenadeName)
    {
        if (player.PlayerPawn.Value == null) return 1;
        var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
        if (weapons == null) return 1;
        int count = 0;
        foreach (var weapon in weapons)
        {
            if (weapon.Value != null && weapon.Value.DesignerName == grenadeName)
                count++;
        }
        return count;
    }

    private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !IsPlayerValid(player) || player.PlayerPawn.Value == null) return HookResult.Continue;

        // Strip reserve ammo on any weapon pickup during No Reload round
        AddTimer(0.5f, () =>
        {
            if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) return;
            var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
            if (weapons == null) return;
            foreach (var weaponHandle in weapons)
            {
                var weapon = weaponHandle.Value;
                if (weapon == null) continue;
                weapon.ReserveAmmo[0] = 0;
            }
        });

        return HookResult.Continue;
    }

    private static bool IsPlayerValid(CCSPlayerController player)
    {
        return player.IsValid && player.PawnIsAlive && player.PlayerPawn.Value != null;
    }

    private static bool IsAdmin(CCSPlayerController? player)
    {
        // Server console always has permission
        if (player == null) return true;
        return AdminManager.PlayerHasPermissions(player, "@css/root");
    }

    private void ResetAllState()
    {
        _gravitySwitchTimer?.Kill();
        _flashbangSpamTimer?.Kill();
        _gravityMonitorTimer?.Kill();
        _speedEnforceTimer?.Kill();
        _swapTimer?.Kill();
        _heRefillTimer?.Kill();
        _noReloadTimer?.Kill();
        _ammoRefillTimer?.Kill();
        _playerSpeeds.Clear();

        try
        {
            if (_hurtHandlerRegistered)
            {
                DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post);
                _hurtHandlerRegistered = false;
            }
            if (_itemPickupHandlerRegistered)
            {
                DeregisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Post);
                _itemPickupHandlerRegistered = false;
            }
            if (_deathHandlerRegistered)
            {
                DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
                _deathHandlerRegistered = false;
            }
            if (_spawnHandlerRegistered)
            {
                DeregisterEventHandler<EventPlayerSpawn>(OnRespawnSpawn, HookMode.Post);
                _spawnHandlerRegistered = false;
            }
            if (_entityListenerRegistered)
            {
                RemoveListener<Listeners.OnEntitySpawned>(OnWeirdGrenadeEntitySpawned);
                _entityListenerRegistered = false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning("[RandomRoundEvents] Error deregistering handlers: {Error}", ex.Message);
        }

        _chaosDoubleDamage = false;
        _currentGravity = 800.0f;
        _tRespawns = 0;
        _ctRespawns = 0;
        SetGravity(800.0f);
        SetNospread(false);
        SetBhop(false);
        ResetAllPlayersVisibility();
        ResetAllPlayersHUD();
        ResetAllPlayersFOV();
        RemoveAllFog();
        RemoveAllGlow();
        ResetAllPlayersSizes();
        Server.ExecuteCommand("mp_respawn_on_death_t 0; mp_respawn_on_death_ct 0; mp_randomspawn 0; mp_buytime 20");
        EnableBuying();
        Server.ExecuteCommand("mp_taser_recharge_time 30; mp_friendlyfire 1");
        var infiniteAmmo = ConVar.Find("sv_infinite_ammo");
        if (infiniteAmmo != null) infiniteAmmo.SetValue(0);
        Server.ExecuteCommand("mp_death_drop_gun 0");
        ResetMaxHealth();
        _activeEvent = EventType.None;
    }

    private static void AnnounceEvent(string title, string description)
    {
        Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.White} {title}");
        Server.PrintToChatAll($" {ChatColors.Grey}» {description}");
    }

    private void StripAllWeapons()
    {
        foreach (var player in Utilities.GetPlayers())
            if (IsPlayerValid(player)) player.RemoveWeapons();

        // Give armor to all players after strip
        GiveAllPlayersFullArmor();

        if (Config.EnableBomb)
        {
            var ts = new List<CCSPlayerController>();
            foreach (var player in Utilities.GetPlayers())
            {
                if (IsPlayerValid(player) && player.Team == CsTeam.Terrorist)
                    ts.Add(player);
            }
            if (ts.Count > 0)
                ts[_random.Next(ts.Count)].GiveNamedItem("weapon_c4");
        }
    }

    private void GiveAllPlayersRandomWeapons()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player)) continue;
            string weapon = RandomWeapons[_random.Next(0, RandomWeapons.Count)];
            try { player.GiveNamedItem(weapon); }
            catch (Exception ex) { Logger.LogWarning("[RandomRoundEvents] Failed to give weapon to {Player}: {Error}", player.PlayerName, ex.Message); }
        }
    }

    private void GiveAllPlayersFlashbangs()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player)) continue;
            // CS2 max is 2 flashbangs — only give up to that
            int count = GetPlayerGrenadeCount(player, "weapon_flashbang");
            if (count < 1)
            {
                try { player.GiveNamedItem("weapon_flashbang"); }
                catch { /* ignore */ }
            }
        }
    }

    private void StartFlashbangSpamRound()
    {
        _flashbangSpamTimer?.Kill();
        _flashbangSpamTimer = AddTimer(Config.FlashbangRefillInterval, () =>
        {
            foreach (var player in Utilities.GetPlayers())
                if (IsPlayerValid(player)) GivePlayerFlashbang(player);
        }, TimerFlags.REPEAT);
    }

    private void GivePlayerFlashbang(CCSPlayerController player)
    {
        if (!IsPlayerValid(player)) return;
        if (GetPlayerGrenadeCount(player, "weapon_flashbang") >= 1) return;
        try { player.GiveNamedItem("weapon_flashbang"); }
        catch { /* ignore */ }
    }

    private void SetAllPlayersHealth(int health)
    {
        health = Math.Clamp(health, 1, 1000);
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
            var pawn = player.PlayerPawn.Value;
            if (health > 100)
                pawn.MaxHealth = health;
            pawn.Health = health;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
    }

    private void GiveAllPlayersKnives()
    {
        foreach (var player in Utilities.GetPlayers())
            if (IsPlayerValid(player)) player.GiveNamedItem("weapon_knife");
    }

    private void GiveAllPlayersZeus()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player)) continue;
            player.GiveNamedItem("weapon_taser");
        }
    }

    private void SwapRandomPlayers()
    {
        var ts = new List<CCSPlayerController>();
        var cts = new List<CCSPlayerController>();

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || !player.PawnIsAlive || player.IsBot) continue;
            if (player.Team == CsTeam.Terrorist) ts.Add(player);
            else if (player.Team == CsTeam.CounterTerrorist) cts.Add(player);
        }

        if (ts.Count == 0 || cts.Count == 0) return;

        var swappedT = ts[_random.Next(ts.Count)];
        var swappedCT = cts[_random.Next(cts.Count)];

        swappedT.SwitchTeam(CsTeam.CounterTerrorist);
        swappedCT.SwitchTeam(CsTeam.Terrorist);

        Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.White} {swappedT.PlayerName} is now a CT!");
        Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.White} {swappedCT.PlayerName} is now a T!");
    }

    private void StartSwapTimer()
    {
        _swapTimer?.Kill();
        _swapTimer = AddTimer((float)Config.SwapInterval, () =>
        {
            SwapRandomPlayers();
        }, TimerFlags.REPEAT);
    }

    private void GiveAllPlayersPistols()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player)) continue;
            string pistol = Pistols[_random.Next(0, Pistols.Count)];
            player.GiveNamedItem(pistol);
        }
    }

    private void GiveAllPlayersScout()
    {
        foreach (var player in Utilities.GetPlayers())
            if (IsPlayerValid(player)) player.GiveNamedItem("weapon_ssg08");
    }

    private static void GiveAllPlayersGlock()
    {
        foreach (var player in Utilities.GetPlayers())
            if (IsPlayerValid(player)) player.GiveNamedItem("weapon_glock");
    }

    private static void GiveAllPlayersDeagle()
    {
        foreach (var player in Utilities.GetPlayers())
            if (IsPlayerValid(player)) player.GiveNamedItem("weapon_deagle");
    }

    private void GiveAllPlayersShotgun()
    {
        string[] shotguns = { "weapon_nova", "weapon_xm1014", "weapon_mag7" };
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player)) continue;
            string shotgun = shotguns[_random.Next(shotguns.Length)];
            player.GiveNamedItem(shotgun);
        }
    }

    private void GiveAllPlayersFullArmor()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player)) continue;
            // item_assaultsuit gives kevlar + helmet
            player.GiveNamedItem("item_assaultsuit");
        }
    }

    private void GiveAllPlayersUnlimitedHE()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player)) continue;
            if (GetPlayerGrenadeCount(player, "weapon_hegrenade") < 1)
            {
                try { player.GiveNamedItem("weapon_hegrenade"); }
                catch { /* ignore */ }
            }
        }
    }

    private static void GiveAllPlayersMolotov()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player)) continue;
            if (GetPlayerGrenadeCount(player, "weapon_molotov") < 1 && GetPlayerGrenadeCount(player, "weapon_incgrenade") < 1)
            {
                try { player.GiveNamedItem("weapon_molotov"); }
                catch { /* ignore */ }
            }
        }
    }

    private void SetAllPlayersInvisible()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
            player.PlayerPawn.Value.Render = Color.FromArgb(0, 255, 255, 255);
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
        }
    }

    private void ResetAllPlayersVisibility()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.PlayerPawn.Value == null) continue;
            player.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
        }
    }

    private void RandomizeAllPlayersSpeed()
    {
        _playerSpeeds.Clear();
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
            float speed = Config.SpeedMin + (float)(_random.NextDouble() * (Config.SpeedMax - Config.SpeedMin));
            _playerSpeeds[player.Slot] = speed;
            player.PlayerPawn.Value.VelocityModifier = speed;
            if (!player.IsBot)
            {
                int percent = (int)(speed * 100);
                player.PrintToCenterAlert($"Speed: {percent}%");
            }
        }
        // Engine resets VelocityModifier on weapon switch, landing, etc. — enforce it
        _speedEnforceTimer?.Kill();
        _speedEnforceTimer = AddTimer(0.25f, () =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
                if (_playerSpeeds.TryGetValue(player.Slot, out float speed))
                    player.PlayerPawn.Value.VelocityModifier = speed;
            }
        }, TimerFlags.REPEAT);
    }

    private void ApplyNoReload()
    {
        // Strip reserve ammo so players only have what's in the clip — no reloading
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
            var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
            if (weapons == null) continue;
            foreach (var weaponHandle in weapons)
            {
                var weapon = weaponHandle.Value;
                if (weapon == null) continue;
                weapon.ReserveAmmo[0] = 0;
            }
        }
    }

    private void StartNoReloadTimer()
    {
        _noReloadTimer?.Kill();
        _noReloadTimer = AddTimer(0.5f, () =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
                var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
                if (weapons == null) continue;
                foreach (var weaponHandle in weapons)
                {
                    var weapon = weaponHandle.Value;
                    if (weapon == null) continue;
                    if (weapon.ReserveAmmo[0] > 0)
                        weapon.ReserveAmmo[0] = 0;
                }
            }
        }, TimerFlags.REPEAT);
    }

    private void StartAmmoRefillTimer()
    {
        _ammoRefillTimer?.Kill();
        _ammoRefillTimer = AddTimer(0.5f, () =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
                var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
                if (weapons == null) continue;
                foreach (var weaponHandle in weapons)
                {
                    var weapon = weaponHandle.Value;
                    if (weapon == null || weapon.DesignerName.Contains("knife") || weapon.DesignerName.Contains("bayonet")) continue;
                    if (weapon.ReserveAmmo[0] < 100)
                        weapon.ReserveAmmo[0] = 100;
                }
            }
        }, TimerFlags.REPEAT);
    }

    private void ApplyChaosRound()
    {
        _chaosDoubleDamage = false;
        var mods = new List<string>();

        StripAllWeapons();
        GiveAllPlayersKnives();

        // 1. Gravity: 40% low, 30% switching, 30% normal
        int gravRoll = _random.Next(100);
        if (gravRoll < 40)
        {
            SetGravity(Config.LowGravityValue);
            StartGravityMonitor();
            mods.Add("Low gravity");
        }
        else if (gravRoll < 70)
        {
            StartGravitySwitch();
            StartGravityMonitor();
            mods.Add("Gravity switching");
        }

        // 2. Speed: 50% chance
        if (_random.Next(100) < 50)
        {
            RandomizeAllPlayersSpeed();
            mods.Add("Random speed");
        }

        // 3. Double damage: 40% chance
        if (_random.Next(100) < 40)
        {
            _chaosDoubleDamage = true;
            RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post); _hurtHandlerRegistered = true;
            mods.Add($"{Config.DoubleDamageMultiplier}x damage");
        }

        // 4. Nospread: 30% chance
        if (_random.Next(100) < 30)
        {
            SetNospread(true);
            mods.Add("Perfect accuracy");
        }

        // 5. Weapon: pick one
        string[] weaponOptions = { "weapon_glock", "weapon_deagle", "weapon_ssg08", "weapon_ak47", "weapon_m4a1", "weapon_awp" };
        string[] weaponNames = { "Glock", "Deagle", "Scout", "AK-47", "M4A1", "AWP" };
        int weaponIdx = _random.Next(weaponOptions.Length);
        foreach (var player in Utilities.GetPlayers())
            if (IsPlayerValid(player)) player.GiveNamedItem(weaponOptions[weaponIdx]);
        mods.Add(weaponNames[weaponIdx]);

        string desc = string.Join(", ", mods);
        Server.PrintToChatAll($" {ChatColors.Red}[CHAOS ROUND]{ChatColors.White} Buckle up!");
        Server.PrintToChatAll($" {ChatColors.Grey}» {desc}");
    }

    private void SetGravity(float value)
    {
        _currentGravity = value;
        var gravity = ConVar.Find("sv_gravity");
        if (gravity != null)
        {
            try { gravity.SetValue(value); }
            catch (Exception ex) { Logger.LogWarning("[RandomRoundEvents] Failed to set gravity: {Error}", ex.Message); }
        }
        else
        {
            Logger.LogWarning("[RandomRoundEvents] sv_gravity ConVar not found.");
        }
    }

    private void StartGravityMonitor()
    {
        _gravityMonitorTimer?.Kill();
        _gravityMonitorTimer = AddTimer(0.5f, () =>
        {
            var gravity = ConVar.Find("sv_gravity");
            if (gravity == null) return;
            float current = gravity.GetPrimitiveValue<float>();
            if (Math.Abs(current - _currentGravity) > 0.01f)
                SetGravity(_currentGravity);
        }, TimerFlags.REPEAT);
    }

    private void ResetMaxHealth()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
            player.PlayerPawn.Value.MaxHealth = 100;
        }
    }

    private void HideAllPlayersHUD()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.PlayerPawn.Value == null) continue;
            player.PlayerPawn.Value.HideHUD = (uint)(player.PlayerPawn.Value.HideHUD | (1 << 8));
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBasePlayerPawn", "m_iHideHUD");
        }
    }

    private void ResetAllPlayersHUD()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.PlayerPawn.Value == null) continue;
            player.PlayerPawn.Value.HideHUD = (uint)(player.PlayerPawn.Value.HideHUD & ~(1 << 8));
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBasePlayerPawn", "m_iHideHUD");
        }
    }

    private void RandomizeAllPlayersFOV()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.PlayerPawn.Value == null) continue;
            uint fov = (uint)_random.Next((int)Config.ZoomMinFOV, (int)Config.ZoomMaxFOV + 1);
            player.DesiredFOV = fov;
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
            if (!player.IsBot)
                player.PrintToCenterAlert($"FOV: {fov}");
        }
    }

    private void ResetAllPlayersFOV()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid) continue;
            player.DesiredFOV = 0; // 0 = default
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
        }
    }

    private void ApplyFog()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.PlayerPawn.Value == null) continue;
            var fog = Utilities.CreateEntityByName<CFogController>("env_fog_controller");
            if (fog == null) continue;
            fog.Entity!.Name = $"rre_fog_{player.Slot}";
            fog.Fog.Enable = true;
            fog.Fog.ColorPrimary = Color.DarkGray;
            fog.Fog.Exponent = 1.0f;
            fog.Fog.Maxdensity = Config.FogDensity;
            fog.Fog.End = Config.FogEndDistance;
            fog.DispatchSpawn();
            player.PlayerPawn.Value.AcceptInput("SetFogController", fog, fog, "!activator");
            _fogEntities.Add(fog);
        }
    }

    private void RemoveAllFog()
    {
        foreach (var fog in _fogEntities)
        {
            if (fog.IsValid) fog.Remove();
        }
        _fogEntities.Clear();
    }

    private void ApplyGlowToAllPlayers()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
            var pawn = player.PlayerPawn.Value;
            var modelName = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState.ModelName;
            if (modelName == null) continue;

            Color glowColor = player.Team == CsTeam.Terrorist ? Color.Red : Color.Blue;

            // Create proxy prop (invisible, follows player)
            var proxy = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (proxy == null) continue;
            proxy.Spawnflags = 256u;
            proxy.RenderMode = RenderMode_t.kRenderNone;
            proxy.SetModel(modelName);
            proxy.AcceptInput("FollowEntity", pawn, proxy, "!activator");
            proxy.DispatchSpawn();

            // Create glow prop (visible glow, follows proxy)
            var glow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (glow == null) { proxy.Remove(); continue; }
            glow.SetModel(modelName);
            glow.AcceptInput("FollowEntity", proxy, glow, "!activator");
            glow.DispatchSpawn();
            glow.Render = Color.FromArgb(255, 255, 255, 255);
            glow.Glow.GlowColorOverride = glowColor;
            glow.Spawnflags = 256u;
            glow.RenderMode = RenderMode_t.kRenderNormal;
            glow.Glow.GlowRange = 5000;
            glow.Glow.GlowTeam = -1;
            glow.Glow.GlowType = 3;
            glow.Glow.GlowRangeMin = 20;

            _glowEntities.Add(proxy);
            _glowEntities.Add(glow);
        }
    }

    private void RemoveAllGlow()
    {
        foreach (var entity in _glowEntities)
        {
            if (entity.IsValid) entity.Remove();
        }
        _glowEntities.Clear();
    }

    private void RandomizeAllPlayersSizes()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
            var pawn = player.PlayerPawn.Value;
            var skeleton = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance();
            if (skeleton == null) continue;

            float size = (float)(Config.SizeMin + (_random.NextDouble() * (Config.SizeMax - Config.SizeMin)));
            size = (float)Math.Round(size, 2);

            skeleton.Scale = size;
            pawn.AcceptInput("SetScale", null, null, size.ToString());
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");

            // Scale HP proportionally
            int hp = (int)(100 * size);
            pawn.Health = hp;
            pawn.MaxHealth = hp;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");

            if (!player.IsBot)
                player.PrintToCenterAlert($"Size: {(int)(size * 100)}% | HP: {hp}");
        }
    }

    private void ResetAllPlayersSizes()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.PlayerPawn.Value == null) continue;
            var pawn = player.PlayerPawn.Value;
            var skeleton = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance();
            if (skeleton == null) continue;
            skeleton.Scale = 1.0f;
            pawn.AcceptInput("SetScale", null, null, "1");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_CBodyComponent");
        }
    }

    private void SpawnChickensForAllPlayers()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
            var pawn = player.PlayerPawn.Value;
            if (pawn.AbsOrigin == null) continue;

            for (int i = 0; i < Config.ChickenCount; i++)
            {
                var chicken = Utilities.CreateEntityByName<CChicken>("chicken");
                if (chicken == null) continue;

                var offset = new Vector(
                    (float)(100 * Math.Cos(2 * Math.PI * i / Config.ChickenCount)),
                    (float)(100 * Math.Sin(2 * Math.PI * i / Config.ChickenCount)),
                    0
                );

                chicken.DispatchSpawn();
                chicken.Teleport(pawn.AbsOrigin + offset, pawn.AbsRotation, pawn.AbsVelocity);
                chicken.CBodyComponent!.SceneNode!.Scale = Config.ChickenSize;
                Schema.SetSchemaValue(chicken.Handle, "CChicken", "m_leader", player.PlayerPawn.Raw);
            }
        }
    }

    private void TeleportToSpawn(CCSPlayerController victim)
    {
        if (victim.PlayerPawn.Value == null) return;
        string spawnName = victim.Team == CsTeam.CounterTerrorist
            ? "info_player_counterterrorist"
            : "info_player_terrorist";
        var spawns = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(spawnName).ToList();
        if (spawns.Count == 0)
            spawns = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_start").ToList();
        if (spawns.Count == 0) return;
        var spawn = spawns[_random.Next(spawns.Count)];
        if (spawn.AbsOrigin != null)
            victim.PlayerPawn.Value.Teleport(spawn.AbsOrigin);
    }

    private void OnWeirdGrenadeEntitySpawned(CEntityInstance entity)
    {
        if (_activeEvent != EventType.WeirdGrenadesRound) return;
        var name = entity.DesignerName;
        if (name != "flashbang_projectile" && name != "smokegrenade_projectile" &&
            name != "hegrenade_projectile" && name != "decoy_projectile")
            return;

        var grenade = entity.As<CBaseCSGrenadeProjectile>();
        Server.NextFrame(() =>
        {
            if (!grenade.IsValid) return;
            float min = Config.WeirdGrenadeMinTime;
            float max = Config.WeirdGrenadeMaxTime;
            float offset = (float)(_random.NextDouble() * (max - min) + min);
            grenade.DetonateTime = Server.CurrentTime + offset;
            Utilities.SetStateChanged(grenade, "CBaseGrenade", "m_flDetonateTime");
        });
    }

    private static void SetNospread(bool enabled)
    {
        var nospread = ConVar.Find("weapon_accuracy_nospread");
        if (nospread != null)
            nospread.SetValue(enabled ? 1 : 0);
    }

    private static void DisableBuying()
    {
        Server.ExecuteCommand("mp_buy_allow_guns 0; mp_buy_allow_grenades 0; mp_free_armor 0; mp_max_armor 0; mp_weapons_allow_zeus 0");
    }

    private static void EnableBuying()
    {
        Server.ExecuteCommand("mp_buy_allow_guns 255; mp_buy_allow_grenades 1; mp_max_armor 2; mp_weapons_allow_zeus -1");
    }

    private static void SetBhop(bool enabled)
    {
        Server.ExecuteCommand(enabled
            ? "sv_autobunnyhopping 1; sv_enablebunnyhopping 1"
            : "sv_autobunnyhopping 0; sv_enablebunnyhopping 0");
    }

    private void StartGravitySwitch()
    {
        _gravitySwitchTimer?.Kill();
        _gravitySwitchTimer = AddTimer(Config.GravitySwitchInterval, () =>
        {
            _currentGravity = _currentGravity == Config.GravitySwitchLow ? Config.GravitySwitchHigh : Config.GravitySwitchLow;
            SetGravity(_currentGravity);
        }, TimerFlags.REPEAT);
    }

    // Manual command handlers — admin only (@css/root)
    // Resets state and forces the event to apply on next round start
    private void ForceEvent(CCSPlayerController? player, EventType eventType)
    {
        if (!IsAdmin(player)) return;
        _forcedEvent = eventType;
        _roundEventTriggered = false;
        Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.White} Next round: {ChatColors.Green}{eventType}");
    }

    private void OnLowGravityCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.LowGravity);
    private void OnHeadshotOnlyCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.HeadshotOnly);
    private void OnRandomWeaponCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.RandomWeapon);
    private void OnDoubleDamageCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.DoubleDamage);
    private void OnSwapTeamsCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.SwapTeams);
    private void OnFlashbangSpamCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.FlashbangSpam);
    private void OnKnifeOnlyCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.KnifeOnly);
    private void OnZeusOnlyCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.ZeusOnly);
    private void OnNoReloadCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.NoReload);
    private void OnGravitySwitchCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.GravitySwitch);
    private void OnSpeedRandomizerCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.SpeedRandomizer);
    private void OnLastManStandingCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.LastManStanding);
    private void OnPowerUpRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.PowerUpRound);
    private void OnTankRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.TankRound);
    private void OnInvisibleRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.InvisibleRound);
    private void OnRespawnRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.RespawnRound);
    private void OnVampireRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.VampireRound);
    private void OnJammerRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.JammerRound);
    private void OnZoomRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.ZoomRound);
    private void OnFogRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.FogRound);
    private void OnGlowRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.GlowRound);
    private void OnSizeRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.SizeRound);
    private void OnChickenRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.ChickenRound);
    private void OnReturnToSenderCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.ReturnToSenderRound);
    private void OnWeirdGrenadesCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.WeirdGrenadesRound);
    private void OnChaosRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.ChaosRound);

    private void OnMenuCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !IsAdmin(player)) return;

        var menu = new ChatMenu("Event Roulette");
        menu.AddMenuOption("Low Gravity", (p, _) => { OnLowGravityCommand(p, command); });
        menu.AddMenuOption("Headshot Only", (p, _) => { OnHeadshotOnlyCommand(p, command); });
        menu.AddMenuOption("Random Weapon", (p, _) => { OnRandomWeaponCommand(p, command); });
        menu.AddMenuOption("Double Damage", (p, _) => { OnDoubleDamageCommand(p, command); });
        menu.AddMenuOption("Team Swap", (p, _) => { OnSwapTeamsCommand(p, command); });
        menu.AddMenuOption("Flashbang Spam", (p, _) => { OnFlashbangSpamCommand(p, command); });
        menu.AddMenuOption("Knife Only", (p, _) => { OnKnifeOnlyCommand(p, command); });
        menu.AddMenuOption("Zeus Only", (p, _) => { OnZeusOnlyCommand(p, command); });
        menu.AddMenuOption("No Reload", (p, _) => { OnNoReloadCommand(p, command); });
        menu.AddMenuOption("Gravity Switch", (p, _) => { OnGravitySwitchCommand(p, command); });
        menu.AddMenuOption("Speed Randomizer", (p, _) => { OnSpeedRandomizerCommand(p, command); });
        menu.AddMenuOption("Last Man Standing", (p, _) => { OnLastManStandingCommand(p, command); });
        menu.AddMenuOption("Power-Up Round", (p, _) => { OnPowerUpRoundCommand(p, command); });
        menu.AddMenuOption("Tank Round", (p, _) => { OnTankRoundCommand(p, command); });
        menu.AddMenuOption("Invisible Round", (p, _) => { OnInvisibleRoundCommand(p, command); });
        menu.AddMenuOption("Respawn Round", (p, _) => { OnRespawnRoundCommand(p, command); });
        menu.AddMenuOption("Vampire Round", (p, _) => { OnVampireRoundCommand(p, command); });
        menu.AddMenuOption("Jammer Round", (p, _) => { OnJammerRoundCommand(p, command); });
        menu.AddMenuOption("Zoom Round", (p, _) => { OnZoomRoundCommand(p, command); });
        menu.AddMenuOption("Fog of War", (p, _) => { OnFogRoundCommand(p, command); });
        menu.AddMenuOption("Glow Round", (p, _) => { OnGlowRoundCommand(p, command); });
        menu.AddMenuOption("Size Randomizer", (p, _) => { OnSizeRoundCommand(p, command); });
        menu.AddMenuOption("Chicken Leader", (p, _) => { OnChickenRoundCommand(p, command); });
        menu.AddMenuOption("Return to Sender", (p, _) => { OnReturnToSenderCommand(p, command); });
        menu.AddMenuOption("Weird Grenades", (p, _) => { OnWeirdGrenadesCommand(p, command); });
        menu.AddMenuOption("Chaos Round", (p, _) => { OnChaosRoundCommand(p, command); });
        menu.AddMenuOption("Reset All", (p, _) => { OnResetCommand(p, command); });

        MenuManager.OpenChatMenu(player, menu);
    }

    private void OnResetCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _roundEventTriggered = false;
        Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.White} All events reset.");
    }
}
