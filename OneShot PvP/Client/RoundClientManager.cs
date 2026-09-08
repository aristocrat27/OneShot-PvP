using System.Collections.Generic;

namespace OneShotPvP.Client
{
    internal static class RoundClientManager
    {
        private static bool _roundActive;

        private static uint _currentRoundId;

        private static readonly HashSet<ushort> _deadPlayers =
            new HashSet<ushort>();

        public static bool IsRoundActive
        {
            get
            {
                return _roundActive;
            }
        }

        public static uint CurrentRoundId
        {
            get
            {
                return _currentRoundId;
            }
        }

        public static void StartRound(
            uint roundId)
        {
            _roundActive = true;

            _currentRoundId =
                roundId;

            _deadPlayers.Clear();

            /*
             * Каждый новый раунд должен начинаться
             * с возможностью отправить новый DeathReport.
             */
            ClientDeathTracker.Reset();

            LastAttackerTracker.Clear();

            SetOneHealth();

            ClientManaManager.StartRound();

            Modding.Logger.Log(
                "[OneShotPvP] Client round started. " +
                "RoundId=" +
                roundId
            );
        }

        public static void EndRound(
            uint roundId,
            ushort winnerId,
            bool isTeamVictory,
            byte winnerTeam)
        {
            if (!_roundActive)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] RoundEnd received while " +
                    "client round was already inactive. " +
                    "RoundId=" +
                    roundId +
                    " WinnerId=" +
                    winnerId +
                    " IsTeamVictory=" +
                    isTeamVictory +
                    " WinnerTeam=" +
                    winnerTeam
                );

                return;
            }

            if (roundId != _currentRoundId)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] RoundEnd ignored: " +
                    "packet belongs to another round. " +
                    "PacketRoundId=" +
                    roundId +
                    " CurrentRoundId=" +
                    _currentRoundId +
                    " WinnerId=" +
                    winnerId +
                    " IsTeamVictory=" +
                    isTeamVictory +
                    " WinnerTeam=" +
                    winnerTeam
                );

                return;
            }

            _roundActive = false;

            _deadPlayers.Clear();

            /*
             * Сбрасываем состояние отправки смерти,
             * чтобы следующий раунд не унаследовал
             * старый флаг.
             */
            ClientDeathTracker.Clear();

            LastAttackerTracker.Clear();

            ClientManaManager.Clear();

            if (isTeamVictory)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] Client round ended. " +
                    "RoundId=" +
                    roundId +
                    " WinningTeam=" +
                    winnerTeam
                );
            }
            else
            {
                Modding.Logger.Log(
                    "[OneShotPvP] Client round ended. " +
                    "RoundId=" +
                    roundId +
                    " WinnerId=" +
                    winnerId
                );
            }
        }

        public static bool IsPlayerDead(
            ushort playerId)
        {
            return _deadPlayers.Contains(
                playerId
            );
        }

        public static void MarkPlayerDead(
            uint roundId,
            ushort playerId)
        {
            if (!_roundActive)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] PlayerDeath ignored: " +
                    "client round is not active. " +
                    "RoundId=" +
                    roundId +
                    " PlayerId=" +
                    playerId
                );

                return;
            }

            if (roundId != _currentRoundId)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] PlayerDeath ignored: " +
                    "packet belongs to another round. " +
                    "PacketRoundId=" +
                    roundId +
                    " CurrentRoundId=" +
                    _currentRoundId +
                    " PlayerId=" +
                    playerId
                );

                return;
            }

            if (!_deadPlayers.Add(
                playerId))
            {
                Modding.Logger.Log(
                    "[OneShotPvP] PlayerDeath ignored: " +
                    "player is already marked dead. " +
                    "RoundId=" +
                    roundId +
                    " PlayerId=" +
                    playerId
                );

                return;
            }

            Modding.Logger.Log(
                "[OneShotPvP] Player marked dead on client. " +
                "RoundId=" +
                roundId +
                " PlayerId=" +
                playerId
            );
        }

        private static void SetOneHealth()
        {
            if (PlayerData.instance == null)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] Cannot set HP: " +
                    "PlayerData.instance is null."
                );

                return;
            }

            PlayerData.instance.health =
                1;

            PlayerData.instance.healthBlue =
                0;

            PlayerData.instance.joniHealthBlue =
                0;

            PlayerData.instance.damagedBlue =
                false;

            if (HeroController.instance != null)
            {
                HeroController.instance.TakeHealth(
                    0
                );
            }

            Modding.Logger.Log(
                "[OneShotPvP] Player HP set to 1 " +
                "and vanilla HP HUD refresh requested."
            );
        }

        public static void Clear()
        {
            _roundActive = false;

            _deadPlayers.Clear();

            /*
             * Полностью сбрасываем состояние смерти,
             * чтобы после повторного подключения/старта
             * не осталось состояние предыдущего раунда.
             */
            ClientDeathTracker.Clear();

            LastAttackerTracker.Clear();

            ClientManaManager.Clear();

            Modding.Logger.Log(
                "[OneShotPvP] Client round state cleared."
            );
        }
    }
}