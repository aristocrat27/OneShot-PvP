using Hkmp.Api.Client;

namespace OneShotPvP.Client
{
    internal sealed class OneShotClientAddon : ClientAddon
    {
        private ClientNetManager _network;

        protected override string Name
        {
            get
            {
                return OneShotConstants.Name;
            }
        }

        protected override string Version
        {
            get
            {
                return OneShotConstants.Version;
            }
        }

        public override bool NeedsNetwork
        {
            get
            {
                return true;
            }
        }

        public override void Initialize(
            IClientApi clientApi)
        {
            _network = new ClientNetManager(
                this,
                clientApi.NetClient
            );

            ManaCollectionBlocker.Initialize();

            ManaConsumptionTracker.Initialize(
                _network
            );

            LastAttackerTracker.Initialize();

            ClientDeathTracker.Initialize(
                _network
            );

            RoundClientManager.Clear();

            On.HeroController.Update +=
                OnHeroUpdate;
        }

        private void OnHeroUpdate(
            On.HeroController.orig_Update orig,
            HeroController self)
        {
            orig(self);

            ClientManaManager.Update();
        }

        public ClientNetManager Network
        {
            get
            {
                return _network;
            }
        }
    }
}