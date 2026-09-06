using Hkmp.Api.Server;

namespace OneShotPvP.Server
{
    internal sealed class OneShotServerAddon : ServerAddon
    {
        private ServerManaManager _manaManager;
        private ServerNetManager _network;
        private RoundManager _roundManager;

        protected override string Name
        {
            get { return OneShotConstants.Name; }
        }

        protected override string Version
        {
            get { return OneShotConstants.Version; }
        }

        public override bool NeedsNetwork
        {
            get { return true; }
        }

        public override void Initialize(
            IServerApi serverApi)
        {
            _manaManager =
                new ServerManaManager();

            _roundManager =
                new RoundManager(
                    serverApi,
                    _manaManager
                );

            _network =
                new ServerNetManager(
                    this,
                    serverApi,
                    _manaManager,
                    _roundManager
                );

            _roundManager.SetNetwork(
                _network
            );

            serverApi.CommandManager.RegisterCommand(
                new OneShotCommand(
                    _roundManager
                )
            );

            serverApi.CommandManager.RegisterCommand(
                new ServerManaTestCommand()
            );

            serverApi.CommandManager.RegisterCommand(
                new ServerTestRoundCommand(
                    serverApi,
                    _manaManager,
                    _network,
                    _roundManager
                )
            );

            serverApi.ServerManager.PlayerConnectEvent +=
                OnPlayerConnect;

            serverApi.ServerManager.PlayerDisconnectEvent +=
                OnPlayerDisconnect;
        }

        private void OnPlayerConnect(
            IServerPlayer player)
        {
            _manaManager.AddPlayer(
                player.Id
            );

            _network.SendInitialMana(
                player.Id
            );
        }

        private void OnPlayerDisconnect(
            IServerPlayer player)
        {
            _roundManager.OnPlayerDisconnect(
                player.Id
            );

            _manaManager.RemovePlayer(
                player.Id
            );
        }
    }
}