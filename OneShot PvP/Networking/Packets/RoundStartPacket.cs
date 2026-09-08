using Hkmp.Networking.Packet;

namespace OneShotPvP.Networking.Packets
{
    internal sealed class RoundStartPacket : IPacketData
    {
        public uint RoundId { get; private set; }

        public bool IsReliable
        {
            get { return true; }
        }

        public bool DropReliableDataIfNewerExists
        {
            get { return false; }
        }

        public RoundStartPacket()
        {
        }

        public RoundStartPacket(
            uint roundId)
        {
            RoundId = roundId;
        }

        public void WriteData(
            IPacket packet)
        {
            packet.Write(
                RoundId
            );
        }

        public void ReadData(
            IPacket packet)
        {
            RoundId =
                packet.ReadUInt();
        }
    }
}