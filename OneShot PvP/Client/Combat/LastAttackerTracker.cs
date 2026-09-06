using System;
using UnityEngine;

namespace OneShotPvP.Client
{
    internal static class LastAttackerTracker
    {
        private static bool _initialized;

        private static ushort? _lastAttackerId;

        public static bool HasAttacker
        {
            get
            {
                return _lastAttackerId.HasValue;
            }
        }

        public static ushort LastAttackerId
        {
            get
            {
                return _lastAttackerId.HasValue
                    ? _lastAttackerId.Value
                    : ushort.MaxValue;
            }
        }

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _lastAttackerId = null;

            On.HealthManager.TakeDamage +=
                OnTakeDamage;

            Modding.Logger.Log(
                "[OneShotPvP] LastAttackerTracker initialized."
            );
        }

        private static void OnTakeDamage(
            On.HealthManager.orig_TakeDamage orig,
            HealthManager self,
            HitInstance hitInstance)
        {
            if (self == null)
            {
                orig(
                    self,
                    hitInstance
                );

                return;
            }

            if (hitInstance.Source != null)
            {
                TryRegisterAttackSource(
                    hitInstance.Source
                );
            }

            orig(
                self,
                hitInstance
            );
        }

        public static bool TryRegisterAttackSource(
            GameObject source)
        {
            if (source == null)
            {
                return false;
            }

            Transform current =
                source.transform;

            while (current != null)
            {
                ushort playerId;

                if (TryParsePlayerContainer(
                    current.name,
                    out playerId))
                {
                    _lastAttackerId = playerId;

                    Modding.Logger.Log(
                        "[OneShotPvP] Last attacker registered. " +
                        "PlayerId=" +
                        playerId +
                        " Source=" +
                        source.name
                    );

                    return true;
                }

                current =
                    current.parent;
            }

            return false;
        }

        public static bool TryGetLastAttacker(
            out ushort playerId)
        {
            if (!_lastAttackerId.HasValue)
            {
                playerId = 0;

                return false;
            }

            playerId =
                _lastAttackerId.Value;

            return true;
        }

        public static void Clear()
        {
            _lastAttackerId = null;
        }

        private static bool TryParsePlayerContainer(
            string objectName,
            out ushort playerId)
        {
            playerId = 0;

            if (string.IsNullOrEmpty(
                objectName))
            {
                return false;
            }

            const string prefix =
                "Player Container ";

            if (!objectName.StartsWith(
                prefix,
                StringComparison.Ordinal))
            {
                return false;
            }

            string idText =
                objectName.Substring(
                    prefix.Length
                );

            ushort parsedId;

            if (!ushort.TryParse(
                idText,
                out parsedId))
            {
                return false;
            }

            playerId = parsedId;

            return true;
        }
    }
}