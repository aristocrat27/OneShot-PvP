using System.Collections.Generic;
using System.Linq;

using Hkmp.Api.Server;

namespace OneShotPvP.Server
{
    internal sealed class RoundManager
    {
        private readonly IServerApi _serverApi;
        private readonly ServerManaManager _manaManager;
        private readonly RoundDamageSettings _damageSettings;

        private ServerNetManager _network;

        private readonly HashSet<ushort> _alivePlayers =
            new HashSet<ushort>();

        private bool _roundActive;

        public bool IsRoundActive
        {
            get
            {
                return _roundActive;
            }
        }

        public RoundManager(
            IServerApi serverApi,
            ServerManaManager manaManager)
        {
            _serverApi = serverApi;
            _manaManager = manaManager;

            _damageSettings =
                new RoundDamageSettings(
                    serverApi
                );
        }

        public void SetNetwork(
            ServerNetManager network)
        {
            _network = network;
        }

        public bool StartRound()
        {
            if (_roundActive)
            {
                return false;
            }

            IReadOnlyCollection<IServerPlayer> players =
                _serverApi.ServerManager.Players;

            if (players.Count < 2)
            {
                return false;
            }

            if (!_damageSettings.ApplyForRound())
            {
                _serverApi.ServerManager.BroadcastMessage(
                    "OneShotPvP: не удалось применить " +
                    "настройки PvP-урона."
                );

                return false;
            }

            _alivePlayers.Clear();

            _roundActive = true;

            foreach (IServerPlayer player in players)
            {
                _alivePlayers.Add(
                    player.Id
                );

                _manaManager.AddPlayer(
                    player.Id
                );

                if (_network != null)
                {
                    _network.SendInitialMana(
                        player.Id
                    );

                    _network.SendRoundStart(
                        player.Id
                    );
                }
            }

            _serverApi.ServerManager.BroadcastMessage(
                "Раунд начался!"
            );

            Modding.Logger.Log(
                "[OneShotPvP] Round started. " +
                "Alive players=" +
                _alivePlayers.Count
            );

            return true;
        }

        public bool ApplyTestDamageSettings()
        {
            bool applied =
                _damageSettings.ApplyForRound();

            if (applied)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] Test damage settings applied."
                );
            }
            else
            {
                Modding.Logger.Log(
                    "[OneShotPvP] Failed to apply test damage settings."
                );
            }

            return applied;
        }

        public void OnPlayerDeath(
            ushort playerId)
        {
            if (!_roundActive)
            {
                return;
            }

            if (!_alivePlayers.Remove(
                playerId))
            {
                return;
            }

            Modding.Logger.Log(
                "[OneShotPvP] Player removed from alive list. " +
                "PlayerId=" +
                playerId +
                " AlivePlayers=" +
                _alivePlayers.Count
            );

            CheckRoundEnd();
        }

        private void CheckRoundEnd()
        {
            if (!_roundActive)
            {
                return;
            }

            if (_alivePlayers.Count != 1)
            {
                return;
            }

            ushort winnerId =
                _alivePlayers.First();

            EndRound(
                winnerId
            );
        }

        private void EndRound(
            ushort winnerId)
        {
            if (!_roundActive)
            {
                return;
            }

            _roundActive = false;

            Modding.Logger.Log(
                "[OneShotPvP] Round ended. " +
                "WinnerId=" +
                winnerId
            );

            _damageSettings.Restore();

            if (_network != null)
            {
                _network.BroadcastRoundEnd(
                    winnerId
                );
            }

            IServerPlayer winner;

            if (_serverApi.ServerManager.TryGetPlayer(
                winnerId,
                out winner))
            {
                _serverApi.ServerManager.BroadcastMessage(
                    "Победитель раунда: " +
                    winner.Username
                );
            }

            _alivePlayers.Clear();
        }

        public void OnPlayerDisconnect(
            ushort playerId)
        {
            if (!_roundActive)
            {
                return;
            }

            if (!_alivePlayers.Remove(
                playerId))
            {
                return;
            }

            Modding.Logger.Log(
                "[OneShotPvP] Player disconnected from round. " +
                "PlayerId=" +
                playerId +
                " AlivePlayers=" +
                _alivePlayers.Count
            );

            CheckRoundEnd();
        }

        public void Reset()
        {
            _roundActive = false;

            _alivePlayers.Clear();

            _damageSettings.Restore();

            Modding.Logger.Log(
                "[OneShotPvP] Round manager reset."
            );
        }
    }
}