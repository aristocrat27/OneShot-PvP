using Hkmp.Networking.Packet;

namespace OneShotPvP.Networking.Packets
{
    internal sealed class RoundEndPacket : IPacketData
    {
        public uint RoundId { get; private set; }

        public ushort WinnerId { get; private set; }

        public bool IsReliable
        {
            get { return true; }
        }

        public bool DropReliableDataIfNewerExists
        {
            get { return false; }
        }

        public RoundEndPacket()
        {
        }

        public RoundEndPacket(
            uint roundId,
            ushort winnerId)
        {
            RoundId = roundId;
            WinnerId = winnerId;
        }

        public void WriteData(
            IPacket packet)
        {
            packet.Write(
                RoundId
            );

            packet.Write(
                WinnerId
            );
        }

        public void ReadData(
            IPacket packet)
        {
            RoundId =
                packet.ReadUInt();

            WinnerId =
                packet.ReadUShort();
        }
    }
}