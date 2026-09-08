using Hkmp.Api.Server;
using Hkmp.Api.Server.Networking;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;

using OneShotPvP.Networking;
using OneShotPvP.Networking.Packets;

namespace OneShotPvP.Server
{
    internal sealed class ServerNetManager
    {
        private readonly ServerManaManager _manaManager;
        private readonly RoundManager _roundManager;

        private readonly IServerAddonNetworkSender<
            OneShotClientPacketId> _netSender;

        private readonly IServerAddonNetworkReceiver<
            OneShotServerPacketId> _netReceiver;

        public ServerNetManager(
            OneShotServerAddon addon,
            IServerApi serverApi,
            ServerManaManager manaManager,
            RoundManager roundManager)
        {
            _manaManager = manaManager;
            _roundManager = roundManager;

            _netSender =
                serverApi.NetServer.GetNetworkSender<
                    OneShotClientPacketId>(
                    addon
                );

            _netReceiver =
                serverApi.NetServer.GetNetworkReceiver<
                    OneShotServerPacketId>(
                    addon,
                    InstantiatePacket
                );

            _netReceiver.RegisterPacketHandler(
                OneShotServerPacketId.ResetMana,
                OnResetMana
            );

            _netReceiver.RegisterPacketHandler<DeathReportPacket>(
                OneShotServerPacketId.DeathReport,
                OnDeathReport
            );

            _netReceiver.RegisterPacketHandler<ManaSpentPacket>(
                OneShotServerPacketId.ManaSpent,
                OnManaSpent
            );

            Modding.Logger.Log(
                "[OneShotPvP] ServerNetManager initialized."
            );
        }

        private static IPacketData InstantiatePacket(
            OneShotServerPacketId packetId)
        {
            switch (packetId)
            {
                case OneShotServerPacketId.ResetMana:
                    return new ReliableEmptyData();

                case OneShotServerPacketId.DeathReport:
                    return new DeathReportPacket();

                case OneShotServerPacketId.ManaSpent:
                    return new ManaSpentPacket();
            }

            return null;
        }

        private void OnResetMana(
            ushort playerId)
        {
            Modding.Logger.Log(
                "[OneShotPvP] Server received ResetMana. " +
                "PlayerId=" +
                playerId
            );

            _manaManager.ResetPlayer(
                playerId
            );

            SendMana(
                playerId
            );
        }

        private void OnManaSpent(
            ushort playerId,
            ManaSpentPacket packet)
        {
            Modding.Logger.Log(
                "[OneShotPvP] Server received ManaSpent. " +
                "PlayerId=" +
                playerId +
                " Amount=" +
                packet.Amount
            );

            int spent =
                _manaManager.SpendMana(
                    playerId,
                    packet.Amount
                );

            Modding.Logger.Log(
                "[OneShotPvP] Server ManaSpent result. " +
                "PlayerId=" +
                playerId +
                " Spent=" +
                spent +
                " CurrentMana=" +
                _manaManager.GetMana(
                    playerId
                )
            );

            if (spent <= 0)
            {
                return;
            }

            SendMana(
                playerId
            );
        }

        private void OnDeathReport(
            ushort playerId,
            DeathReportPacket packet)
        {
            ushort victimId =
                playerId;

            ushort killerId =
                packet.KillerId;

            Modding.Logger.Log(
                "[OneShotPvP] Server received DeathReport. " +
                "RoundId=" +
                packet.RoundId +
                " CurrentRoundId=" +
                _roundManager.CurrentRoundId +
                " VictimId=" +
                victimId +
                " KillerId=" +
                killerId
            );

            if (!_roundManager.IsRoundActive)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] DeathReport rejected: " +
                    "round is not active."
                );

