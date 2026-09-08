using System.Collections.Generic;
using System.Linq;

using Hkmp.Api.Server;

using OneShotPvP.Server.Stats;

namespace OneShotPvP.Server
{
    internal sealed class RoundManager
    {
        private readonly IServerApi _serverApi;
        private readonly ServerManaManager _manaManager;
        private readonly RoundDamageSettings _damageSettings;
        private readonly StatsManager _statsManager;

        private ServerNetManager _network;

        private readonly HashSet<ushort> _alivePlayers =
            new HashSet<ushort>();

        private bool _roundActive;

        private uint _currentRoundId;

        public bool IsRoundActive
        {
            get
            {
                return _roundActive;
            }
        }

        public uint CurrentRoundId
        {
            get
            {
                return _currentRoundId;
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
            ServerManaManager manaManager,
            StatsManager statsManager)
        {
            _serverApi = serverApi;
            _manaManager = manaManager;
            _statsManager = statsManager;

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

        private uint StartNewRoundId()
        {
            if (_currentRoundId == uint.MaxValue)
            {
                _currentRoundId = 1;
            }
            else
            {
                _currentRoundId++;
            }

            if (_currentRoundId == 0)
            {
                _currentRoundId = 1;
            }

            return _currentRoundId;
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

            if (players == null ||
                players.Count < 2)
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

            uint roundId =
                StartNewRoundId();

            _roundActive = true;

            foreach (IServerPlayer player in players)
            {
                if (player == null)
                {
                    continue;
                }

                ushort playerId =
                    player.Id;

                _alivePlayers.Add(
                    playerId
                );

                _manaManager.AddPlayer(
                    playerId
                );
            }

            if (_alivePlayers.Count < 2)
            {
                _roundActive = false;

                _alivePlayers.Clear();
                _manaManager.Clear();

                _damageSettings.Restore();

                Modding.Logger.Log(
                    "[OneShotPvP] StartRound aborted: " +
                    "less than 2 valid players remained."
                );

                return false;
            }

            foreach (IServerPlayer player in players)
            {
                if (player == null ||
                    _network == null)
                {
                    continue;
                }

                if (!_alivePlayers.Contains(
                    player.Id))
                {
                    continue;
                }

                _network.SendInitialMana(
                    player.Id
                );
            }

            foreach (IServerPlayer player in players)
            {
                if (player == null ||
                    _network == null)
                {
                    continue;
                }

                if (!_alivePlayers.Contains(
                    player.Id))
                {
                    continue;
                }

                _network.SendRoundStart(
                    player.Id,
                    roundId
                );
            }

            _serverApi.ServerManager.BroadcastMessage(
                "Раунд начался!"
            );

            Modding.Logger.Log(
                "[OneShotPvP] Round started. " +
                "RoundId=" +
                roundId +
                " Alive players=" +
                _alivePlayers.Count
            );

            return true;
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
                "RoundId=" +
                _currentRoundId +
                " PlayerId=" +
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
                "RoundId=" +
                _currentRoundId +
                " PlayerId=" +
                playerId +
                " AlivePlayers=" +
                _alivePlayers.Count
            );

            CheckRoundEnd();
        }

        private void CheckRoundEnd()
        {
            /*
             * CHECK 1
             *
             * Exactly one alive player always wins.
             */
            if (_alivePlayers.Count == 1)
            {
                ushort winnerId =
                    _alivePlayers.First();

                Modding.Logger.Log(
                    "[OneShotPvP] Round end detected: " +
                    "one alive player remains. " +
                    "WinnerId=" +
                    winnerId
                );

                EndPlayerRound(
                    winnerId
                );

                return;
            }

            /*
             * No alive players means there is no valid winner.
             */
            if (_alivePlayers.Count == 0)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] CheckRoundEnd: " +
                    "no alive players remain."
                );

                return;
            }

            /*
             * CHECK 2
             *
             * Team logic is only relevant when HKMP teams
             * are enabled.
             */
            if (!_serverApi.ServerManager.ServerSettings.TeamsEnabled)
            {
                return;
            }

            object winningTeam;

            if (!TryGetTeamWinner(
                out winningTeam))
            {
                return;
            }

            Modding.Logger.Log(
                "[OneShotPvP] Round end detected: " +
                "one alive team remains. " +
                "WinnerTeam=" +
                winningTeam
            );

