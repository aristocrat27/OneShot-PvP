using System.Collections.Generic;
using Modding;

namespace OneShotPvP.Client
{
    internal static class RoundClientManager
    {
        private static bool _roundActive;

        private static readonly HashSet<ushort> _deadPlayers =
            new HashSet<ushort>();

        public static bool IsRoundActive
        {
            get { return _roundActive; }
        }

        public static void StartRound()
        {
            _roundActive = true;

            _deadPlayers.Clear();

            SetOneHealth();

            ClientManaManager.StartRound();

            Modding.Logger.Log(
                "[OneShotPvP] Client round started."
            );
        }

        public static void EndRound()
        {
            if (!_roundActive)
            {
                return;
            }

            _roundActive = false;

            _deadPlayers.Clear();

            ClientManaManager.EndRound();

            Modding.Logger.Log(
                "[OneShotPvP] Client round ended."
            );
        }

        public static bool IsPlayerDead(
            ushort playerId)
        {
            return _deadPlayers.Contains(
                playerId
            );
        }

        public static void MarkPlayerDead(
            ushort playerId)
        {
            _deadPlayers.Add(
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

            PlayerData.instance.health = 1;
            PlayerData.instance.healthBlue = 0;
            PlayerData.instance.joniHealthBlue = 0;
            PlayerData.instance.damagedBlue = false;

            /*
             * Простого изменения PlayerData.health недостаточно
             * для визуального обновления ванильных масок.
             *
             * TakeHealth() отправляет ванильное событие:
             * HeroCtrl-HeroDamaged
             *
             * Поэтому вызываем его с 0 урона.
             * Значение здоровья при этом не изменится.
             */
            if (HeroController.instance != null)
            {
                HeroController.instance.TakeHealth(0);
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

            ClientManaManager.Clear();

            Modding.Logger.Log(
                "[OneShotPvP] Client round state cleared."
            );
        }
    }
}