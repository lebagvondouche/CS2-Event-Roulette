using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RandomRoundEvents;

internal static class Players
{
    internal static bool IsValid(CCSPlayerController player)
    {
        return player.IsValid && player.PawnIsAlive && player.PlayerPawn.Value != null;
    }

    internal static void RestoreHealth(CCSPlayerPawn pawn, int amount, int maxHealth)
    {
        pawn.Health = Math.Min(pawn.Health + amount, maxHealth);
        if (pawn.LifeState != (byte)0 && pawn.Health > 0)
        {
            pawn.LifeState = (byte)0;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_lifeState");
        }

        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }

    internal static bool HasWeapon(CCSPlayerController player, string weaponName)
    {
        if (!player.IsValid || player.PlayerPawn.Value == null)
            return false;

        var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
        if (weapons == null)
            return false;

        foreach (var weaponHandle in weapons)
        {
            var weapon = weaponHandle.Value;
            if (weapon?.DesignerName == weaponName)
                return true;
        }

        return false;
    }

    internal static bool CanReceiveGrenadeRefill(CCSPlayerController player)
    {
        if (!player.IsValid || player.PlayerPawn.Value == null)
            return false;

        var pawn = player.PlayerPawn.Value;
        return player.PawnIsAlive && pawn.Health > 0 && pawn.LifeState == 0;
    }

    internal static int GetGrenadeCount(CCSPlayerController player, string grenadeName)
    {
        if (player.PlayerPawn.Value == null)
            return 1;

        var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
        if (weapons == null)
            return 1;

        int count = 0;
        foreach (var weapon in weapons)
        {
            if (weapon.Value != null && weapon.Value.DesignerName == grenadeName)
                count++;
        }

        return count;
    }

    internal static float Distance2D(Vector a, Vector b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    internal static float Distance3D(Vector a, Vector b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }
}
