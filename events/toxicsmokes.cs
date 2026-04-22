using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.UserMessages;

namespace RandomRoundEvents;

internal sealed class ToxicSmokes
{
    private sealed class PendingSmoke
    {
        public required CSmokeGrenadeProjectile Projectile { get; init; }
    }

    private sealed class ActiveCloud
    {
        public required Vector Position { get; init; }
        public required float ExpiresAt { get; init; }
    }

    private static readonly IReadOnlyList<Vector> ToxicPalette =
    [
        new Vector(32.0f, 255.0f, 32.0f),
        new Vector(64.0f, 255.0f, 64.0f),
        new Vector(96.0f, 255.0f, 32.0f),
        new Vector(128.0f, 255.0f, 64.0f)
    ];

    private readonly RandomRoundEvents _plugin;
    private readonly List<PendingSmoke> _pendingSmokes = [];
    private readonly List<ActiveCloud> _activeClouds = [];
    private readonly Dictionary<int, float> _nextDebuffCueAt = [];
    private CounterStrikeSharp.API.Modules.Timers.Timer? _updateTimer;
    private bool _listenerRegistered;
    private float _nextDamageTickAt;

    public ToxicSmokes(RandomRoundEvents plugin)
    {
        _plugin = plugin;
    }

    public void Apply()
    {
        _plugin.ShowEvent("Toxic Green Smokes Round", "Normal buy round. Smokes bloom toxic green and damage anyone standing inside them!");
        _plugin.GiveAllPlayersStandardLoadout();
        _plugin.GiveAllPlayersSmokes();
        _pendingSmokes.Clear();
        _activeClouds.Clear();
        _nextDebuffCueAt.Clear();
        _nextDamageTickAt = Server.CurrentTime + _plugin.Config.ToxicSmokeTickInterval;
        EnsureListenerRegistered();
        EnsureUpdateTimer();
    }

