using Modding;

namespace OneShotPvP.Client
{
    internal static class ClientDeathTracker
    {
        private static bool _initialized;
        private static bool _deathReported;
        private static ClientNetManager _network;

        public static void Initialize(
            ClientNetManager network)
        {
            if (_initialized)
            {
                _network = network;
                return;
            }

            _initialized = true;
            _deathReported = false;
            _network = network;

            Modding.Logger.Log(
                "[OneShotPvP] ClientDeathTracker initialized."
            );
        }

        public static void ReportPvpDeath(
            ushort killerId)
        {
            if (!_initialized)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] ReportPvpDeath ignored: " +
                    "ClientDeathTracker is not initialized."
                );

                return;
            }

            if (!RoundClientManager.IsRoundActive)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] ReportPvpDeath ignored: " +
                    "round is not active. " +
                    "KillerId=" +
                    killerId
                );

                return;
            }

            if (_deathReported)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] ReportPvpDeath ignored: " +
                    "death was already reported. " +
                    "KillerId=" +
                    killerId
                );

                return;
            }

            if (_network == null)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] ReportPvpDeath failed: " +
                    "network is null."
                );

                return;
            }

            _deathReported = true;

            Modding.Logger.Log(
                "[OneShotPvP] PvP death confirmed. " +
                "KillerId=" +
                killerId
            );

            _network.SendDeathReport(
                killerId
            );

            Modding.Logger.Log(
                "[OneShotPvP] DeathReport sent. " +
                "KillerId=" +
                killerId
            );
        }

        public static void Reset()
        {
            _deathReported = false;

            LastAttackerTracker.Clear();

            Modding.Logger.Log(
                "[OneShotPvP] ClientDeathTracker reset."
            );
        }

        public static void Clear()
        {
            _deathReported = false;

            LastAttackerTracker.Clear();
        }
    }
}