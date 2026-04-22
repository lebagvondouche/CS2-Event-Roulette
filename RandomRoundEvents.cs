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
using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace RandomRoundEvents;

public class RandomRoundEventsConfig : BasePluginConfig
{
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
    public bool EnableZoomRound { get; set; } = true;
    public bool EnableGlowRound { get; set; } = true;
    public bool EnableSizeRound { get; set; } = true;
    public bool EnableChickenRound { get; set; } = true;
    public bool EnableReturnToSenderRound { get; set; } = true;
    public bool EnableGrenadeRouletteRound { get; set; } = true;
    public bool EnableRainbowSmokesRound { get; set; } = true;
    public bool EnableClownGrenadesRound { get; set; } = false;

    // Legacy alias kept for backwards compatibility with older configs.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? EnableWeirdGrenadesRound { get; set; }

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
    public float SizeMin { get; set; } = 0.5f;
    public float SizeMax { get; set; } = 2.0f;
    public int ChickenCount { get; set; } = 5;
    public float ChickenSize { get; set; } = 2.0f;
    public float WeirdGrenadeMinTime { get; set; } = 0.1f;
    public float WeirdGrenadeMaxTime { get; set; } = 5.0f;
    public float ClownGrenadesModelScale { get; set; } = 0.35f;

    public static readonly List<string> DefaultMayhemRoundBlocklist = new()
    {
        "HeadshotOnly",
        "SwapTeams",
        "FlashbangSpam",
        "KnifeOnly",
        "ZeusOnly",
        "PowerUpRound",
        "TankRound",
        "InvisibleRound",
        "RespawnRound"
    };

    // Mayhem round
    public int MayhemRoundChance { get; set; } = 15; // percentage chance (0-100)
    public List<string> MayhemRoundBlocklist { get; set; } = [.. DefaultMayhemRoundBlocklist];

    // Legacy aliases kept for backwards compatibility with older configs.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ChaosRoundChance { get; set; } = int.MinValue;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ChaosRoundBlocklist { get; set; }
}

public class RandomRoundEvents : BasePlugin, IPluginConfig<RandomRoundEventsConfig>
{
    public override string ModuleName => "RandomRoundEvents";
    public override string ModuleVersion => "0.6.2";
    public override string ModuleAuthor => "Martin Persson";
    public override string ModuleDescription => "A plugin that triggers random events during rounds.";

    public RandomRoundEventsConfig Config { get; set; } = new RandomRoundEventsConfig();
    private readonly LoadoutCombat _loadoutCombat;
    private readonly MovementWorld _movementWorld;
    private readonly VisibilityInfo _visibilityInfo;
    private readonly ClownGrenades _clownGrenades;
    private readonly GrenadeRoulette _grenadeRoulette;
    private readonly RainbowSmokes _rainbowSmokes;
    private readonly Respawn _respawn;
    private readonly Mayhem _mayhem;
    private readonly Weapons _weapons;
    private readonly Settings _settings;
    private readonly Diagnostics _diagnostics;
    private readonly Spawns _spawns;

    public RandomRoundEvents()
    {
        _loadoutCombat = new LoadoutCombat(this);
        _movementWorld = new MovementWorld(this);
        _visibilityInfo = new VisibilityInfo(this);
        _clownGrenades = new ClownGrenades(this);
        _grenadeRoulette = new GrenadeRoulette(this);
        _rainbowSmokes = new RainbowSmokes(this);
        _respawn = new Respawn(this);
        _mayhem = new Mayhem(this, _grenadeRoulette);
        _weapons = new Weapons(this);
        _settings = new Settings(this);
        _diagnostics = new Diagnostics(this);
        _spawns = new Spawns(this);
    }

    internal bool IsClownGrenadesRoundActive => _activeEvent == EventType.ClownGrenadesRound;
    internal bool IsMayhemRoundActive => _activeEvent == EventType.MayhemRound;
    internal bool IsGrenadeRouletteRoundActive => _activeEvent == EventType.GrenadeRouletteRound;
    internal bool IsRainbowSmokesRoundActive => _activeEvent == EventType.RainbowSmokesRound;
    internal Random Random => _random;
    internal IReadOnlyList<string> RandomWeaponNames => RandomWeapons;
    internal IReadOnlyList<string> PistolNames => Pistols;
    internal float CurrentGravity => _currentGravity;
    internal static IEnumerable<CCSPlayerController> GetPlayers() => Utilities.GetPlayers();
    internal static bool IsValidAlivePlayer(CCSPlayerController player) => Players.IsValid(player);

