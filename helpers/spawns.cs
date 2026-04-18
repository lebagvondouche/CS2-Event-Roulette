using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RandomRoundEvents;

internal sealed class Spawns
{
    private readonly RandomRoundEvents _plugin;

    public Spawns(RandomRoundEvents plugin)
    {
        _plugin = plugin;
    }

    internal void TeleportToSpawn(CCSPlayerController victim)
    {
        if (victim.PlayerPawn.Value == null)
            return;

        string spawnName = victim.Team == CsTeam.CounterTerrorist
            ? "info_player_counterterrorist"
            : "info_player_terrorist";

        var spawns = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(spawnName).ToList();
        if (spawns.Count == 0)
            spawns = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_player_start").ToList();
        if (spawns.Count == 0)
            return;

        foreach (var spawn in spawns.OrderBy(_ => _plugin.Random.Next()))
        {
            if (spawn.AbsOrigin == null)
                continue;

            if (TryTeleportToClearPosition(victim, spawn.AbsOrigin, spawn.AbsRotation))
                return;
        }

        var fallback = spawns[_plugin.Random.Next(spawns.Count)];
        if (fallback.AbsOrigin != null)
            victim.PlayerPawn.Value.Teleport(fallback.AbsOrigin, fallback.AbsRotation, victim.PlayerPawn.Value.AbsVelocity);
    }

    private bool TryTeleportToClearPosition(CCSPlayerController player, Vector origin, QAngle? rotation)
    {
        foreach (var candidate in GetSpawnCandidates(origin))
        {
            if (!IsPositionOccupied(candidate, player))
            {
                player.PlayerPawn.Value?.Teleport(candidate, rotation, player.PlayerPawn.Value.AbsVelocity);
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<Vector> GetSpawnCandidates(Vector origin)
    {
        yield return origin;
        yield return origin + new Vector(72, 0, 0);
        yield return origin + new Vector(-72, 0, 0);
        yield return origin + new Vector(0, 72, 0);
        yield return origin + new Vector(0, -72, 0);
        yield return origin + new Vector(72, 72, 0);
        yield return origin + new Vector(72, -72, 0);
        yield return origin + new Vector(-72, 72, 0);
        yield return origin + new Vector(-72, -72, 0);
    }

    private static bool IsPositionOccupied(Vector target, CCSPlayerController teleportedPlayer)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player == teleportedPlayer || player.PlayerPawn.Value?.AbsOrigin == null)
                continue;

            if (Players.Distance2D(player.PlayerPawn.Value.AbsOrigin, target) < 56.0f)
                return true;
        }

        return false;
    }
}