                return;
            }

            if (packet.RoundId !=
                _roundManager.CurrentRoundId)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] DeathReport rejected: " +
                    "packet belongs to another round. " +
                    "PacketRoundId=" +
                    packet.RoundId +
                    " CurrentRoundId=" +
                    _roundManager.CurrentRoundId
                );

                return;
            }

            if (!_roundManager.IsAlive(
                victimId))
            {
                Modding.Logger.Log(
                    "[OneShotPvP] DeathReport rejected: " +
                    "victim is not alive in current round. " +
                    "VictimId=" +
                    victimId
                );

                return;
            }

            if (!_roundManager.IsAlive(
                killerId))
            {
                Modding.Logger.Log(
                    "[OneShotPvP] DeathReport rejected: " +
                    "killer is not alive in current round. " +
                    "KillerId=" +
                    killerId
                );

                return;
            }

            if (killerId == victimId)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] DeathReport rejected: " +
                    "killer and victim are the same player."
                );

                return;
            }

            Modding.Logger.Log(
                "[OneShotPvP] Before kill processing: " +
                "RoundId=" +
                _roundManager.CurrentRoundId +
                " VictimMana=" +
                _manaManager.GetMana(
                    victimId
                ) +
                " KillerMana=" +
                _manaManager.GetMana(
                    killerId
                ) +
                " VictimDead=" +
                _manaManager.IsDead(
                    victimId
                )
            );

            int reward;

            bool accepted =
                _manaManager.TryGiveKillReward(
                    killerId,
                    victimId,
                    out reward
                );

            Modding.Logger.Log(
                "[OneShotPvP] TryGiveKillReward result: " +
                "Accepted=" +
                accepted +
                " Reward=" +
                reward
            );

            if (!accepted)
            {
                Modding.Logger.Log(
                    "[OneShotPvP] DeathReport rejected by " +
                    "ServerManaManager."
                );

                return;
            }

            Modding.Logger.Log(
                "[OneShotPvP] Kill accepted. " +
                "RoundId=" +
                _roundManager.CurrentRoundId +
                " KillerId=" +
                killerId +
                " VictimId=" +
                victimId +
                " Reward=" +
                reward +
                " KillerManaAfter=" +
                _manaManager.GetMana(
                    killerId
                ) +
                " VictimManaAfter=" +
                _manaManager.GetMana(
                    victimId
                )
            );

            SendMana(
                killerId
            );

            SendMana(
                victimId
            );

            BroadcastPlayerDeath(
                victimId
            );

            Modding.Logger.Log(
                "[OneShotPvP] Calling RoundManager.OnPlayerDeath. " +
                "VictimId=" +
                victimId
            );

            _roundManager.OnPlayerDeath(
                victimId
            );

            Modding.Logger.Log(
                "[OneShotPvP] RoundManager.OnPlayerDeath returned."
            );
        }

        public void SendMana(
            ushort playerId)
        {
            int mana =
                _manaManager.GetMana(
                    playerId
                );

            _netSender.SendSingleData(
                OneShotClientPacketId.ManaUpdate,
                new ManaUpdatePacket(
                    mana
                ),
                playerId
            );
        }

        public void SendInitialMana(
            ushort playerId)
        {
            SendMana(
                playerId
            );
        }

        public void SendRoundStart(
            ushort playerId,
            uint roundId)
        {
            Modding.Logger.Log(
                "[OneShotPvP] Sending RoundStart. " +
                "RoundId=" +
                roundId +
                " PlayerId=" +
                playerId
            );

            _netSender.SendSingleData(
                OneShotClientPacketId.RoundStart,
                new RoundStartPacket(
                    roundId
                ),
                playerId
            );
        }

        public void BroadcastPlayerDeath(
            ushort playerId)
        {
            Modding.Logger.Log(
                "[OneShotPvP] Broadcasting PlayerDeath. " +
                "RoundId=" +
                _roundManager.CurrentRoundId +
                " PlayerId=" +
                playerId
            );

            _netSender.BroadcastSingleData(
                OneShotClientPacketId.PlayerDeath,
                new PlayerDeathPacket(
                    _roundManager.CurrentRoundId,
                    playerId
                )
            );
        }

        public void BroadcastRoundEnd(
            uint roundId,
            ushort winnerId)
        {
            Modding.Logger.Log(
                "[OneShotPvP] Broadcasting RoundEnd. " +
                "RoundId=" +
                roundId +
                " WinnerId=" +
                winnerId
            );

            _netSender.BroadcastSingleData(
                OneShotClientPacketId.RoundEnd,
                new RoundEndPacket(
                    roundId,
                    winnerId
                )
            );
        }
    }
}