    internal void LogClownGrenadesInfo(string message, params object[] args) =>
        Logger.LogInformation(message, args);

    internal void LogClownGrenadesWarning(string message, params object[] args) =>
        Logger.LogWarning(message, args);
    internal void LogMayhemInfo(string message, params object[] args) =>
        Logger.LogInformation(message, args);
    internal void LogMayhemWarning(string message, params object[] args) =>
        Logger.LogWarning(message, args);
    internal void LogGrenadeRouletteWarning(string message, params object[] args) =>
        Logger.LogWarning(message, args);
    internal void LogRainbowSmokesInfo(string message, params object[] args) =>
        Logger.LogInformation(message, args);
    internal void LogRainbowSmokesWarning(string message, params object[] args) =>
        Logger.LogWarning(message, args);
    internal void LogPluginWarning(string message, params object[] args) =>
        Logger.LogWarning(message, args);
    internal void ShowEvent(string title, string description) =>
        AnnounceEvent(title, description);

    private readonly Random _random = new();
    private CounterStrikeSharp.API.Modules.Timers.Timer? _gravitySwitchTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _flashbangSpamTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _gravityMonitorTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _speedEnforceTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _fovEnforceTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _swapTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _heRefillTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _noReloadTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _ammoRefillTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _bombRefreshTimer;
    
    private readonly Dictionary<int, float> _playerSpeeds = new();
    private readonly Dictionary<int, uint> _playerFovs = new();
    private readonly List<CDynamicProp> _glowEntities = new();
    private readonly List<CChicken> _spawnedChickens = new();
    private readonly HashSet<int> _pendingReturnToSenderTeleports = new();
    private bool _isLoaded = false;
    private bool _roundEventTriggered = false;
    private float _currentGravity = 800.0f;
    private float? _gravityMonitorTarget = null;

    private static readonly IReadOnlyList<string> RandomWeapons = new List<string>
    {
        "weapon_ak47", "weapon_m4a1", "weapon_awp", "weapon_ssg08", "weapon_mp5sd", "weapon_ump45", "weapon_deagle", "weapon_glock"
    }.AsReadOnly();

    private static readonly IReadOnlyList<string> Pistols = new List<string>
    {
        "weapon_deagle", "weapon_glock", "weapon_p250", "weapon_usp_silencer", "weapon_fiveseven"
    }.AsReadOnly();

