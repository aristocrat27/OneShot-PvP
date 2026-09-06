using Hkmp.Api.Client;
using Hkmp.Api.Client.Networking;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

using OneShotPvP.Networking;
using OneShotPvP.Networking.Packets;

namespace OneShotPvP.Client
{
    internal sealed class ClientNetManager
    {
        private readonly IClientAddonNetworkSender<
            OneShotServerPacketId> _netSender;

        public ClientNetManager(
            OneShotClientAddon addon,
            INetClient netClient)
        {
            _netSender =
                netClient.GetNetworkSender<
                    OneShotServerPacketId>(
                    addon
                );

            var netReceiver =
                netClient.GetNetworkReceiver<
                    OneShotClientPacketId>(
                    addon,
                    InstantiatePacket
                );

            netReceiver.RegisterPacketHandler<ManaUpdatePacket>(
                OneShotClientPacketId.ManaUpdate,
                OnManaUpdate
            );

            netReceiver.RegisterPacketHandler(
                OneShotClientPacketId.RoundStart,
                OnRoundStart
            );

            netReceiver.RegisterPacketHandler<PlayerDeathPacket>(
                OneShotClientPacketId.PlayerDeath,
                OnPlayerDeath
            );
        }

        private static IPacketData InstantiatePacket(
            OneShotClientPacketId packetId)
        {
            switch (packetId)
            {
                case OneShotClientPacketId.ManaUpdate:
                    return new ManaUpdatePacket();

                case OneShotClientPacketId.RoundStart:
                    return new ReliableEmptyData();

                case OneShotClientPacketId.PlayerDeath:
                    return new PlayerDeathPacket();
            }

            return null;
        }

        private void OnManaUpdate(
            ManaUpdatePacket packet)
        {
            ClientManaManager.SetMana(packet.Mana);
        }

        private void OnRoundStart()
        {
            Modding.Logger.Log(
                "[OneShotPvP] Client received RoundStart."
            );

            RoundClientManager.StartRound();
        }

        private void OnPlayerDeath(
            PlayerDeathPacket packet)
        {
            Modding.Logger.Log(
                "[OneShotPvP] Client received PlayerDeath. " +
                "PlayerId=" +
                packet.PlayerId
            );

            RoundClientManager.MarkPlayerDead(
                packet.PlayerId
            );
        }

        public void SendResetMana()
        {
            Modding.Logger.Log(
                "[OneShotPvP] Sending ResetMana."
            );

            _netSender.SendSingleData(
                OneShotServerPacketId.ResetMana,
                new ReliableEmptyData()
            );
        }

        public void SendDeathReport(
            ushort killerId)
        {
            Modding.Logger.Log(
                "[OneShotPvP] SendDeathReport called. " +
                "KillerId=" +
                killerId
            );

            _netSender.SendSingleData(
                OneShotServerPacketId.DeathReport,
                new DeathReportPacket(killerId)
            );

            Modding.Logger.Log(
                "[OneShotPvP] DeathReport packet submitted to HKMP."
            );
        }

        public void SendManaSpent(
            int amount)
        {
            if (amount <= 0)
                return;

            _netSender.SendSingleData(
                OneShotServerPacketId.ManaSpent,
                new ManaSpentPacket(amount)
            );
        }
    }
}