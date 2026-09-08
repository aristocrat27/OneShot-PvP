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

            netReceiver.RegisterPacketHandler<RoundStartPacket>(
                OneShotClientPacketId.RoundStart,
                OnRoundStart
            );

            netReceiver.RegisterPacketHandler<PlayerDeathPacket>(
                OneShotClientPacketId.PlayerDeath,
                OnPlayerDeath
            );

            netReceiver.RegisterPacketHandler<RoundEndPacket>(
                OneShotClientPacketId.RoundEnd,
                OnRoundEnd
            );

            Modding.Logger.Log(
                "[OneShotPvP] ClientNetManager initialized."
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
                    return new RoundStartPacket();

                case OneShotClientPacketId.PlayerDeath:
                    return new PlayerDeathPacket();

                case OneShotClientPacketId.RoundEnd:
                    return new RoundEndPacket();
            }

            return null;
        }

        private void OnManaUpdate(
            ManaUpdatePacket packet)
        {
            Modding.Logger.Log(
                "[OneShotPvP] Client received ManaUpdate. " +
                "Mana=" +
                packet.Mana
            );

            ClientManaManager.SetMana(
                packet.Mana
            );
        }

        private void OnRoundStart(
            RoundStartPacket packet)
        {
            Modding.Logger.Log(
                "[OneShotPvP] Client received RoundStart. " +
                "RoundId=" +
                packet.RoundId
            );

            RoundClientManager.StartRound(
                packet.RoundId
            );
        }

        private void OnPlayerDeath(
            PlayerDeathPacket packet)
        {
            Modding.Logger.Log(
                "[OneShotPvP] Client received PlayerDeath. " +
                "RoundId=" +
                packet.RoundId +
                " PlayerId=" +
                packet.PlayerId
            );

            RoundClientManager.MarkPlayerDead(
                packet.RoundId,
                packet.PlayerId
            );
        }

        private void OnRoundEnd(
            RoundEndPacket packet)
        {
            Modding.Logger.Log(
                "[OneShotPvP] Client received RoundEnd. " +
                "RoundId=" +
                packet.RoundId +
                " WinnerId=" +
                packet.WinnerId
            );

            RoundClientManager.EndRound(
                packet.RoundId,
                packet.WinnerId
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
            uint roundId =
                RoundClientManager.CurrentRoundId;

            Modding.Logger.Log(
                "[OneShotPvP] SendDeathReport called. " +
                "RoundId=" +
                roundId +
                " KillerId=" +
                killerId
            );

            _netSender.SendSingleData(
                OneShotServerPacketId.DeathReport,
                new DeathReportPacket(
                    roundId,
                    killerId
                )
            );

            Modding.Logger.Log(
                "[OneShotPvP] DeathReport packet submitted to HKMP. " +
                "RoundId=" +
                roundId
            );
        }

        public void SendManaSpent(
            int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _netSender.SendSingleData(
                OneShotServerPacketId.ManaSpent,
                new ManaSpentPacket(
                    amount
                )
            );
        }
    }
}