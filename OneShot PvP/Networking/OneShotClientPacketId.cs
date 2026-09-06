namespace OneShotPvP.Networking
{
    internal enum OneShotClientPacketId : byte
    {
        ManaUpdate = 0,
        RoundStart = 1,
        PlayerDeath = 2,
        RoundEnd = 3
    }
}