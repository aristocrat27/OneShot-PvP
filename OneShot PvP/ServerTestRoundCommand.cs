using Hkmp.Api.Command.Server;
using Hkmp.Api.Server;

namespace OneShotPvP.Server
{
    internal sealed class ServerTestRoundCommand : IServerCommand
    {
        private readonly IServerApi _serverApi;
        private readonly ServerManaManager _manaManager;
        private readonly ServerNetManager _network;
        private readonly RoundManager _roundManager;

        public ServerTestRoundCommand(
            IServerApi serverApi,
            ServerManaManager manaManager,
            ServerNetManager network,
            RoundManager roundManager)
        {
            _serverApi = serverApi;
            _manaManager = manaManager;
            _network = network;
            _roundManager = roundManager;
        }

        public string Trigger
        {
            get { return "/os_test_round"; }
        }

        public string[] Aliases
        {
            get { return new string[0]; }
        }

        public bool AuthorizedOnly
        {
            get { return true; }
        }

        public void Execute(
            ICommandSender commandSender,
            string[] arguments)
        {
            if (_serverApi.ServerManager.Players.Count != 1)
            {
                commandSender.SendMessage(
                    "OneShotPvP: для /os_test_round " +
                    "должен быть подключён ровно 1 игрок."
                );

                return;
            }

            IServerPlayer player =
                null;

            foreach (IServerPlayer currentPlayer
                in _serverApi.ServerManager.Players)
            {
                player = currentPlayer;
                break;
            }

            if (player == null)
            {
                commandSender.SendMessage(
                    "OneShotPvP: игрок не найден."
                );

                return;
            }

            if (!_roundManager.ApplyTestDamageSettings())
            {
                commandSender.SendMessage(
                    "OneShotPvP: не удалось применить " +
                    "тестовые настройки PvP-урона."
                );

                return;
            }

            _manaManager.AddPlayer(
                player.Id
            );

            _network.SendInitialMana(
                player.Id
            );

            _network.SendRoundStart(
                player.Id
            );

            commandSender.SendMessage(
                "OneShotPvP: тестовый раунд отправлен игроку " +
                player.Username +
                ". Настройки PvP-урона применены."
            );
        }
    }
}