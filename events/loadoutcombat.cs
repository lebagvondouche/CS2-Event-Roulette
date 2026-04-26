using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace RandomRoundEvents;

internal sealed class LoadoutCombat
{
    private readonly RandomRoundEvents _plugin;

    public LoadoutCombat(RandomRoundEvents plugin)
    {
        _plugin = plugin;
    }

    public void Apply(RandomRoundEvents.EventType eventType)
    {
        switch (eventType)
        {
            case RandomRoundEvents.EventType.HeadshotOnly:
                _plugin.ShowEvent("Juan Deag Round", "Deagle only, headshots only. One tap or nothing!");
                _plugin.EnsurePlayerHurtHandler();
                _plugin.StripAllWeapons();
                _plugin.GiveAllPlayersKnives();
                RandomRoundEvents.GiveAllPlayersDeagle();
                break;
            case RandomRoundEvents.EventType.RandomWeapon:
                _plugin.ShowEvent("Random Weapon Round", "Everyone gets a random weapon. Good luck!");
                _plugin.SetConVar("mp_death_drop_gun", 1);
                _plugin.StripAllWeapons();
                _plugin.GiveAllPlayersKnives();
                _plugin.GiveAllPlayersRandomWeapons();
                break;
            case RandomRoundEvents.EventType.DoubleDamage:
                _plugin.ShowEvent("Double Damage Round", "All damage is doubled. Play it safe!");
                _plugin.EnsurePlayerHurtHandler();
                _plugin.StripAllWeapons();
                RandomRoundEvents.GiveAllPlayersGlock();
                break;
            case RandomRoundEvents.EventType.FlashbangSpam:
                _plugin.ShowEvent("Flashbang Spam Round", "1 HP, flashbangs only. Knife does no damage!");
                _plugin.StartFlashbangSpamRound();
                _plugin.EnsurePlayerHurtHandler();
                _plugin.StripAllWeapons();
                _plugin.GiveAllPlayersKnives();
                foreach (var player in RandomRoundEvents.GetPlayers())
                {
                    if (!RandomRoundEvents.IsValidAlivePlayer(player) || player.PlayerPawn.Value == null)
                        continue;

                    var pawn = player.PlayerPawn.Value;
                    pawn.ArmorValue = 0;
                    Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
                }
                _plugin.SetAllPlayersHealth(_plugin.Config.FlashbangStartHP);
                _plugin.GiveAllPlayersFlashbangs();
                break;
            case RandomRoundEvents.EventType.KnifeOnly:
                _plugin.ShowEvent("Knife-Only Round", "Knives out! Bhop enabled!");
                _plugin.SetBhop(true);
                _plugin.StripAllWeapons();
                _plugin.GiveAllPlayersKnives();
                break;
            case RandomRoundEvents.EventType.ZeusOnly:
                _plugin.ShowEvent("Zeus-Only Round", "Zeus only. Bhop enabled!");
                _plugin.SetBhop(true);
                _plugin.SetConVar("mp_weapons_allow_zeus", -1);
                _plugin.SetConVar("mp_taser_recharge_time", _plugin.Config.ZeusRechargeTime);
                _plugin.StripAllWeapons();
                _plugin.GiveAllPlayersKnives();
                _plugin.GiveAllPlayersZeus();
                break;
            case RandomRoundEvents.EventType.NoReload:
                _plugin.ShowEvent("No Reload Round", "AK only. One magazine only. Make every bullet count!");
                _plugin.StripAllWeapons();
                _plugin.GiveAllPlayersKnives();
                RandomRoundEvents.GiveAllPlayersAK47();
                RandomRoundEvents.ZeroAllReserveAmmo();
                _plugin.EnsureItemPickupHandler();
                _plugin.ApplyNoReload();
                _plugin.StartNoReloadTimer();
                break;
            case RandomRoundEvents.EventType.LastManStanding:
                _plugin.ShowEvent("Last Man Standing Round", "Random pistol only. Survive!");
                _plugin.StripAllWeapons();
                _plugin.GiveAllPlayersKnives();
                _plugin.GiveAllPlayersPistols();
                break;
            case RandomRoundEvents.EventType.PowerUpRound:
                _plugin.ShowEvent("Power-Up Round", $"{_plugin.Config.PowerUpHP} HP, full armor, unlimited HE. Knife does no damage!");
                _plugin.SetConVar("mp_friendlyfire", 0);
                _plugin.SetConVar("mp_death_drop_gun", 0);
                _plugin.StartHERefillTimer();
                _plugin.EnsurePlayerHurtHandler();
                _plugin.StripAllWeapons();
                _plugin.GiveAllPlayersKnives();
                _plugin.SetAllPlayersHealth(_plugin.Config.PowerUpHP);
                _plugin.GiveAllPlayersFullArmor();
                _plugin.GiveAllPlayersUnlimitedHE();
                RandomRoundEvents.GiveAllPlayersMolotov();
                break;
            case RandomRoundEvents.EventType.TankRound:
                _plugin.ShowEvent("Tank Round", $"{_plugin.Config.TankHP} HP, full armor, XM1014 only. Unlimited ammo!");
                _plugin.StripAllWeapons();
                _plugin.GiveAllPlayersKnives();
                _plugin.SetAllPlayersHealth(_plugin.Config.TankHP);
                _plugin.GiveAllPlayersFullArmor();
                _plugin.GiveAllPlayersXM1014();
                _plugin.SetConVar("sv_infinite_ammo", 2);
                break;
            case RandomRoundEvents.EventType.VampireRound:
                _plugin.ShowEvent("Vampire Round", $"Damage dealt heals you! Max {_plugin.Config.VampireMaxHP} HP. Pistols only!");
                _plugin.EnsurePlayerHurtHandler();
                _plugin.StripAllWeapons();
                _plugin.GiveAllPlayersKnives();
                _plugin.GiveAllPlayersPistols();
                break;
            case RandomRoundEvents.EventType.ReturnToSenderRound:
                _plugin.ShowEvent("Return to Sender Round", "Hit someone and they teleport back to spawn! Pistols only!");
                _plugin.EnsurePlayerHurtHandler();
                _plugin.StripAllWeapons();
                _plugin.GiveAllPlayersKnives();
                _plugin.GiveAllPlayersPistols();
                break;
            case RandomRoundEvents.EventType.ScreenShakeRound:
                _plugin.ShowEvent("Screen Shake Round", "Normal buy round, but everyone gets hit by constant shake pulses!");
                _plugin.GiveAllPlayersStandardLoadout();
                _plugin.StartScreenShakeRound();
                break;
        }
    }

    public void Reset()
    {
        _plugin.ResetLoadoutCombatState();
    }
}
