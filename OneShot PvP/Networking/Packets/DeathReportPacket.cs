using Hkmp.Networking.Packet;

namespace OneShotPvP.Networking.Packets
{
    internal sealed class DeathReportPacket : IPacketData
    {
        public ushort KillerId { get; private set; }

        public bool IsReliable => true;

        public bool DropReliableDataIfNewerExists => false;

        public DeathReportPacket()
        {
        }

        public DeathReportPacket(ushort killerId)
        {
            KillerId = killerId;
        }

        public void WriteData(IPacket packet)
        {
            packet.Write(KillerId);
        }

        public void ReadData(IPacket packet)
        {
            KillerId = packet.ReadUShort();
        }
    }
}