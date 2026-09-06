using Hkmp.Api.Client;
using Hkmp.Api.Server;
using OneShotPvP.Client;
using OneShotPvP.Server;

namespace OneShotPvP
{
    internal static class OneShotPvPMod
    {
        public static void InitializeClient()
        {
            ClientAddon.RegisterAddon(
                new OneShotClientAddon()
            );
        }

        public static void InitializeServer()
        {
            ServerAddon.RegisterAddon(
                new OneShotServerAddon()
            );
        }
    }
}