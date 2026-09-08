using Hkmp.Networking.Packet;

namespace OneShotPvP.Networking.Packets
{
    internal sealed class PlayerDeathPacket : IPacketData
    {
        public uint RoundId { get; private set; }

        public ushort PlayerId { get; private set; }

        public bool IsReliable
        {
            get { return true; }
        }

        public bool DropReliableDataIfNewerExists
        {
            get { return false; }
        }

        public PlayerDeathPacket()
        {
        }

        public PlayerDeathPacket(
            uint roundId,
            ushort playerId)
        {
            RoundId = roundId;
            PlayerId = playerId;
        }

        public void WriteData(
            IPacket packet)
        {
            packet.Write(
                RoundId
            );

            packet.Write(
                PlayerId
            );
        }

        public void ReadData(
            IPacket packet)
        {
            RoundId =
                packet.ReadUInt();

            PlayerId =
                packet.ReadUShort();
        }
    }
}