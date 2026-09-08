using Hkmp.Api.Server;

using OneShotPvP.Server.Stats;
using OneShotPvP.Settings;

namespace OneShotPvP.Server
{
    internal sealed class OneShotServerAddon : ServerAddon
    {
        private ServerManaManager _manaManager;
        private ServerNetManager _network;
        private RoundManager _roundManager;
        private StatsManager _statsManager;

        protected override string Name
        {
            get
            {
                return OneShotConstants.Name;
            }
        }

        protected override string Version
        {
            get
            {
                return OneShotConstants.Version;
            }
        }

        public override bool NeedsNetwork
        {
            get
            {
                return true;
            }
        }

        public override void Initialize(
            IServerApi serverApi)
        {
            // Защищаем сохранение HKMP GlobalSettings.
            //
            // Если OneShotPvP временно изменит ServerSettings
            // во время раунда, эти временные значения не попадут
            // в постоянные настройки HKMP.
            HkmpGlobalSettingsSaveGuard.Initialize();

            _manaManager =
                new ServerManaManager();

            _statsManager =
                new StatsManager();

            _roundManager =
                new RoundManager(
                    serverApi,
                    _manaManager,
                    _statsManager
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
                new StatsCommand(
                    _statsManager
                )
            );

            serverApi.ServerManager.PlayerConnectEvent +=
                OnPlayerConnect;

            serverApi.ServerManager.PlayerDisconnectEvent +=
                OnPlayerDisconnect;

            Modding.Logger.Log(
                "[OneShotPvP] Server addon initialized."
            );
        }

        private void OnPlayerConnect(
            IServerPlayer player)
        {
            if (player == null)
            {
                return;
            }

            _statsManager.RegisterPlayer(
                player.Username
            );

            _manaManager.AddPlayer(
                player.Id
            );

            if (_network != null)
            {
                _network.SendInitialMana(
                    player.Id
                );
            }
        }

        private void OnPlayerDisconnect(
            IServerPlayer player)
        {
            if (player == null)
            {
                return;
            }

            _roundManager.OnPlayerDisconnect(
                player.Id
            );

            _manaManager.RemovePlayer(
                player.Id
            );
        }
    }
}