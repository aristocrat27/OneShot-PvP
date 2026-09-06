using Modding;

namespace OneShotPvP.Client
{
    internal static class ClientManaManager
    {
        private static int _serverMana;
        private static bool _roundActive;

        public static int Mana
        {
            get { return _serverMana; }
        }

        public static bool IsRoundActive
        {
            get { return _roundActive; }
        }

        public static void SetMana(int mana)
        {
            if (mana < 0)
            {
                mana = 0;
            }

            _serverMana = mana;

            ApplyToPlayer();
        }

        public static void OnManaSpent(
            int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            int newMana =
                _serverMana - amount;

            if (newMana < 0)
            {
                newMana = 0;
            }

            _serverMana = newMana;

            ApplyToPlayer();
        }

        public static void StartRound()
        {
            _roundActive = true;

            ApplyToPlayer();
        }

        public static void EndRound()
        {
            _roundActive = false;
        }

        private static void ApplyToPlayer()
        {
            if (HeroController.instance == null)
            {
                return;
            }

            HeroController.instance.SetMPCharge(
                _serverMana
            );

            if (PlayerData.instance != null)
            {
                PlayerData.instance.MPReserve = 0;
            }
        }

        public static void Update()
        {
            if (!_roundActive)
            {
                return;
            }

            if (PlayerData.instance == null)
            {
                return;
            }

            if (PlayerData.instance.MPCharge !=
                _serverMana)
            {
                ApplyToPlayer();
            }

            if (PlayerData.instance.MPReserve != 0)
            {
                PlayerData.instance.MPReserve = 0;
            }
        }

        public static void Clear()
        {
            _roundActive = false;
            _serverMana = 0;

            if (HeroController.instance != null)
            {
                HeroController.instance.SetMPCharge(0);
            }

            if (PlayerData.instance != null)
            {
                PlayerData.instance.MPReserve = 0;
            }
        }
    }
}