            EndTeamRound(
                winningTeam
            );
        }

        private bool TryGetTeamWinner(
            out object winningTeam)
        {
            winningTeam = null;

            HashSet<object> aliveTeams =
                new HashSet<object>();

            int unteamedPlayers = 0;

            foreach (ushort playerId in _alivePlayers)
            {
                IServerPlayer player;

                if (!_serverApi.ServerManager.TryGetPlayer(
                    playerId,
                    out player))
                {
                    continue;
                }

                if (player == null)
                {
                    continue;
                }

                object team =
                    player.Team;

                /*
                 * Team.None means that the player does not
                 * belong to a team.
                 *
                 * An unteamed player is treated as an
                 * independent side, so team victory cannot
                 * be declared while one remains.
                 */
                if (team == null ||
                    team.ToString() == "None")
                {
                    unteamedPlayers++;

                    continue;
                }

                aliveTeams.Add(
                    team
                );
            }

            /*
             * A mixed team / FFA situation cannot produce
             * a team victory.
             */
            if (unteamedPlayers > 0)
            {
                return false;
            }

            /*
             * Teams are enabled, but nobody is assigned
             * to a team. This is ordinary FFA.
             */
            if (aliveTeams.Count == 0)
            {
                return false;
            }

            /*
             * More than one team remains alive.
             */
            if (aliveTeams.Count > 1)
            {
                return false;
            }

            /*
             * Exactly one team remains.
             */
            winningTeam =
                aliveTeams.First();

            return true;
        }

        private void EndPlayerRound(
            ushort winnerId)
        {
            if (!_roundActive)
            {
                return;
            }

            uint roundId =
                _currentRoundId;

            _roundActive = false;

            Modding.Logger.Log(
                "[OneShotPvP] Player round ended. " +
                "RoundId=" +
                roundId +
                " WinnerId=" +
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

                _statsManager.AddWin(
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
                    roundId,
                    winnerId
                );
            }

            FinishRound(
                roundId
            );
        }

        private void EndTeamRound(
            object winningTeam)
        {
            if (!_roundActive)
            {
                return;
            }

            uint roundId =
                _currentRoundId;

            _roundActive = false;

            string teamName =
                winningTeam.ToString();

            Modding.Logger.Log(
                "[OneShotPvP] Team round ended. " +
                "RoundId=" +
                roundId +
                " WinnerTeam=" +
                teamName
            );

            _serverApi.ServerManager.BroadcastMessage(
                "Победила команда: " +
                teamName
            );

            /*
             * Every alive player belonging to the winning
             * team receives one win.
             */
            int winnersCount = 0;

            foreach (ushort playerId in _alivePlayers)
            {
                IServerPlayer player;

                if (!_serverApi.ServerManager.TryGetPlayer(
                    playerId,
                    out player))
                {
                    continue;
                }

                if (player == null)
                {
                    continue;
                }

                object playerTeam =
                    player.Team;

                if (playerTeam == null ||
                    !playerTeam.Equals(
                        winningTeam))
                {
                    continue;
                }

                if (_statsManager.AddWin(
                    player.Username))
                {
                    winnersCount++;

                    Modding.Logger.Log(
                        "[OneShotPvP] Team winner recorded. " +
                        "Player=" +
                        player.Username +
                        " Team=" +
                        teamName
                    );
                }
            }

            Modding.Logger.Log(
                "[OneShotPvP] Team victory recorded for " +
                winnersCount +
                " player(s). " +
                "Team=" +
                teamName
            );

            if (_network != null)
            {
                byte winnerTeam =
                    System.Convert.ToByte(
                        winningTeam
                    );

                _network.BroadcastTeamRoundEnd(
                    roundId,
                    winnerTeam
                );
            }

            FinishRound(
                roundId
            );
        }

        private void FinishRound(
            uint roundId)
        {
            _damageSettings.Restore();

            _manaManager.Clear();

            _alivePlayers.Clear();

            Modding.Logger.Log(
                "[OneShotPvP] Round state cleared. " +
                "RoundId=" +
                roundId
            );
        }

        public void Reset()
        {
            _roundActive = false;

            _alivePlayers.Clear();

            _manaManager.Clear();

            _damageSettings.Restore();

            Modding.Logger.Log(
                "[OneShotPvP] Round manager reset. " +
                "CurrentRoundId=" +
                _currentRoundId
            );
        }
    }
}