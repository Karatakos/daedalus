namespace Daedalus.Core.Network;

using LiteNetLib;

public interface INetPacketProcessor {
    public void Process(NetPeer peer, PacketHeader packet, NetPacketReader reader);
}