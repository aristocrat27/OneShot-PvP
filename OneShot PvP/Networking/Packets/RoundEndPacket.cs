using Hkmp.Networking.Packet;

namespace OneShotPvP.Networking.Packets
{
    internal sealed class RoundEndPacket : IPacketData
    {
        public uint RoundId { get; private set; }

        public ushort WinnerId { get; private set; }

        public bool IsTeamVictory
        {
            get
            {
                return WinnerTeam != byte.MaxValue;
            }
        }

        public byte WinnerTeam { get; private set; }

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
            WinnerTeam = byte.MaxValue;
        }

        public RoundEndPacket(
            uint roundId,
            ushort winnerId)
        {
            RoundId = roundId;
            WinnerId = winnerId;
            WinnerTeam = byte.MaxValue;
        }

        public RoundEndPacket(
            uint roundId,
            byte winnerTeam)
        {
            RoundId = roundId;
            WinnerId = 0;
            WinnerTeam = winnerTeam;
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

            packet.Write(
                WinnerTeam
            );
        }

        public void ReadData(
            IPacket packet)
        {
            RoundId =
                packet.ReadUInt();

            WinnerId =
                packet.ReadUShort();

            WinnerTeam =
                packet.ReadByte();
        }
    }
}