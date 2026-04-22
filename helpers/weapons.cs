using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RandomRoundEvents;

internal sealed class Weapons
{
    private readonly RandomRoundEvents _plugin;

    public Weapons(RandomRoundEvents plugin)
    {
        _plugin = plugin;
    }

    internal void StripAllWeapons()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            StripPlayerWeapons(player);
        }

        GiveAllPlayersFullArmor();
    }

    internal void GiveAllPlayersRandomWeapons()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.IsValid(player))
                continue;

            string weapon = _plugin.RandomWeaponNames[_plugin.Random.Next(0, _plugin.RandomWeaponNames.Count)];
            TryGiveRandomWeapon(player, weapon);

            _plugin.AddTimer(0.1f, () =>
            {
                if (!Players.IsValid(player) || Players.HasWeapon(player, weapon))
                    return;

                TryGiveRandomWeapon(player, weapon);
            });

            _plugin.AddTimer(0.25f, () =>
            {
                if (!Players.IsValid(player) || Players.HasWeapon(player, weapon))
                    return;

                TryGiveRandomWeapon(player, weapon);
            });
        }
    }

    private void TryGiveRandomWeapon(CCSPlayerController player, string weapon)
    {
        try
        {
            player.GiveNamedItem(weapon);
        }
        catch (Exception ex)
        {
            _plugin.LogPluginWarning("[RandomRoundEvents] Failed to give weapon to {Player}: {Error}", player.PlayerName, ex.Message);
        }
    }

    internal void GiveAllPlayersStandardLoadout()
    {
        StripAllWeapons();
        GiveAllPlayersKnives();
        GiveAllPlayersDefaultPistolsWithRetry();
    }

    internal static void GiveAllPlayersDefaultPistols()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.IsValid(player))
                continue;

            GivePlayerDefaultPistol(player);
        }
    }

    internal static void GivePlayerDefaultPistol(CCSPlayerController player)
    {
        if (!Players.IsValid(player))
            return;

        string pistol = player.Team == CsTeam.CounterTerrorist
            ? "weapon_usp_silencer"
            : "weapon_glock";

        try { player.GiveNamedItem(pistol); }
        catch { }
    }

    internal void GiveAllPlayersFlashbangs()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.IsValid(player))
                continue;

            if (Players.GetGrenadeCount(player, "weapon_flashbang") < 1)
            {
                try { player.GiveNamedItem("weapon_flashbang"); }
                catch { }
            }
        }
    }

    internal void GiveAllPlayersSmokes()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.IsValid(player))
                continue;

            if (Players.GetGrenadeCount(player, "weapon_smokegrenade") >= 1)
                continue;

            try { player.GiveNamedItem("weapon_smokegrenade"); }
            catch { }
        }
    }

    internal void GivePlayerFlashbang(CCSPlayerController player)
    {
        if (!Players.IsValid(player))
            return;

        if (Players.GetGrenadeCount(player, "weapon_flashbang") >= 1)
            return;

        try { player.GiveNamedItem("weapon_flashbang"); }
        catch { }
    }

    internal void SetAllPlayersHealth(int health)
    {
        health = Math.Clamp(health, 1, 1000);
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.IsValid(player) || player.PlayerPawn.Value == null)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (health > 100)
                pawn.MaxHealth = health;
            pawn.Health = health;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
        }
    }

    internal void GiveAllPlayersKnives()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.IsValid(player))
                continue;

            TryGiveKnife(player);

            _plugin.AddTimer(0.1f, () =>
            {
                if (!Players.IsValid(player) || Players.HasWeapon(player, "weapon_knife"))
                    return;

                TryGiveKnife(player);
            });

            _plugin.AddTimer(0.25f, () =>
            {
                if (!Players.IsValid(player) || Players.HasWeapon(player, "weapon_knife"))
                    return;

                TryGiveKnife(player);
            });
        }
    }

    internal void GiveAllPlayersZeus()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.IsValid(player))
                continue;

            TryGiveZeus(player);

            _plugin.AddTimer(0.15f, () =>
            {
                if (!Players.IsValid(player) || Players.HasWeapon(player, "weapon_taser"))
                    return;

                TryGiveZeus(player);
            });
        }
    }

    internal void GiveAllPlayersPistols()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.IsValid(player))
                continue;

            string pistol = _plugin.PistolNames[_plugin.Random.Next(0, _plugin.PistolNames.Count)];
            TryGivePistol(player, pistol);

            _plugin.AddTimer(0.1f, () =>
            {
                if (!Players.IsValid(player) || Players.HasWeapon(player, pistol))
                    return;

                TryGivePistol(player, pistol);
            });

            _plugin.AddTimer(0.25f, () =>
            {
                if (!Players.IsValid(player) || Players.HasWeapon(player, pistol))
                    return;

                TryGivePistol(player, pistol);
            });
        }
    }

    private void GiveAllPlayersDefaultPistolsWithRetry()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.IsValid(player))
                continue;

            string pistol = player.Team == CsTeam.CounterTerrorist
                ? "weapon_usp_silencer"
                : "weapon_glock";

            TryGivePistol(player, pistol);

            _plugin.AddTimer(0.1f, () =>
            {
                if (!Players.IsValid(player) || Players.HasWeapon(player, pistol))
                    return;

                TryGivePistol(player, pistol);
            });

            _plugin.AddTimer(0.25f, () =>
            {
                if (!Players.IsValid(player) || Players.HasWeapon(player, pistol))
                    return;

                TryGivePistol(player, pistol);
            });
        }
    }

    private static void TryGiveKnife(CCSPlayerController player)
    {
        try { player.GiveNamedItem("weapon_knife"); }
        catch { }
    }

    private static void TryGivePistol(CCSPlayerController player, string pistol)
    {
        try { player.GiveNamedItem(pistol); }
        catch { }
    }

    internal void GiveAllPlayersScout()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (Players.IsValid(player))
                player.GiveNamedItem("weapon_ssg08");
        }
    }

    internal static void GiveAllPlayersGlock()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (Players.IsValid(player))
                player.GiveNamedItem("weapon_glock");
        }
    }

    internal static void GiveAllPlayersAK47()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (Players.IsValid(player))
                player.GiveNamedItem("weapon_ak47");
        }
    }

    internal static void ZeroAllReserveAmmo()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.IsValid(player) || player.PlayerPawn.Value == null)
                continue;

            var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
            if (weapons == null)
                continue;

            foreach (var weaponHandle in weapons)
            {
                var weapon = weaponHandle.Value;
                if (weapon == null)
                    continue;

                ZeroReserveAmmo(weapon);
            }
        }
    }

    internal static void ZeroReserveAmmo(CBasePlayerWeapon weapon)
    {
        weapon.ReserveAmmo[0] = 0;

        try
        {
            weapon.ReserveAmmo[1] = 0;
        }
        catch
        {
            // Some weapons expose only one reserve slot.
        }

        try
        {
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
        }
        catch
        {
            // Best-effort client sync only.
        }
    }

    internal static void GiveAllPlayersDeagle()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (Players.IsValid(player))
                player.GiveNamedItem("weapon_deagle");
        }
    }

    internal void GiveAllPlayersShotgun()
    {
        string[] shotguns = { "weapon_nova", "weapon_xm1014", "weapon_mag7" };
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.IsValid(player))
                continue;

            string shotgun = shotguns[_plugin.Random.Next(shotguns.Length)];
            player.GiveNamedItem(shotgun);
        }
    }

    internal void GiveAllPlayersXM1014()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.IsValid(player))
                continue;

            player.GiveNamedItem("weapon_xm1014");
        }
    }

    internal void GiveAllPlayersFullArmor()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (Players.IsValid(player))
                player.GiveNamedItem("item_assaultsuit");
        }
    }

    internal void GiveAllPlayersUnlimitedHE()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.CanReceiveGrenadeRefill(player))
                continue;

            if (Players.GetGrenadeCount(player, "weapon_hegrenade") < 1)
            {
                try { player.GiveNamedItem("weapon_hegrenade"); }
                catch { }
            }
        }
    }

    internal static void GiveAllPlayersMolotov()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.CanReceiveGrenadeRefill(player))
                continue;

            if (Players.GetGrenadeCount(player, "weapon_molotov") < 1 &&
                Players.GetGrenadeCount(player, "weapon_incgrenade") < 1)
            {
                try { player.GiveNamedItem("weapon_molotov"); }
                catch { }
            }
        }
    }

    internal void GiveAllPlayersGrenadeRouletteGrenades()
    {
        foreach (var player in RandomRoundEvents.GetPlayers())
        {
            if (!Players.IsValid(player))
                continue;

            TryGiveItem(player, "weapon_hegrenade");
            TryGiveItem(player, "weapon_flashbang");
            TryGiveItem(player, "weapon_smokegrenade");
            TryGiveItem(player, "weapon_decoy");
            TryGiveItem(player, player.Team == CsTeam.CounterTerrorist ? "weapon_incgrenade" : "weapon_molotov");
        }
    }

    private void StripPlayerWeapons(CCSPlayerController player)
    {
        if (!Players.IsValid(player) || player.PlayerPawn.Value == null)
            return;

        var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
        if (weapons == null)
            return;

        var weaponsToRemove = new List<CBasePlayerWeapon>();
        foreach (var weaponHandle in weapons)
        {
            var weapon = weaponHandle.Value;
            if (weapon == null)
                continue;

            if (_plugin.Config.EnableBomb && weapon.DesignerName == "weapon_c4")
                continue;

            weaponsToRemove.Add(weapon);
        }

        foreach (var weapon in weaponsToRemove)
        {
            try
            {
                player.PlayerPawn.Value.RemovePlayerItem(weapon);
                weapon.Remove();
            }
            catch (Exception ex)
            {
                _plugin.LogPluginWarning("[RandomRoundEvents] Failed to remove weapon {Weapon} from {Player}: {Error}", weapon.DesignerName, player.PlayerName, ex.Message);
            }
        }
    }

    private static void TryGiveZeus(CCSPlayerController player)
    {
        try { player.GiveNamedItem("weapon_taser"); }
        catch { }
    }

    private void TryGiveItem(CCSPlayerController player, string itemName)
    {
        try
        {
            player.GiveNamedItem(itemName);
        }
        catch (Exception ex)
        {
            _plugin.LogPluginWarning("[RandomRoundEvents] Failed to give {Item} to {Player}: {Error}", itemName, player.PlayerName, ex.Message);
        }
    }
}