    internal enum EventType
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
        MayhemRound,
        VampireRound,
        ZoomRound,
        GlowRound,
        SizeRound,
        ChickenRound,
        ReturnToSenderRound,
        GrenadeRouletteRound,
        ClownGrenadesRound,
        RainbowSmokesRound,
        
    }

    private EventType _activeEvent = EventType.None;
    private EventType _forcedEvent = EventType.None;
    private bool _hurtHandlerRegistered = false;
    private bool _itemPickupHandlerRegistered = false;
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
        Config.ZoomMinFOV = Math.Clamp(Config.ZoomMinFOV, 1u, 170u);
        Config.ZoomMaxFOV = Math.Clamp(Config.ZoomMaxFOV, Config.ZoomMinFOV, 170u);
        Config.SizeMin = Math.Clamp(Config.SizeMin, 0.1f, 5.0f);
        Config.SizeMax = Math.Clamp(Config.SizeMax, Config.SizeMin, 5.0f);
        Config.ChickenCount = Math.Clamp(Config.ChickenCount, 0, 32);
        Config.ChickenSize = Math.Clamp(Config.ChickenSize, 0.1f, 5.0f);
        Config.WeirdGrenadeMinTime = Math.Clamp(Config.WeirdGrenadeMinTime, 0.1f, 30.0f);
        Config.WeirdGrenadeMaxTime = Math.Clamp(Config.WeirdGrenadeMaxTime, Config.WeirdGrenadeMinTime, 30.0f);
        Config.ClownGrenadesModelScale = Math.Clamp(Config.ClownGrenadesModelScale, 0.1f, 3.0f);
        if (Config.ChaosRoundChance != int.MinValue && Config.MayhemRoundChance == 15)
            Config.MayhemRoundChance = Config.ChaosRoundChance;

        if ((Config.MayhemRoundBlocklist.Count == 0 || Config.MayhemRoundBlocklist.SequenceEqual(RandomRoundEventsConfig.DefaultMayhemRoundBlocklist))
            && Config.ChaosRoundBlocklist is { Count: > 0 })
        {
            Config.MayhemRoundBlocklist = [.. Config.ChaosRoundBlocklist];
        }

        if (Config.EnableWeirdGrenadesRound.HasValue)
            Config.EnableGrenadeRouletteRound = Config.EnableWeirdGrenadesRound.Value;

        Config.MayhemRoundChance = Math.Clamp(Config.MayhemRoundChance, 0, 100);

        Logger.LogInformation("[RandomRoundEvents] Configuration loaded.");
        if (Config.EnableClownGrenadesRound)
            Logger.LogWarning("[RandomRoundEvents] Clown Grenades is currently WIP and kept out of automatic rotation. Use !rre_clowngrenades for manual testing.");
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

        CaptureManagedConVars();
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
        AddCommand("css_rre_inception", "Trigger Inception Round event", OnZoomRoundCommand);
        AddCommand("css_rre_xraygoggles", "Trigger X-Ray Goggles Round event", OnGlowRoundCommand);
        AddCommand("css_rre_size", "Trigger Size Randomizer Round event", OnSizeRoundCommand);
        AddCommand("css_rre_chicken", "Trigger Chicken Leader Round event", OnChickenRoundCommand);
        AddCommand("css_rre_returntosender", "Trigger Return to Sender Round event", OnReturnToSenderCommand);
        AddCommand("css_rre_grenaderoulette", "Trigger Grenade Roulette Round event", OnGrenadeRouletteCommand);
        AddCommand("css_rre_rainbowsmokes", "Trigger Rainbow Smokes Round event", OnRainbowSmokesCommand);
        AddCommand("css_rre_clowngrenades", "Trigger Clown Grenades Round event", OnClownGrenadesCommand);
        AddCommand("css_rre_reset", "Reset all events", OnResetCommand);
        AddCommand("css_rre_menu", "Open event selection menu", OnMenuCommand);
        AddCommand("css_rre_mayhem", "Trigger Mayhem Round", OnMayhemRoundCommand);
        AddCommand("css_rre_dumpmodels", "Dump currently readable server-side entity models", OnDumpModelsCommand);

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
        if (Config.EnableZoomRound) enabledEvents.Add(EventType.ZoomRound);
        if (Config.EnableGlowRound) enabledEvents.Add(EventType.GlowRound);
        if (Config.EnableSizeRound) enabledEvents.Add(EventType.SizeRound);
        if (Config.EnableChickenRound) enabledEvents.Add(EventType.ChickenRound);
        if (Config.EnableReturnToSenderRound) enabledEvents.Add(EventType.ReturnToSenderRound);
        if (Config.EnableGrenadeRouletteRound) enabledEvents.Add(EventType.GrenadeRouletteRound);
        if (Config.EnableRainbowSmokesRound) enabledEvents.Add(EventType.RainbowSmokesRound);
        // Clown Grenades is currently WIP and should only be triggered manually.

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
        else if (Config.MayhemRoundChance > 0 && _random.Next(0, 100) < Config.MayhemRoundChance)
        {
            selectedEvent = EventType.MayhemRound;
        }
        else
            selectedEvent = enabledEvents[_random.Next(0, enabledEvents.Count)];

        Logger.LogInformation("[RandomRoundEvents] Selected event: {Event}", selectedEvent);
        ResetAllState();  // resets based on previous _activeEvent, then sets it to None
        _activeEvent = selectedEvent;

        // Enable buying only for events that allow it
        if (selectedEvent == EventType.SwapTeams || selectedEvent == EventType.SpeedRandomizer || selectedEvent == EventType.GravitySwitch || selectedEvent == EventType.RespawnRound || selectedEvent == EventType.VampireRound || selectedEvent == EventType.ZoomRound || selectedEvent == EventType.GlowRound || selectedEvent == EventType.SizeRound || selectedEvent == EventType.ChickenRound || selectedEvent == EventType.GrenadeRouletteRound || selectedEvent == EventType.RainbowSmokesRound || selectedEvent == EventType.ClownGrenadesRound)
            EnableBuying();
        else
            DisableBuying();

        switch (selectedEvent)
        {
            case EventType.LowGravity:
            case EventType.SwapTeams:
            case EventType.GravitySwitch:
            case EventType.SpeedRandomizer:
            case EventType.ChickenRound:
                _movementWorld.Apply(selectedEvent);
                break;
            case EventType.HeadshotOnly:
            case EventType.RandomWeapon:
            case EventType.DoubleDamage:
            case EventType.FlashbangSpam:
            case EventType.KnifeOnly:
            case EventType.ZeusOnly:
            case EventType.NoReload:
            case EventType.LastManStanding:
            case EventType.PowerUpRound:
            case EventType.TankRound:
            case EventType.VampireRound:
            case EventType.ReturnToSenderRound:
                _loadoutCombat.Apply(selectedEvent);
                break;
            case EventType.InvisibleRound:
            case EventType.ZoomRound:
            case EventType.GlowRound:
            case EventType.SizeRound:
                _visibilityInfo.Apply(selectedEvent);
                break;
            case EventType.RespawnRound:
                _respawn.Apply();
                break;
            case EventType.GrenadeRouletteRound:
                _grenadeRoulette.Apply();
                break;
            case EventType.RainbowSmokesRound:
                _rainbowSmokes.Apply();
                break;
            case EventType.ClownGrenadesRound:
                _clownGrenades.Apply();
                break;
            case EventType.MayhemRound:
                _mayhem.Apply();
                break;
        }

        if (Config.EnableBomb)
            StartBombSelectionRefresh();

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
        if (victim == null || !victim.IsValid || victim.PlayerPawn.Value == null) return HookResult.Continue;

        var pawn = victim.PlayerPawn.Value;
        string weapon = @event.Weapon ?? string.Empty;
        int victimMaxHealth = pawn.MaxHealth > 0 ? pawn.MaxHealth : 100;

        // Heal back knife damage on PowerUp/Flashbang rounds FIRST (before death check)
        if ((_activeEvent == EventType.PowerUpRound || _activeEvent == EventType.FlashbangSpam) && 
            (weapon.Contains("knife") || weapon.Contains("bayonet")))
        {
            RestorePawnHealth(pawn, @event.DmgHealth, _activeEvent == EventType.PowerUpRound ? Config.PowerUpHP : Config.FlashbangStartHP);
            return HookResult.Continue; // Don't process further
        }

        if (_activeEvent == EventType.HeadshotOnly && @event.Hitgroup != 1)
        {
            RestorePawnHealth(pawn, @event.DmgHealth, victimMaxHealth);
            return HookResult.Continue;
        }

        // Skip if player is already dead
        if (pawn.Health <= 0) return HookResult.Continue;

        var attacker = @event.Attacker;
        if (attacker == null || !IsPlayerValid(attacker)) return HookResult.Continue;

        if (_activeEvent == EventType.HeadshotOnly && @event.Hitgroup != 1)
        {
            // Not a headshot — heal back the damage
            int dmg = @event.DmgHealth;
            pawn.Health = Math.Min(pawn.Health + dmg, 100);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }

        if (_activeEvent == EventType.DoubleDamage || _mayhem.DoubleDamageActive)
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

        if ((_activeEvent == EventType.VampireRound || _mayhem.VampireActive) && attacker.PlayerPawn.Value != null)
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

        if ((_activeEvent == EventType.ReturnToSenderRound || _mayhem.ReturnToSenderActive) && pawn.Health > 0)
        {
            QueueReturnToSenderTeleport(victim);
        }

        return HookResult.Continue;
    }

    internal void StartHERefillTimer()
    {
        _heRefillTimer?.Kill();
        _heRefillTimer = AddTimer(1.0f, () =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!Players.CanReceiveGrenadeRefill(player)) continue;
                if (Players.GetGrenadeCount(player, "weapon_hegrenade") < 1)
                {
                    try { player.GiveNamedItem("weapon_hegrenade"); }
                    catch { /* ignore */ }
                }
                if (Players.GetGrenadeCount(player, "weapon_molotov") < 1 && Players.GetGrenadeCount(player, "weapon_incgrenade") < 1)
                {
                    try { player.GiveNamedItem("weapon_molotov"); }
                    catch { /* ignore */ }
                }
            }
        }, TimerFlags.REPEAT);
    }

    private void StartBombSelectionRefresh()
    {
        _bombRefreshTimer?.Kill();

        LogBombState("bomb_refresh_start");

        int passes = 0;
        _bombRefreshTimer = AddTimer(0.4f, () =>
        {
            if (_activeEvent == EventType.None)
                return;

            RefreshBombCarrierSelection();
            LogBombState($"bomb_refresh_pass_{passes + 1}");
            passes++;

            if (passes >= 6)
            {
                _bombRefreshTimer?.Kill();
                _bombRefreshTimer = null;
            }
        }, TimerFlags.REPEAT);
    }

    private void RefreshBombCarrierSelection()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!HasBomb(player) || player.IsBot)
                continue;

            RefreshBombSelectionForPlayer(player);
        }
    }

    private void RefreshBombSelectionForPlayer(CCSPlayerController player)
    {
        try
        {
            player.ExecuteClientCommandFromServer("slot3");
            AddTimer(0.1f, () =>
            {
                if (!HasBomb(player) || !player.IsValid)
                    return;

                try
                {
                    player.ExecuteClientCommandFromServer("slot5");
                }
                catch (Exception ex)
                {
                    Logger.LogDebug("[RandomRoundEvents] Failed to switch bomb carrier to slot5 for {Player}: {Error}", player.PlayerName, ex.Message);
                }
            });

            AddTimer(0.2f, () =>
            {
                if (!HasBomb(player) || !player.IsValid)
                    return;

                try
                {
                    player.ExecuteClientCommandFromServer("use weapon_c4");
                }
                catch (Exception ex)
                {
                    Logger.LogDebug("[RandomRoundEvents] Failed to refresh bomb carrier selection for {Player}: {Error}", player.PlayerName, ex.Message);
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogDebug("[RandomRoundEvents] Failed to refresh bomb selection for {Player}: {Error}", player.PlayerName, ex.Message);
        }
    }

    private static bool HasBomb(CCSPlayerController player)
    {
        if (!player.IsValid || player.PlayerPawn.Value == null)
            return false;

        return Players.HasWeapon(player, "weapon_c4");
    }

    private void LogBombState(string context)
    {
        if (!Config.Debug || !Config.EnableBomb)
            return;

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.PlayerPawn.Value == null)
                continue;

            var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
            string inventory = weapons == null
                ? "<no weapon services>"
                : string.Join(", ", weapons
                    .Select(handle => handle.Value?.DesignerName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Cast<string>());

            Logger.LogInformation(
                "[RandomRoundEvents] Bomb debug {Context}: {Player} team={Team} alive={Alive} hasC4={HasBomb} inventory=[{Inventory}]",
                context,
                player.PlayerName,
                player.Team,
                player.PawnIsAlive,
                HasBomb(player),
                inventory);
        }
    }


    private HookResult OnItemPickup(EventItemPickup @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !IsPlayerValid(player) || player.PlayerPawn.Value == null) return HookResult.Continue;

        // Strip reserve ammo on any weapon pickup during No Reload round
        AddTimer(0.1f, () =>
        {
            if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) return;
            var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
            if (weapons == null) return;
            foreach (var weaponHandle in weapons)
            {
                var weapon = weaponHandle.Value;
                if (weapon == null) continue;
                Weapons.ZeroReserveAmmo(weapon);
            }
        });

        return HookResult.Continue;
    }

    internal void EnsurePlayerHurtHandler()
    {
        if (_hurtHandlerRegistered)
            return;

        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post);
        _hurtHandlerRegistered = true;
    }

    internal void EnsureItemPickupHandler()
    {
        if (_itemPickupHandlerRegistered)
            return;

        RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Post);
        _itemPickupHandlerRegistered = true;
    }

    private static bool IsPlayerValid(CCSPlayerController player) => Players.IsValid(player);

    private static bool IsAdmin(CCSPlayerController? player)
    {
        // Server console always has permission
        if (player == null) return true;
        return AdminManager.PlayerHasPermissions(player, "@css/root");
    }

    private void ResetAllState()
    {
        _bombRefreshTimer?.Kill();
        _pendingReturnToSenderTeleports.Clear();

        _loadoutCombat.Reset();
        _movementWorld.Reset();
        _visibilityInfo.Reset();
        _clownGrenades.Reset();
        _grenadeRoulette.Reset();
        _rainbowSmokes.Reset();
        _respawn.Reset();
        _mayhem.Reset();

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
        }
        catch (Exception ex)
        {
            Logger.LogWarning("[RandomRoundEvents] Error deregistering handlers: {Error}", ex.Message);
        }

        _currentGravity = 800.0f;
        RestoreManagedConVars();
        _activeEvent = EventType.None;
    }

    internal void ResetLoadoutCombatState()
    {
        _flashbangSpamTimer?.Kill();
        _heRefillTimer?.Kill();
        _noReloadTimer?.Kill();
        _ammoRefillTimer?.Kill();
        ResetMaxHealth();
    }

    internal void ResetMovementWorldState()
    {
        _gravitySwitchTimer?.Kill();
        _gravityMonitorTimer?.Kill();
        _speedEnforceTimer?.Kill();
        _swapTimer?.Kill();
        _gravityMonitorTarget = null;
        _playerSpeeds.Clear();
        RemoveAllChickens();
    }

    internal void ResetVisibilityInfoState()
    {
        _fovEnforceTimer?.Kill();
        _playerFovs.Clear();
        ResetAllPlayersVisibility();
        ResetAllPlayersFOV();
        RemoveAllGlow();
        ResetAllPlayersSizes();
    }

    private void QueueReturnToSenderTeleport(CCSPlayerController victim)
    {
        if (!victim.IsValid || victim.PlayerPawn.Value == null) return;

        int slot = victim.Slot;
        if (!_pendingReturnToSenderTeleports.Add(slot))
            return;

        AddTimer(0.05f, () =>
        {
            _pendingReturnToSenderTeleports.Remove(slot);

            if (!IsPlayerValid(victim) || victim.PlayerPawn.Value == null)
                return;

            TeleportToSpawn(victim);
        });
    }

    private static void AnnounceEvent(string title, string description)
    {
        Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.White} {title}");
        Server.PrintToChatAll($" {ChatColors.Grey}» {description}");
    }

    private static void RestorePawnHealth(CCSPlayerPawn pawn, int amount, int maxHealth)
    {
        Players.RestoreHealth(pawn, amount, maxHealth);
    }

    internal void StripAllWeapons()
    {
        _weapons.StripAllWeapons();
    }

    internal void GiveAllPlayersRandomWeapons() => _weapons.GiveAllPlayersRandomWeapons();

    internal void GiveAllPlayersStandardLoadout() => _weapons.GiveAllPlayersStandardLoadout();

    internal static void GiveAllPlayersDefaultPistols() => Weapons.GiveAllPlayersDefaultPistols();

    internal static void GivePlayerDefaultPistol(CCSPlayerController player) => Weapons.GivePlayerDefaultPistol(player);

    internal void GiveAllPlayersFlashbangs() => _weapons.GiveAllPlayersFlashbangs();

    internal void StartFlashbangSpamRound()
    {
        _flashbangSpamTimer?.Kill();
        _flashbangSpamTimer = AddTimer(Config.FlashbangRefillInterval, () =>
        {
            foreach (var player in Utilities.GetPlayers())
                if (Players.IsValid(player)) _weapons.GivePlayerFlashbang(player);
        }, TimerFlags.REPEAT);
    }

    internal void SetAllPlayersHealth(int health) => _weapons.SetAllPlayersHealth(health);

    internal void GiveAllPlayersKnives() => _weapons.GiveAllPlayersKnives();

    internal void GiveAllPlayersZeus() => _weapons.GiveAllPlayersZeus();

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

    internal void StartSwapTimer()
    {
        _swapTimer?.Kill();
        _swapTimer = AddTimer((float)Config.SwapInterval, () =>
        {
            SwapRandomPlayers();
        }, TimerFlags.REPEAT);
    }

    internal void GiveAllPlayersPistols() => _weapons.GiveAllPlayersPistols();

    internal void GiveAllPlayersScout() => _weapons.GiveAllPlayersScout();

    internal static void GiveAllPlayersGlock() => Weapons.GiveAllPlayersGlock();

    internal static void GiveAllPlayersAK47() => Weapons.GiveAllPlayersAK47();

    internal static void ZeroAllReserveAmmo() => Weapons.ZeroAllReserveAmmo();

    internal static void GiveAllPlayersDeagle() => Weapons.GiveAllPlayersDeagle();

    internal void GiveAllPlayersShotgun() => _weapons.GiveAllPlayersShotgun();

    internal void GiveAllPlayersFullArmor() => _weapons.GiveAllPlayersFullArmor();

    internal void GiveAllPlayersXM1014() => _weapons.GiveAllPlayersXM1014();

    internal void GiveAllPlayersUnlimitedHE() => _weapons.GiveAllPlayersUnlimitedHE();

    internal static void GiveAllPlayersMolotov() => Weapons.GiveAllPlayersMolotov();

    internal void SetAllPlayersInvisible()
    {
        SetAllPlayersVisibilityAlpha(0);
    }

    private void SetAllPlayersVisibilityAlpha(int alpha)
    {
        alpha = Math.Clamp(alpha, 0, 255);
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
            player.PlayerPawn.Value.RenderMode = alpha == 0
                ? RenderMode_t.kRenderNone
                : RenderMode_t.kRenderNormal;
            player.PlayerPawn.Value.Render = Color.FromArgb(alpha, 255, 255, 255);
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_nRenderMode");
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
        }
    }

    internal void SetAllPlayerWeaponsInvisible()
    {
        SetAllPlayerWeaponsVisibilityAlpha(0);
    }

    private void SetAllPlayerWeaponsVisibilityAlpha(int alpha)
    {
        alpha = Math.Clamp(alpha, 0, 255);
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
            var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
            if (weapons == null) continue;

            foreach (var weaponHandle in weapons)
            {
                var weapon = weaponHandle.Value;
                if (weapon == null) continue;
                weapon.RenderMode = alpha == 0
                    ? RenderMode_t.kRenderNone
                    : RenderMode_t.kRenderNormal;
                weapon.Render = Color.FromArgb(alpha, 255, 255, 255);
                Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_nRenderMode");
                Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_clrRender");
            }
        }
    }

    private void ResetAllPlayersVisibility()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.PlayerPawn.Value == null) continue;
            player.PlayerPawn.Value.RenderMode = RenderMode_t.kRenderNormal;
            player.PlayerPawn.Value.Render = Color.FromArgb(255, 255, 255, 255);
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_nRenderMode");
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");

            var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
            if (weapons == null) continue;

            foreach (var weaponHandle in weapons)
            {
                var weapon = weaponHandle.Value;
                if (weapon == null) continue;
                weapon.RenderMode = RenderMode_t.kRenderNormal;
                weapon.Render = Color.FromArgb(255, 255, 255, 255);
                Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_nRenderMode");
                Utilities.SetStateChanged(weapon, "CBaseModelEntity", "m_clrRender");
            }
        }
    }

    internal void RandomizeAllPlayersSpeed()
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

    internal void ApplyNoReload()
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
                Weapons.ZeroReserveAmmo(weapon);
            }
        }

        Server.NextFrame(Weapons.ZeroAllReserveAmmo);
        AddTimer(0.1f, Weapons.ZeroAllReserveAmmo);
    }

    internal void StartNoReloadTimer()
    {
        _noReloadTimer?.Kill();
        _noReloadTimer = AddTimer(0.1f, () =>
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
                        Weapons.ZeroReserveAmmo(weapon);
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

    private void ApplyMayhemRound()
    {
        _mayhem.Apply();
    }

    internal void SetGravity(float value)
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

    internal void StartGravityMonitor()
    {
        StartGravityMonitor(_currentGravity);
    }

    internal void StartGravityMonitor(float desiredGravity)
    {
        _gravityMonitorTimer?.Kill();
        _gravityMonitorTarget = desiredGravity;
        _gravityMonitorTimer = AddTimer(0.5f, () =>
        {
            if (_gravityMonitorTarget == null)
                return;

            try
            {
                var gravity = ConVar.Find("sv_gravity");
                if (gravity == null)
                    return;

                float current = gravity.GetPrimitiveValue<float>();
                if (Math.Abs(current - _gravityMonitorTarget.Value) > 0.01f)
                    SetGravity(_gravityMonitorTarget.Value);
            }
            catch (Exception ex)
            {
                Logger.LogDebug("[RandomRoundEvents] Gravity monitor retry after read failure: {Error}", ex.Message);
                SetGravity(_gravityMonitorTarget.Value);
            }
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

    internal void RandomizeAllPlayersFOV()
    {
        _playerFovs.Clear();
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.PlayerPawn.Value == null) continue;
            uint fov = (uint)_random.Next((int)Config.ZoomMinFOV, (int)Config.ZoomMaxFOV + 1);
            _playerFovs[player.Slot] = fov;
            player.DesiredFOV = fov;
            Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
            if (!player.IsBot)
                player.PrintToCenterAlert($"FOV: {fov}");
        }

        _fovEnforceTimer?.Kill();
        _fovEnforceTimer = AddTimer(0.5f, () =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!player.IsValid) continue;
                if (_playerFovs.TryGetValue(player.Slot, out uint fov) && player.DesiredFOV != fov)
                {
                    player.DesiredFOV = fov;
                    Utilities.SetStateChanged(player, "CBasePlayerController", "m_iDesiredFOV");
                }
            }
        }, TimerFlags.REPEAT);
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

    internal void ApplyGlowToAllPlayers()
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

    private void RemoveAllChickens()
    {
        foreach (var chicken in _spawnedChickens)
        {
            if (chicken.IsValid)
                chicken.Remove();
        }

        _spawnedChickens.Clear();
    }

    internal void RandomizeAllPlayersSizes()
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
            pawn.AcceptInput("SetScale", null, null, size.ToString(CultureInfo.InvariantCulture));
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

    internal void SpawnChickensForAllPlayers()
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
                _spawnedChickens.Add(chicken);
            }
        }
    }

    internal void TeleportToSpawn(CCSPlayerController victim) => _spawns.TeleportToSpawn(victim);

    private void OnWeirdGrenadeEntitySpawned(CEntityInstance entity)
    {
        _grenadeRoulette.OnEntitySpawned(entity);
    }

    internal void SetNospread(bool enabled) => _settings.SetNospread(enabled);

    private void DisableBuying() => _settings.DisableBuying();

    private void EnableBuying() => _settings.EnableBuying();

    internal void SetBhop(bool enabled) => _settings.SetBhop(enabled);

    private void CaptureManagedConVars() => _settings.CaptureManagedConVars();

    internal void GiveAllPlayersGrenadeRouletteGrenades() => _weapons.GiveAllPlayersGrenadeRouletteGrenades();

    internal void GiveAllPlayersSmokes() => _weapons.GiveAllPlayersSmokes();

    private void RestoreManagedConVars() => _settings.RestoreManagedConVars();

    internal void SetConVar(string name, int value) => _settings.SetConVar(name, value);

    internal void SetConVar(string name, float value) => _settings.SetConVar(name, value);

    internal void SetConVar(string name, string value) => _settings.SetConVar(name, value);

    internal void StartGravitySwitch()
    {
        _gravitySwitchTimer?.Kill();
        _gravitySwitchTimer = AddTimer(Config.GravitySwitchInterval, () =>
        {
            _currentGravity = _currentGravity == Config.GravitySwitchLow ? Config.GravitySwitchHigh : Config.GravitySwitchLow;
            SetGravity(_currentGravity);
        }, TimerFlags.REPEAT);
    }

    private static string GetEventDisplayName(EventType eventType)
    {
        return eventType switch
        {
            EventType.HeadshotOnly => "Juan Deag Round",
            EventType.ZoomRound => "Inception Round",
            EventType.GlowRound => "X-Ray Goggles Round",
            EventType.MayhemRound => "Mayhem Round",
            EventType.SizeRound => "Size Randomizer Round",
            EventType.GrenadeRouletteRound => "Grenade Roulette Round",
            EventType.RainbowSmokesRound => "Rainbow Smokes Round",
            EventType.ClownGrenadesRound => "Clown Grenades Round",
            EventType.ReturnToSenderRound => "Return to Sender Round",
            EventType.PowerUpRound => "Power-Up Round",
            EventType.RespawnRound => "Respawn Round",
            EventType.ChickenRound => "Chicken Leader Round",
            _ => eventType.ToString()
        };
    }

    // Manual command handlers — admin only (@css/root)
    // Resets state and forces the event to apply on next round start
    private void ForceEvent(CCSPlayerController? player, EventType eventType)
    {
        if (!IsAdmin(player)) return;
        _forcedEvent = eventType;
        _roundEventTriggered = false;
        Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.White} Next round: {ChatColors.Green}{GetEventDisplayName(eventType)}");
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
    private void OnZoomRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.ZoomRound);
    private void OnGlowRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.GlowRound);
    private void OnSizeRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.SizeRound);
    private void OnChickenRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.ChickenRound);
    private void OnReturnToSenderCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.ReturnToSenderRound);
    private void OnGrenadeRouletteCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.GrenadeRouletteRound);
    private void OnRainbowSmokesCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.RainbowSmokesRound);
    private void OnClownGrenadesCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.ClownGrenadesRound);
    private void OnMayhemRoundCommand(CCSPlayerController? player, CommandInfo command) => ForceEvent(player, EventType.MayhemRound);
    private void OnDumpModelsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        _diagnostics.DumpLoadedModels(player);
    }

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
        menu.AddMenuOption("Inception Round", (p, _) => { OnZoomRoundCommand(p, command); });
        menu.AddMenuOption("X-Ray Goggles Round", (p, _) => { OnGlowRoundCommand(p, command); });
        menu.AddMenuOption("Size Randomizer", (p, _) => { OnSizeRoundCommand(p, command); });
        menu.AddMenuOption("Chicken Leader", (p, _) => { OnChickenRoundCommand(p, command); });
        menu.AddMenuOption("Return to Sender", (p, _) => { OnReturnToSenderCommand(p, command); });
        menu.AddMenuOption("Grenade Roulette", (p, _) => { OnGrenadeRouletteCommand(p, command); });
        menu.AddMenuOption("Rainbow Smokes", (p, _) => { OnRainbowSmokesCommand(p, command); });
        menu.AddMenuOption("Clown Grenades", (p, _) => { OnClownGrenadesCommand(p, command); });
        menu.AddMenuOption("Mayhem Round", (p, _) => { OnMayhemRoundCommand(p, command); });
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
