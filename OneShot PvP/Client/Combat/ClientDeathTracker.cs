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
                return;
            }

            _initialized = true;
            _deathReported = false;

            _network = network;

            On.HeroController.Update +=
                OnHeroUpdate;

            Modding.Logger.Log(
                "[OneShotPvP] ClientDeathTracker initialized."
            );
        }

        private static void OnHeroUpdate(
            On.HeroController.orig_Update orig,
            HeroController self)
        {
            orig(self);

            if (self == null)
            {
                return;
            }

            if (!RoundClientManager.IsRoundActive)
            {
                _deathReported = false;

                return;
            }

            bool isDead =
                self.cState.dead;

            if (!isDead)
            {
                if (_deathReported)
                {
                    Modding.Logger.Log(
                        "[OneShotPvP] Player is alive again. " +
                        "Resetting death state."
                    );
                }

                _deathReported = false;

                return;
            }

            if (_deathReported)
            {
                return;
            }

            _deathReported = true;

            Modding.Logger.Log(
                "[OneShotPvP] Hero death detected."
            );

            OnPlayerDeath();
        }

        private static void OnPlayerDeath()
        {
            ushort killerId;

            if (!LastAttackerTracker.TryGetLastAttacker(
                out killerId))
            {
                Modding.Logger.Log(
                    "[OneShotPvP] DeathReport was not sent: " +
                    "last attacker was not found."
                );

                return;
            }

            Modding.Logger.Log(
                "[OneShotPvP] Killer detected. " +
                "KillerId=" +
                killerId
            );

            if (_network == null)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] DeathReport was not sent: " +
                    "network is null."
                );

                return;
            }

            _network.SendDeathReport(
                killerId
            );

            Modding.Logger.Log(
                "[OneShotPvP] DeathReport sent. " +
                "KillerId=" +
                killerId
            );

            LastAttackerTracker.Clear();
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