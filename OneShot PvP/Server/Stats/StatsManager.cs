using System;
using System.Collections.Generic;
using System.Linq;

namespace OneShotPvP.Server.Stats
{
    internal sealed class StatsManager
    {
        private readonly Dictionary<string, StatsData> _players =
            new Dictionary<string, StatsData>(
                StringComparer.OrdinalIgnoreCase
            );

        public int PlayerCount
        {
            get
            {
                return _players.Count;
            }
        }

        public void RegisterPlayer(
            string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return;
            }

            string normalizedName =
                playerName.Trim();

            if (_players.ContainsKey(
                normalizedName))
            {
                return;
            }

            _players.Add(
                normalizedName,
                new StatsData(
                    normalizedName
                )
            );

            Modding.Logger.Log(
                "[OneShotPvP] Stats player registered. " +
                "Player=" +
                normalizedName
            );
        }

        public bool AddWin(
            string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                return false;
            }

            string normalizedName =
                playerName.Trim();

            StatsData stats;

            if (!_players.TryGetValue(
                normalizedName,
                out stats))
            {
                stats =
                    new StatsData(
                        normalizedName
                    );

                _players.Add(
                    normalizedName,
                    stats
                );

                Modding.Logger.Log(
                    "[OneShotPvP] Stats player created " +
                    "when adding win. Player=" +
                    normalizedName
                );
            }

            stats.AddWin();

            Modding.Logger.Log(
                "[OneShotPvP] Player win recorded. " +
                "Player=" +
                normalizedName +
                " Wins=" +
                stats.Wins
            );

            return true;
        }

        public IReadOnlyList<StatsData> GetSortedStats()
        {
            return _players.Values
                .OrderByDescending(
                    stats => stats.Wins
                )
                .ThenBy(
                    stats => stats.PlayerName,
                    StringComparer.OrdinalIgnoreCase
                )
                .ToList();
        }

        public void Reset()
        {
            _players.Clear();

            Modding.Logger.Log(
                "[OneShotPvP] Player statistics reset."
            );
        }
    }
}