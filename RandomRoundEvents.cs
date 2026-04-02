using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;
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

    // Chaos round
    public int ChaosRoundChance { get; set; } = 15; // percentage chance (0-100)
}

public class RandomRoundEvents : BasePlugin, IPluginConfig<RandomRoundEventsConfig>
{
    public override string ModuleName => "RandomRoundEvents";
    public override string ModuleVersion => "1.2";
    public override string ModuleAuthor => "Martin Persson";
    public override string ModuleDescription => "A plugin that triggers random events during rounds.";

    public RandomRoundEventsConfig Config { get; set; } = new RandomRoundEventsConfig();

    private readonly Random _random = new();
    private CounterStrikeSharp.API.Modules.Timers.Timer? _gravitySwitchTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _flashbangSpamTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _gravityMonitorTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _speedEnforceTimer;
    private CounterStrikeSharp.API.Modules.Timers.Timer? _swapTimer;
    private readonly Dictionary<int, float> _playerSpeeds = new();
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
        ChaosRound
    }

    private EventType _activeEvent = EventType.None;
    private bool _chaosDoubleDamage = false;

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
        RegisterEventHandler<EventItemPurchase>(OnItemPurchase, HookMode.Pre);

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
        AddCommand("css_rre_reset", "Reset all events", OnResetCommand);
        AddCommand("css_rre_menu", "Open event selection menu", OnMenuCommand);
        AddCommand("css_rre_chaos", "Trigger Chaos Round", OnChaosRoundCommand);

        _isLoaded = true;
        Logger.LogInformation("[RandomRoundEvents] Plugin loaded successfully.");
    }

    public override void Unload(bool hotReload)
    {
        _gravitySwitchTimer?.Kill();
        _flashbangSpamTimer?.Kill();
        _gravityMonitorTimer?.Kill();
        _speedEnforceTimer?.Kill();
        _swapTimer?.Kill();
        _isLoaded = false;
        _roundEventTriggered = false;
        base.Unload(hotReload);
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Skip events during warmup
        var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
        if (gameRules?.GameRules?.WarmupPeriod == true)
            return HookResult.Continue;

        if (_roundEventTriggered)
            return HookResult.Continue;

        _gravitySwitchTimer?.Kill();
        _flashbangSpamTimer?.Kill();
        _gravityMonitorTimer?.Kill();
        _speedEnforceTimer?.Kill();
        _swapTimer?.Kill();
        _playerSpeeds.Clear();

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

        if (enabledEvents.Count == 0)
        {
            Logger.LogWarning("[RandomRoundEvents] No events enabled in configuration.");
            return HookResult.Continue;
        }

        // Roll for chaos round first
        EventType selectedEvent;
        if (Config.ChaosRoundChance > 0 && _random.Next(0, 100) < Config.ChaosRoundChance)
            selectedEvent = EventType.ChaosRound;
        else
            selectedEvent = enabledEvents[_random.Next(0, enabledEvents.Count)];

        _activeEvent = selectedEvent;
        Logger.LogInformation("[RandomRoundEvents] Selected event: {Event}", selectedEvent);
        ResetAllState();
        _activeEvent = selectedEvent;
        DisableBuying();

        switch (selectedEvent)
        {
            case EventType.LowGravity:
                AnnounceEvent("Low Gravity Round", "Float around with a Scout and Zeus. Perfect accuracy!");
                StripAllWeapons();
                SetGravity(Config.LowGravityValue);
                SetNospread(true);
                StartGravityMonitor();
                GiveAllPlayersScout();
                GiveAllPlayersZeusOnly();
                Server.ExecuteCommand($"mp_taser_recharge_time {Config.ZeusRechargeTime}");
                break;
            case EventType.HeadshotOnly:
                AnnounceEvent("Juan Deag Round", "Deagle only, headshots only. One tap or nothing!");
                StripAllWeapons();
                GiveAllPlayersKnives();
                GiveAllPlayersDeagle();
                RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post);
                break;
            case EventType.RandomWeapon:
                AnnounceEvent("Random Weapon Round", "Everyone gets a random weapon. Good luck!");
                StripAllWeapons();
                GiveAllPlayersRandomWeapons();
                break;
            case EventType.DoubleDamage:
                AnnounceEvent("Double Damage Round", "All damage is doubled. Play it safe!");
                StripAllWeapons();
                GiveAllPlayersGlock();
                RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post);
                break;
            case EventType.SwapTeams:
                AnnounceEvent("Team Swap Round", "A random pair swaps teams every 30 seconds!");
                StartSwapTimer();
                break;
            case EventType.FlashbangSpam:
                AnnounceEvent("Flashbang Spam Round", "1 HP, flashbangs only. One flash and you're dead!");
                StripAllWeapons();
                SetAllPlayersHealth(Config.FlashbangStartHP);
                GiveAllPlayersFlashbangs();
                StartFlashbangSpamRound();
                RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
                break;
            case EventType.KnifeOnly:
                AnnounceEvent("Knife-Only Round", "Knives out! Pure melee combat.");
                StripAllWeapons();
                GiveAllPlayersKnives();
                break;
            case EventType.ZeusOnly:
                AnnounceEvent("Zeus-Only Round", "Zeus only. One zap and they're down!");
                StripAllWeapons();
                GiveAllPlayersZeus();
                Server.ExecuteCommand($"mp_taser_recharge_time {Config.ZeusRechargeTime}");
                break;
            case EventType.NoReload:
                AnnounceEvent("No Reload Round", "One magazine only. Make every bullet count!");
                AddTimer(0.5f, () => { ApplyNoReload(); });
                RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Post);
                break;
            case EventType.GravitySwitch:
                AnnounceEvent("Gravity Switch Round", "Gravity flips between low and high every 5 seconds!");
                StartGravitySwitch();
                StartGravityMonitor();
                break;
            case EventType.SpeedRandomizer:
                AnnounceEvent("Speed Randomizer Round", "Everyone moves at a different random speed!");
                RandomizeAllPlayersSpeed();
                break;
            case EventType.LastManStanding:
                AnnounceEvent("Last Man Standing Round", "Random pistol only. Survive!");
                StripAllWeapons();
                GiveAllPlayersPistols();
                break;
            case EventType.PowerUpRound:
                AnnounceEvent("Power-Up Round", "300 HP, full armor, and HE grenades. Go wild!");
                StripAllWeapons();
                SetAllPlayersHealth(Config.PowerUpHP);
                GiveAllPlayersFullArmor();
                GiveAllPlayersUnlimitedHE();
                RegisterEventHandler<EventWeaponFire>(OnHEFire, HookMode.Post);
                break;
            case EventType.ChaosRound:
                ApplyChaosRound();
                break;
        }

        _roundEventTriggered = true;
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

        if (_activeEvent == EventType.HeadshotOnly && @event.Hitgroup != 1)
        {
            // Not a headshot — heal back the damage
            int dmg = @event.DmgHealth;
            pawn.Health = Math.Min(pawn.Health + dmg, 100);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }

        if (_activeEvent == EventType.DoubleDamage || _chaosDoubleDamage)
        {
            // Apply extra damage based on multiplier (multiplier-1 because original damage already applied)
            int extraDamage = @event.DmgHealth * (Config.DoubleDamageMultiplier - 1);
            pawn.Health = Math.Max(0, pawn.Health - extraDamage);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            if (pawn.Health <= 0)
                pawn.CommitSuicide(false, true);
        }

        return HookResult.Continue;
    }

    private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !IsPlayerValid(player)) return HookResult.Continue;

        if (_activeEvent == EventType.FlashbangSpam && _random.Next(0, 100) < 10)
            GivePlayerFlashbang(player);

        return HookResult.Continue;
    }

    private HookResult OnHEFire(EventWeaponFire @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !IsPlayerValid(player)) return HookResult.Continue;

        AddTimer(1.0f, () =>
        {
            if (IsPlayerValid(player))
            {
                try { player.GiveNamedItem("weapon_hegrenade"); }
                catch { /* ignore */ }
            }
        });

        return HookResult.Continue;
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
        AddTimer(0.1f, () =>
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

    private HookResult OnItemPurchase(EventItemPurchase @event, GameEventInfo info)
    {
        // Purchase blocking is handled via convars (DisableBuying/EnableBuying)
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
        _playerSpeeds.Clear();

        if (_activeEvent == EventType.HeadshotOnly || _activeEvent == EventType.DoubleDamage || _activeEvent == EventType.ChaosRound)
            DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post);
        if (_activeEvent == EventType.FlashbangSpam)
            DeregisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
        if (_activeEvent == EventType.PowerUpRound)
            DeregisterEventHandler<EventWeaponFire>(OnHEFire, HookMode.Post);
        if (_activeEvent == EventType.NoReload)
            DeregisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Post);

        _chaosDoubleDamage = false;
        _currentGravity = 800.0f;
        SetGravity(800.0f);
        SetNospread(false);
        EnableBuying();
        Server.ExecuteCommand("mp_taser_recharge_time 30");
        ResetNoReload();
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
            for (int i = count; i < 2; i++)
            {
                try { player.GiveNamedItem("weapon_flashbang"); }
                catch { break; }
            }
        }
    }

    private void StartFlashbangSpamRound()
    {
        _flashbangSpamTimer?.Kill();
        _flashbangSpamTimer = AddTimer(Config.FlashbangRefillInterval, () =>
        {
            foreach (var player in Utilities.GetPlayers())
                if (IsPlayerValid(player) && _random.Next(0, 100) < 30) GivePlayerFlashbang(player);
        }, TimerFlags.REPEAT);
    }

    private void GivePlayerFlashbang(CCSPlayerController player)
    {
        if (!IsPlayerValid(player)) return;
        if (GetPlayerGrenadeCount(player, "weapon_flashbang") >= 2) return;
        try { player.GiveNamedItem("weapon_flashbang"); }
        catch { /* ignore */ }
    }

    private void SetAllPlayersHealth(int health)
    {
        health = Math.Clamp(health, 1, 300);
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

    private void GiveAllPlayersZeusOnly()
    {
        foreach (var player in Utilities.GetPlayers())
            if (IsPlayerValid(player)) player.GiveNamedItem("weapon_taser");
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

    private void RandomizeAllPlayersSpeed()
    {
        _playerSpeeds.Clear();
        foreach (var player in Utilities.GetPlayers())
        {
            if (!IsPlayerValid(player) || player.PlayerPawn.Value == null) continue;
            float speed = Config.SpeedMin + (float)(_random.NextDouble() * (Config.SpeedMax - Config.SpeedMin));
            _playerSpeeds[player.Slot] = speed;
            player.PlayerPawn.Value.VelocityModifier = speed;
            int percent = (int)(speed * 100);
            player.PrintToChat($" {ChatColors.Blue}[EVENT]{ChatColors.White} Your speed: {ChatColors.Green}{percent}%");
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

    private void ApplyChaosRound()
    {
        _chaosDoubleDamage = false;
        var mods = new List<string>();

        StripAllWeapons();

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
            RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post);
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

    private void ResetNoReload()
    {
        // No convar to reset — reserve ammo resets naturally on new round
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
    private void OnLowGravityCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _activeEvent = EventType.LowGravity;
        AnnounceEvent("Low Gravity Round", "Float around with a Scout and Zeus. Perfect accuracy!");
        SetGravity(Config.LowGravityValue);
        SetNospread(true);
        StartGravityMonitor();
        StripAllWeapons();
        GiveAllPlayersScout();
        GiveAllPlayersZeusOnly();
        Server.ExecuteCommand($"mp_taser_recharge_time {Config.ZeusRechargeTime}");
    }

    private void OnHeadshotOnlyCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _activeEvent = EventType.HeadshotOnly;
        AnnounceEvent("Juan Deag Round", "Deagle only, headshots only. One tap or nothing!");
        StripAllWeapons();
        GiveAllPlayersKnives();
        GiveAllPlayersDeagle();
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post);
    }

    private void OnRandomWeaponCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _activeEvent = EventType.RandomWeapon;
        AnnounceEvent("Random Weapon Round", "Everyone gets a random weapon. Good luck!");
        StripAllWeapons();
        GiveAllPlayersRandomWeapons();
    }

    private void OnDoubleDamageCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _activeEvent = EventType.DoubleDamage;
        AnnounceEvent("Double Damage Round", "All damage is doubled. Play it safe!");
        GiveAllPlayersGlock();
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt, HookMode.Post);
    }

    private void OnSwapTeamsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _activeEvent = EventType.SwapTeams;
        AnnounceEvent("Team Swap Round", "A random pair swaps teams every 30 seconds!");
        StartSwapTimer();
    }

    private void OnFlashbangSpamCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _activeEvent = EventType.FlashbangSpam;
        AnnounceEvent("Flashbang Spam Round", "1 HP, flashbangs only. One flash and you're dead!");
        StripAllWeapons();
        SetAllPlayersHealth(Config.FlashbangStartHP);
        GiveAllPlayersFlashbangs();
        StartFlashbangSpamRound();
        RegisterEventHandler<EventWeaponFire>(OnWeaponFire, HookMode.Post);
    }

    private void OnKnifeOnlyCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _activeEvent = EventType.KnifeOnly;
        AnnounceEvent("Knife-Only Round", "Knives out! Pure melee combat.");
        StripAllWeapons();
        GiveAllPlayersKnives();
    }

    private void OnZeusOnlyCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _activeEvent = EventType.ZeusOnly;
        AnnounceEvent("Zeus-Only Round", "Zeus only. One zap and they're down!");
        StripAllWeapons();
        GiveAllPlayersZeus();
        Server.ExecuteCommand($"mp_taser_recharge_time {Config.ZeusRechargeTime}");
    }

    private void OnNoReloadCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _activeEvent = EventType.NoReload;
        AnnounceEvent("No Reload Round", "One magazine only. Make every bullet count!");
        ApplyNoReload();
        RegisterEventHandler<EventItemPickup>(OnItemPickup, HookMode.Post);
    }

    private void OnGravitySwitchCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _activeEvent = EventType.GravitySwitch;
        AnnounceEvent("Gravity Switch Round", "Gravity flips between low and high every 5 seconds!");
        StartGravitySwitch();
        StartGravityMonitor();
    }

    private void OnSpeedRandomizerCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _activeEvent = EventType.SpeedRandomizer;
        AnnounceEvent("Speed Randomizer Round", "Everyone moves at a different random speed!");
        RandomizeAllPlayersSpeed();
    }

    private void OnLastManStandingCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _activeEvent = EventType.LastManStanding;
        AnnounceEvent("Last Man Standing Round", "Random pistol only. Survive!");
        StripAllWeapons();
        GiveAllPlayersPistols();
    }

    private void OnPowerUpRoundCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _activeEvent = EventType.PowerUpRound;
        AnnounceEvent("Power-Up Round", "300 HP, full armor, and HE grenades. Go wild!");
        StripAllWeapons();
        SetAllPlayersHealth(Config.PowerUpHP);
        GiveAllPlayersFullArmor();
        GiveAllPlayersUnlimitedHE();
        RegisterEventHandler<EventWeaponFire>(OnHEFire, HookMode.Post);
    }

    private void OnChaosRoundCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!IsAdmin(player)) return;
        ResetAllState();
        _activeEvent = EventType.ChaosRound;
        DisableBuying();
        ApplyChaosRound();
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
