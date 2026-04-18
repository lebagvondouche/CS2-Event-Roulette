using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RandomRoundEvents;

internal sealed class Respawn
{
    private readonly RandomRoundEvents _plugin;
    private readonly HashSet<int> _pendingRespawnLoadouts = [];
    private bool _deathHandlerRegistered;
    private bool _spawnHandlerRegistered;
    private int _tRespawns;
    private int _ctRespawns;
    private bool _tOutAnnounced;
    private bool _ctOutAnnounced;

    public Respawn(RandomRoundEvents plugin)
    {
        _plugin = plugin;
    }

    public bool IsActive { get; private set; }

    public void Apply()
    {
        _plugin.ShowEvent("Respawn Round", $"Each team has {_plugin.Config.RespawnPool} shared respawns!");
        IsActive = true;
        _tRespawns = _plugin.Config.RespawnPool;
        _ctRespawns = _plugin.Config.RespawnPool;
        _tOutAnnounced = false;
        _ctOutAnnounced = false;
        _plugin.SetConVar("mp_death_drop_gun", 0);
        _plugin.SetConVar("mp_respawn_on_death_t", 1);
        _plugin.SetConVar("mp_respawn_on_death_ct", 1);
        _plugin.SetConVar("mp_respawnwavetime_ct", 0);
        _plugin.SetConVar("mp_respawnwavetime_t", 0);
        _plugin.SetConVar("mp_randomspawn", 1);
        _plugin.SetConVar("mp_randomspawn_los", 1);
        _plugin.SetConVar("mp_buytime", 9999);
        _plugin.GiveAllPlayersStandardLoadout();
        _plugin.GiveAllPlayersRandomWeapons();

        if (!_deathHandlerRegistered)
        {
            _plugin.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
            _deathHandlerRegistered = true;
        }

        if (!_spawnHandlerRegistered)
        {
            _plugin.RegisterEventHandler<EventPlayerSpawn>(OnRespawnSpawn, HookMode.Post);
            _spawnHandlerRegistered = true;
        }
    }

    public void Reset()
    {
        if (_deathHandlerRegistered)
        {
            _plugin.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
            _deathHandlerRegistered = false;
        }

        if (_spawnHandlerRegistered)
        {
            _plugin.DeregisterEventHandler<EventPlayerSpawn>(OnRespawnSpawn, HookMode.Post);
            _spawnHandlerRegistered = false;
        }

        _pendingRespawnLoadouts.Clear();
        _tRespawns = 0;
        _ctRespawns = 0;
        _tOutAnnounced = false;
        _ctOutAnnounced = false;
        IsActive = false;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (!IsActive)
            return HookResult.Continue;

        var victim = @event.Userid;
        if (victim == null || !victim.IsValid)
            return HookResult.Continue;

        if (victim.Team == CsTeam.Terrorist)
        {
            if (_tRespawns <= 0)
                return HookResult.Continue;

            _pendingRespawnLoadouts.Add(victim.Slot);
            _tRespawns = Math.Max(0, _tRespawns - 1);
            if (_tRespawns == 0)
            {
                _plugin.SetConVar("mp_respawn_on_death_t", 0);
                if (!_tOutAnnounced)
                {
                    _tOutAnnounced = true;
                    Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.Red} T team has no respawns left!");
                }
            }
            else
            {
                Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.White} T respawns remaining: {ChatColors.Green}{_tRespawns}");
            }
        }
        else if (victim.Team == CsTeam.CounterTerrorist)
        {
            if (_ctRespawns <= 0)
                return HookResult.Continue;

            _pendingRespawnLoadouts.Add(victim.Slot);
            _ctRespawns = Math.Max(0, _ctRespawns - 1);
            if (_ctRespawns == 0)
            {
                _plugin.SetConVar("mp_respawn_on_death_ct", 0);
                if (!_ctOutAnnounced)
                {
                    _ctOutAnnounced = true;
                    Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.Red} CT team has no respawns left!");
                }
            }
            else
            {
                Server.PrintToChatAll($" {ChatColors.Blue}[EVENT]{ChatColors.White} CT respawns remaining: {ChatColors.Green}{_ctRespawns}");
            }
        }
        else if (_plugin.Config.Debug)
        {
            _plugin.LogPluginWarning("[RandomRoundEvents] Respawn death event for unexpected team {Team} on {Player}", victim.Team, victim.PlayerName);
        }

        return HookResult.Continue;
    }

    private HookResult OnRespawnSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (!IsActive)
            return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || !_pendingRespawnLoadouts.Remove(player.Slot))
            return HookResult.Continue;

        _plugin.AddTimer(1.0f, () =>
        {
            if (!player.IsValid || !player.PawnIsAlive || player.PlayerPawn.Value == null)
                return;

            _plugin.TeleportToSpawn(player);
            try { player.GiveNamedItem("weapon_knife"); } catch { }
            RandomRoundEvents.GivePlayerDefaultPistol(player);

            string weapon = _plugin.RandomWeaponNames[_plugin.Random.Next(_plugin.RandomWeaponNames.Count)];
            try { player.GiveNamedItem(weapon); } catch { }
        });

        return HookResult.Continue;
    }
}
