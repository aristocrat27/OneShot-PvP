using Modding;

namespace OneShotPvP.Client
{
    internal static class ManaConsumptionTracker
    {
        private static ClientNetManager _network;

        public static void Initialize(ClientNetManager network)
        {
            _network = network;

            On.PlayerData.TakeMP += OnTakeMP;
        }

        private static void OnTakeMP(
            On.PlayerData.orig_TakeMP orig,
            PlayerData self,
            int amount)
        {
            int before = self.GetInt("MPCharge");

            orig(self, amount);

            int after = self.GetInt("MPCharge");

            int spent = before - after;

            if (spent <= 0)
            {
                return;
            }

            ClientManaManager.OnManaSpent(spent);

            if (_network != null)
            {
                _network.SendManaSpent(spent);
            }
        }
    }
}