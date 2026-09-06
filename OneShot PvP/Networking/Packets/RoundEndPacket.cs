using Hkmp.Networking.Packet;

namespace OneShotPvP.Networking.Packets
{
    internal sealed class RoundEndPacket : IPacketData
    {
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
            ushort winnerId)
        {
            WinnerId = winnerId;
        }

        public void WriteData(
            IPacket packet)
        {
            packet.Write(WinnerId);
        }

        public void ReadData(
            IPacket packet)
        {
            WinnerId = packet.ReadUShort();
        }
    }
}