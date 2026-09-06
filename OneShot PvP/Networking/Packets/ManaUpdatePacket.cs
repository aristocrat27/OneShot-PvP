using Hkmp.Networking.Packet;

namespace OneShotPvP.Networking.Packets
{
    internal sealed class ManaUpdatePacket : IPacketData
    {
        public int Mana { get; private set; }

        public bool IsReliable => true;

        public bool DropReliableDataIfNewerExists => true;

        public ManaUpdatePacket()
        {
        }

        public ManaUpdatePacket(int mana)
        {
            Mana = mana;
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(Mana);
        }

        public void ReadData(IPacket packet)
        {
            Mana = packet.ReadInt();
        }
    }
}