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

        public int AlivePlayerCount
        {
            get
            {
                return _alivePlayers.Count;
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

        public bool IsAlive(
            ushort playerId)
        {
            return _alivePlayers.Contains(
                playerId
            );
        }

        public bool StartRound()
        {
            if (_roundActive)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] StartRound rejected: " +
                    "round is already active."
                );

                return false;
            }

            IReadOnlyCollection<IServerPlayer> players =
                _serverApi.ServerManager.Players;

            if (players.Count < 2)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] StartRound rejected: " +
                    "less than 2 players."
                );

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
            _manaManager.Clear();

            _roundActive = true;

            foreach (IServerPlayer player in players)
            {
                ushort playerId =
                    player.Id;

                _alivePlayers.Add(
                    playerId
                );

                _manaManager.AddPlayer(
                    playerId
                );
            }

            foreach (IServerPlayer player in players)
            {
                if (_network == null)
                {
                    continue;
                }

                _network.SendInitialMana(
                    player.Id
                );
            }

            foreach (IServerPlayer player in players)
            {
                if (_network == null)
                {
                    continue;
                }

                _network.SendRoundStart(
                    player.Id
                );
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

        public bool StartTestRound(
            ushort realPlayerId,
            ushort virtualPlayerId1,
            ushort virtualPlayerId2)
        {
            if (_roundActive)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] StartTestRound rejected: " +
                    "round is already active."
                );

                return false;
            }

            if (realPlayerId == virtualPlayerId1 ||
                realPlayerId == virtualPlayerId2 ||
                virtualPlayerId1 == virtualPlayerId2)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] StartTestRound rejected: " +
                    "test player IDs are not unique."
                );

                return false;
            }

            if (!_damageSettings.ApplyForRound())
            {
                Modding.Logger.Log(
                    "[OneShotPvP] StartTestRound rejected: " +
                    "failed to apply damage settings."
                );

                return false;
            }

            _alivePlayers.Clear();
            _manaManager.Clear();

            _roundActive = true;

            _alivePlayers.Add(
                realPlayerId
            );

            _alivePlayers.Add(
                virtualPlayerId1
            );

            _alivePlayers.Add(
                virtualPlayerId2
            );

            _manaManager.AddPlayer(
                realPlayerId
            );

            _manaManager.AddPlayer(
                virtualPlayerId1
            );

            _manaManager.AddPlayer(
                virtualPlayerId2
            );

            if (_network != null)
            {
                _network.SendInitialMana(
                    realPlayerId
                );

                _network.SendRoundStart(
                    realPlayerId
                );
            }

            Modding.Logger.Log(
                "[OneShotPvP] Test round started. " +
                "RealPlayerId=" +
                realPlayerId +
                " VirtualPlayer1=" +
                virtualPlayerId1 +
                " VirtualPlayer2=" +
                virtualPlayerId2
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
                Modding.Logger.Log(
                    "[OneShotPvP] OnPlayerDeath ignored: " +
                    "player is not alive in current round. " +
                    "PlayerId=" +
                    playerId
                );

                return;
            }

            Modding.Logger.Log(
                "[OneShotPvP] Player eliminated. " +
                "PlayerId=" +
                playerId +
                " AlivePlayers=" +
                _alivePlayers.Count
            );

            CheckRoundEnd();
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

            IServerPlayer winner;

            if (_serverApi.ServerManager.TryGetPlayer(
                winnerId,
                out winner))
            {
                _serverApi.ServerManager.BroadcastMessage(
                    "Победитель раунда: " +
                    winner.Username
                );

                Modding.Logger.Log(
                    "[OneShotPvP] Winner announced: " +
                    winner.Username
                );
            }
            else
            {
                Modding.Logger.Log(
                    "[OneShotPvP] Winner player object was not found. " +
                    "WinnerId=" +
                    winnerId
                );
            }

            if (_network != null)
            {
                _network.BroadcastRoundEnd(
                    winnerId
                );
            }

            _damageSettings.Restore();

            _manaManager.Clear();

            _alivePlayers.Clear();

            Modding.Logger.Log(
                "[OneShotPvP] Round state cleared."
            );
        }

        public void Reset()
        {
            _roundActive = false;

            _alivePlayers.Clear();

            _manaManager.Clear();

            _damageSettings.Restore();

            Modding.Logger.Log(
                "[OneShotPvP] Round manager reset."
            );
        }
    }
}