    public void Reset()
    {
        _pendingSmokes.Clear();
        _activeClouds.Clear();
        _nextDebuffCueAt.Clear();
        _updateTimer?.Kill();
        _updateTimer = null;

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

    private void EnsureUpdateTimer()
    {
        if (_updateTimer != null)
            return;

        _updateTimer = _plugin.AddTimer(0.1f, UpdateSmokeState, TimerFlags.REPEAT);
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (!_plugin.IsToxicSmokesRoundActive || entity.DesignerName != "smokegrenade_projectile")
            return;

        var projectile = new CSmokeGrenadeProjectile(entity.Handle);
        Server.NextFrame(() =>
        {
            if (!projectile.IsValid)
            {
                if (_plugin.Config.Debug)
                    _plugin.LogToxicSmokesWarning("[RandomRoundEvents] Toxic Smokes projectile became invalid before setup could complete.");
                return;
            }

            var color = ToxicPalette[_plugin.Random.Next(ToxicPalette.Count)];
            projectile.SmokeColor.X = color.X;
            projectile.SmokeColor.Y = color.Y;
            projectile.SmokeColor.Z = color.Z;
            _pendingSmokes.Add(new PendingSmoke { Projectile = projectile });

            if (_plugin.Config.Debug)
            {
                _plugin.LogToxicSmokesInfo(
                    "[RandomRoundEvents] Toxic Smokes applied color ({R:F0}, {G:F0}, {B:F0})",
                    color.X,
                    color.Y,
                    color.Z);
            }
        });
    }

    private void UpdateSmokeState()
    {
        PromotePendingSmokes();
        ExpireClouds();

        if (_activeClouds.Count == 0 || Server.CurrentTime < _nextDamageTickAt)
            return;

        ApplyToxicDamage();
        _nextDamageTickAt = Server.CurrentTime + _plugin.Config.ToxicSmokeTickInterval;
    }

    private void PromotePendingSmokes()
    {
        for (int i = _pendingSmokes.Count - 1; i >= 0; i--)
        {
            var pending = _pendingSmokes[i];
            var projectile = pending.Projectile;

            if (!projectile.IsValid)
            {
                _pendingSmokes.RemoveAt(i);
                continue;
            }

            if (!projectile.DidSmokeEffect && projectile.SmokeEffectTickBegin <= 0)
                continue;

            var detonationPos = projectile.SmokeDetonationPos;
            Vector cloudPosition = (detonationPos.X != 0.0f || detonationPos.Y != 0.0f || detonationPos.Z != 0.0f)
                ? new Vector(detonationPos.X, detonationPos.Y, detonationPos.Z)
                : projectile.AbsOrigin ?? new Vector(0.0f, 0.0f, 0.0f);

            _activeClouds.Add(new ActiveCloud
            {
                Position = cloudPosition,
                ExpiresAt = Server.CurrentTime + _plugin.Config.ToxicSmokeDuration
            });

            if (_plugin.Config.Debug)
            {
                _plugin.LogToxicSmokesInfo(
                    "[RandomRoundEvents] Toxic Smokes cloud activated at ({X:F0}, {Y:F0}, {Z:F0}) until {ExpiresAt:F2}",
                    cloudPosition.X,
                    cloudPosition.Y,
                    cloudPosition.Z,
                    Server.CurrentTime + _plugin.Config.ToxicSmokeDuration);
            }

            _pendingSmokes.RemoveAt(i);
        }
    }

    private void ExpireClouds()
    {
        float now = Server.CurrentTime;
        _activeClouds.RemoveAll(cloud => cloud.ExpiresAt <= now);
    }

    private void ApplyToxicDamage()
    {
        float radiusSquared = _plugin.Config.ToxicSmokeRadius * _plugin.Config.ToxicSmokeRadius;
        int damagePerCloud = _plugin.Config.ToxicSmokeDamagePerTick;

        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!RandomRoundEvents.IsValidAlivePlayer(player) || player.PlayerPawn.Value?.AbsOrigin == null)
                continue;

            var pawn = player.PlayerPawn.Value;
            var origin = pawn.AbsOrigin!;
            int cloudHits = 0;

            foreach (var cloud in _activeClouds)
            {
                float dx = origin.X - cloud.Position.X;
                float dy = origin.Y - cloud.Position.Y;
                float dz = origin.Z - cloud.Position.Z;
                float distanceSquared = dx * dx + dy * dy + dz * dz;
                if (distanceSquared <= radiusSquared)
                    cloudHits++;
            }

            if (cloudHits == 0)
                continue;

            int totalDamage = damagePerCloud * cloudHits;
            int newHealth = pawn.Health - totalDamage;
            if (newHealth > 0)
            {
                pawn.Health = newHealth;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
            }
            else
            {
                try
                {
                    player.CommitSuicide(false, true);
                }
                catch
                {
                    pawn.Health = 0;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                }
            }

            if (!player.IsBot)
                ApplyToxicDebuffCue(player, totalDamage);
        }
    }

    private void ApplyToxicDebuffCue(CCSPlayerController player, int totalDamage)
    {
        float now = Server.CurrentTime;
        if (_nextDebuffCueAt.TryGetValue(player.Slot, out float nextCueAt) && now < nextCueAt)
            return;

        _nextDebuffCueAt[player.Slot] = now + _plugin.Config.ToxicSmokeDebuffCueInterval;
        player.PrintToCenterAlert($"Poisoned! -{totalDamage} HP");

        try
        {
            var shake = UserMessage.FromPartialName("Shake");
            shake.SetFloat("duration", _plugin.Config.ToxicSmokeShakeDuration);
            shake.SetFloat("amplitude", _plugin.Config.ToxicSmokeShakeAmplitude);
            shake.SetFloat("frequency", _plugin.Config.ToxicSmokeShakeFrequency);
            shake.SetInt("command", (int)ShakeCommand_t.SHAKE_START);
            shake.Recipients.Add(player);
            shake.Send();
        }
        catch (Exception ex)
        {
            if (_plugin.Config.Debug)
                _plugin.LogToxicSmokesWarning("[RandomRoundEvents] Failed to send toxic debuff shake to {Player}: {Error}", player.PlayerName, ex.Message);
        }
    }
}
