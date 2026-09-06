using Hkmp.Networking.Packet;

namespace OneShotPvP.Networking.Packets
{
    internal sealed class ManaSpentPacket : IPacketData
    {
        public int Amount { get; private set; }

        public bool IsReliable => true;

        public bool DropReliableDataIfNewerExists => false;

        public ManaSpentPacket()
        {
        }

        public ManaSpentPacket(int amount)
        {
            Amount = amount;
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(Amount);
        }

        public void ReadData(IPacket packet)
        {
            Amount = packet.ReadInt();
        }
    }
}