using Hkmp.Networking.Packet;

namespace OneShotPvP.Networking.Packets
{
    internal sealed class PlayerDeathPacket : IPacketData
    {
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
            ushort playerId)
        {
            PlayerId = playerId;
        }

        public void WriteData(
            IPacket packet)
        {
            packet.Write(
                PlayerId
            );
        }

        public void ReadData(
            IPacket packet)
        {
            PlayerId =
                packet.ReadUShort();
        }
    }
}