namespace Daedalus.Core.Network;

using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;

public class DaedalusNetPeer (NetPeer peer) {
    public NetPeer Peer { get => peer; }

    public void SendCommand(byte[] bytes) {
        var header = new PacketHeader() { 
            Type = PacketType.Command 
        };

        var writer = new NetDataWriter();
        
        PacketHeader.Serialize(header, writer);
        writer.Put(bytes);

        Send(writer.Data, DeliveryMethod.ReliableOrdered);
    }

    public void SendAuthenticationRequest(string token) {
        var header = new PacketHeader() { 
            Type = PacketType.Authentication 
        };

        var writer = new NetDataWriter();

        PacketHeader.Serialize(header, writer);
        writer.Put(token);

        Send(writer.Data, DeliveryMethod.ReliableOrdered);
    }
    public void SendSnapshotDelta() {
        throw new NotImplementedException();
    }

    public void SendError(byte code, string message) {
        var header = new PacketHeader() { 
            Type = PacketType.Error 
        };

        var writer = new NetDataWriter();

        PacketHeader.Serialize(header, writer);
        writer.Put(code);
        writer.Put(message);

        Send(writer.Data, DeliveryMethod.ReliableOrdered);
    }

    public void SendStatus(byte code) {
        var header = new PacketHeader() { 
            Type = PacketType.Status 
        };

        var writer = new NetDataWriter();

        PacketHeader.Serialize(header, writer);
        writer.Put(code);

        Send(writer.Data, DeliveryMethod.ReliableOrdered);
    }

    public void Send(byte[] bytes, DeliveryMethod method) {
        DS.Log.LogInformation($"[Send] Payload size {(float)bytes.Length/1024} kb");

        Peer.Send(bytes, method);
    }
}