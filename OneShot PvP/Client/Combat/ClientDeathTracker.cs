using Modding;

namespace OneShotPvP.Client
{
    internal static class ClientDeathTracker
    {
        private static bool _initialized;
        private static bool _deathReported;

        public static void Initialize(
            ClientNetManager network)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _deathReported = false;

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
            Modding.Logger.Log(
                "[OneShotPvP] Player death detected. " +
                "Killer detection is currently handled separately."
            );
        }

        public static void Reset()
        {
            _deathReported = false;

            Modding.Logger.Log(
                "[OneShotPvP] ClientDeathTracker reset."
            );
        }

        public static void Clear()
        {
            _deathReported = false;
        }
    }
}