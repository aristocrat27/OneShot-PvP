namespace OneShotPvP.Server.Stats
{
    internal sealed class StatsData
    {
        public string PlayerName { get; private set; }

        public int Wins { get; private set; }

        public StatsData(
            string playerName)
        {
            PlayerName = playerName;
            Wins = 0;
        }

        public void AddWin()
        {
            Wins++;
        }
    }
}