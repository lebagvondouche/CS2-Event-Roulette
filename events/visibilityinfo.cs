namespace RandomRoundEvents;

internal sealed class VisibilityInfo
{
    private readonly RandomRoundEvents _plugin;

    public VisibilityInfo(RandomRoundEvents plugin)
    {
        _plugin = plugin;
    }

    public void Apply(RandomRoundEvents.EventType eventType)
    {
        switch (eventType)
        {
            case RandomRoundEvents.EventType.InvisibleRound:
                _plugin.ShowEvent("Invisible Round", "Everyone is invisible! Knife only, no friendly fire!");
                _plugin.SetConVar("mp_friendlyfire", 0);
                _plugin.StripAllWeapons();
                _plugin.GiveAllPlayersKnives();
                _plugin.SetAllPlayersInvisible();
                _plugin.SetAllPlayerWeaponsInvisible();
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
        _plugin.ResetVisibilityInfoState();
    }
}
