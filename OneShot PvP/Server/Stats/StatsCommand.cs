using System;
using System.Collections.Generic;

using Hkmp.Api.Command.Server;

namespace OneShotPvP.Server.Stats
{
    internal sealed class StatsCommand : IServerCommand
    {
        private readonly StatsManager _statsManager;

        public StatsCommand(
            StatsManager statsManager)
        {
            _statsManager = statsManager;
        }

        public string Trigger
        {
            get
            {
                return "/stats";
            }
        }

        public string[] Aliases
        {
            get
            {
                return new string[0];
            }
        }

        public bool AuthorizedOnly
        {
            get
            {
                return false;
            }
        }

        public void Execute(
            ICommandSender commandSender,
            string[] arguments)
        {
            IReadOnlyList<StatsData> stats =
                _statsManager.GetSortedStats();

            commandSender.SendMessage(
                "=== OneShotPvP ==="
            );

            commandSender.SendMessage(
                "Статистика побед:"
            );

            if (stats.Count == 0)
            {
                commandSender.SendMessage(
                    "Пока нет зарегистрированных игроков."
                );

                return;
            }

            for (int i = 0; i < stats.Count; i++)
            {
                StatsData playerStats =
                    stats[i];

                commandSender.SendMessage(
                    (i + 1) +
                    ". " +
                    playerStats.PlayerName +
                    " — " +
                    playerStats.Wins +
                    " побед"
                );
            }
        }
    }
}