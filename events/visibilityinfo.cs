using CounterStrikeSharp.API.Core;

namespace RandomRoundEvents;

internal sealed class VisibilityInfo
{
    private readonly RandomRoundEvents _plugin;
    private bool _checkTransmitRegistered;
    private bool _invisibleTransmitActive;

    public VisibilityInfo(RandomRoundEvents plugin)
    {
        _plugin = plugin;
    }

    public void Apply(RandomRoundEvents.EventType eventType)
    {
        switch (eventType)
        {
            case RandomRoundEvents.EventType.InvisibleRound:
                _plugin.ShowEvent("Invisible Round", "Everyone is invisible! Knife and Zeus only, no friendly fire!");
                _plugin.SetConVar("mp_friendlyfire", 0);
                _plugin.SetConVar("mp_weapons_allow_zeus", -1);
                _plugin.SetConVar("mp_taser_recharge_time", _plugin.Config.ZeusRechargeTime);
                _plugin.StripAllWeapons();
                _plugin.GiveAllPlayersKnives();
                _plugin.GiveAllPlayersZeus();
                _plugin.SetAllPlayersInvisible();
                _plugin.SetAllPlayerWeaponsInvisible();
                EnableInvisibleTransmitHiding();
                break;
            case RandomRoundEvents.EventType.ZoomRound:
                _plugin.ShowEvent("Inception Round", "Tunnel vision! Everyone has a random FOV!");
                _plugin.GiveAllPlayersStandardLoadout();
                _plugin.RandomizeAllPlayersFOV();
                break;
            case RandomRoundEvents.EventType.GlowRound:
                _plugin.ShowEvent("X-Ray Goggles Round", "Everyone glows through walls! No hiding!");
                _plugin.GiveAllPlayersStandardLoadout();
                _plugin.ApplyGlowToAllPlayers();
                break;
            case RandomRoundEvents.EventType.SizeRound:
                _plugin.ShowEvent("Size Randomizer Round", "Everyone is a random size! HP scales with size!");
                _plugin.GiveAllPlayersStandardLoadout();
                _plugin.RandomizeAllPlayersSizes();
                break;
        }
    }

    public void Reset()
    {
        _invisibleTransmitActive = false;

        if (_checkTransmitRegistered)
        {
            _plugin.RemoveListener<Listeners.CheckTransmit>(OnCheckTransmit);
            _checkTransmitRegistered = false;
        }

        _plugin.ResetVisibilityInfoState();
    }

    private void EnableInvisibleTransmitHiding()
    {
        _invisibleTransmitActive = true;

        if (_checkTransmitRegistered)
            return;

        _plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
        _checkTransmitRegistered = true;
    }

    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (!_invisibleTransmitActive)
            return;

        foreach (var (info, viewer) in infoList)
        {
            if (viewer == null || !viewer.IsValid)
                continue;

            foreach (var player in RandomRoundEvents.GetPlayers())
            {
                if (!RandomRoundEvents.IsValidAlivePlayer(player) || player == viewer || player.PlayerPawn.Value == null)
                    continue;

                var pawn = player.PlayerPawn.Value;
                info.TransmitEntities.Remove(pawn);
                info.TransmitAlways.Remove(pawn);

                var weapons = pawn.WeaponServices?.MyWeapons;
                if (weapons == null)
                    continue;

                foreach (var weaponHandle in weapons)
                {
                    var weapon = weaponHandle.Value;
                    if (weapon == null)
                        continue;

                    info.TransmitEntities.Remove(weapon);
                    info.TransmitAlways.Remove(weapon);
                }
            }
        }
    }
}
