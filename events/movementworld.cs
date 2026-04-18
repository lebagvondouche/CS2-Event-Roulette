namespace RandomRoundEvents;

internal sealed class MovementWorld
{
    private readonly RandomRoundEvents _plugin;

    public MovementWorld(RandomRoundEvents plugin)
    {
        _plugin = plugin;
    }

    public void Apply(RandomRoundEvents.EventType eventType)
    {
        switch (eventType)
        {
            case RandomRoundEvents.EventType.LowGravity:
                _plugin.ShowEvent("Low Gravity Round", "Float around with a Scout and Zeus. Perfect accuracy!");
                _plugin.SetGravity(_plugin.Config.LowGravityValue);
                _plugin.SetNospread(true);
                _plugin.StartGravityMonitor(_plugin.Config.LowGravityValue);
                _plugin.SetConVar("mp_weapons_allow_zeus", -1);
                _plugin.SetConVar("mp_taser_recharge_time", _plugin.Config.ZeusRechargeTime);
                _plugin.StripAllWeapons();
                _plugin.GiveAllPlayersKnives();
                _plugin.GiveAllPlayersScout();
                _plugin.GiveAllPlayersZeus();
                break;
            case RandomRoundEvents.EventType.SwapTeams:
                _plugin.ShowEvent("Team Swap Round", "A random pair swaps teams every 30 seconds!");
                _plugin.GiveAllPlayersStandardLoadout();
                _plugin.StartSwapTimer();
                break;
            case RandomRoundEvents.EventType.GravitySwitch:
                _plugin.ShowEvent("Gravity Switch Round", "Gravity flips between low and high every 5 seconds!");
                _plugin.GiveAllPlayersStandardLoadout();
                _plugin.StartGravitySwitch();
                _plugin.StartGravityMonitor(_plugin.CurrentGravity);
                break;
            case RandomRoundEvents.EventType.SpeedRandomizer:
                _plugin.ShowEvent("Speed Randomizer Round", "Everyone moves at a different random speed!");
                _plugin.GiveAllPlayersStandardLoadout();
                _plugin.RandomizeAllPlayersSpeed();
                break;
            case RandomRoundEvents.EventType.ChickenRound:
                _plugin.ShowEvent("Chicken Leader Round", "A flock of chickens follows each player!");
                _plugin.GiveAllPlayersStandardLoadout();
                _plugin.SpawnChickensForAllPlayers();
                break;
        }
    }

    public void Reset()
    {
        _plugin.ResetMovementWorldState();
    }
}
