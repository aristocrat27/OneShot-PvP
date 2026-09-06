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

            // Полностью очищаем состояние предыдущего раунда.
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

                // Каждый новый раунд начинается с 33 MP.
                _manaManager.AddPlayer(
                    playerId
                );
            }

            // Сначала отправляем состояние маны.
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

            // Затем запускаем раунд на клиентах.
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

            // Раунд заканчивается ТОЛЬКО когда
            // остался ровно один живой игрок.
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

            // Клиенты должны узнать о завершении ДО очистки
            // серверного состояния.
            if (_network != null)
            {
                _network.BroadcastRoundEnd(
                    winnerId
                );
            }

            // Возвращаем оригинальные PvP-настройки HKMP.
            _damageSettings.Restore();

            // Следующий /start должен начинать всё с чистого состояния.
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