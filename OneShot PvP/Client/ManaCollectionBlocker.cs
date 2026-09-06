using Modding;

namespace OneShotPvP.Client
{
    internal static class ManaCollectionBlocker
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            On.PlayerData.AddMPCharge +=
                OnAddMPCharge;

            On.PlayerData.TakeReserveMP +=
                OnTakeReserveMP;
        }

        private static bool OnAddMPCharge(
            On.PlayerData.orig_AddMPCharge orig,
            PlayerData self,
            int amount)
        {
            if (ClientManaManager.IsRoundActive)
            {
                return false;
            }

            return orig(
                self,
                amount
            );
        }

        private static void OnTakeReserveMP(
            On.PlayerData.orig_TakeReserveMP orig,
            PlayerData self,
            int amount)
        {
            if (ClientManaManager.IsRoundActive)
            {
                self.MPReserve = 0;
                return;
            }

            orig(
                self,
                amount
            );
        }
    }
}