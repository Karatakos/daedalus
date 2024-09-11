namespace Daedalus.Core.Network.Client;

using System.Net;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;

using Arch.Core;
using LiteNetLib;

using Daedalus.Core.Network.Errors;

/*  Implements INetEventListener to route events to a CommandProcessor
*
*   
*/
public class GameClientNetHandler(string authenticationJwt): INetEventListener {
    public event Action<byte, byte[]> OnServerCommand;
    public event Action<byte, string> OnServerError;
    public event Action<byte> OnServerStatusUpdate;
    public event Action OnWorldStateUpdated;
    public event Action<DaedalusNetPeer> OnAuthenticated;

    public void OnConnectionRequest(ConnectionRequest request) {
        // Ignore
    } 

    public void OnPeerConnected(NetPeer peer) {
        DS.Log.LogInformation($"Connected. Sending auth request to join game on server {peer.Address}"); 
            
        var daedalusPeer = new DaedalusNetPeer(peer);
        daedalusPeer.SendAuthenticationRequest(authenticationJwt);

        // HACK: Server should be signalling auth status
        //
        DS.Log.LogInformation($"Authenticated. Ready for bootstraping."); 

        OnAuthenticated(daedalusPeer);
    } 

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info) {
        // Try reconnect?
    } 

    public void OnNetworkReceive(
        NetPeer peer, 
        NetPacketReader reader, 
        byte firstByte, 
        DeliveryMethod method) {

        var header = new PacketHeader() {
            Type = (PacketType)reader.GetByte()
        };

        DS.Log.LogInformation($"Received packet type {header.Type} from peer {peer.Address} of total size {(float)reader.UserDataSize/1024} kb");

        switch (header.Type) {
            case PacketType.Command: 
                var type = reader.GetByte();

                OnServerCommand(type, reader.GetRemainingBytes());

                break;

            case PacketType.SnapshotDelta:
                DS.Log.LogInformation($"SnapshotDelta received from server");

                OnWorldStateUpdated();

                break;

            case PacketType.Status: 
                var status = reader.GetByte();

                DS.Log.LogInformation($"Server status update: {status}");

                OnServerStatusUpdate(status);

                break;

            case PacketType.Error: 
                var code = reader.GetByte();
                var message = reader.GetString();

                DS.Log.LogInformation($"Server responded with error: {message} code: {code}");

                OnServerError(code, message);

                break;

            default: 
                peer.Send(
                    [(byte)PacketType.Error, (byte)ServerErrors.PACKET_TYPE_UNSUPPORTED],
                    DeliveryMethod.ReliableOrdered);

                DS.Log.LogInformation($"Net packet type {header.Type} not supported on the client");

                break;
        }
    } 

    public void OnNetworkReceiveUnconnected(
        IPEndPoint endpoint, 
        NetPacketReader reader, 
        UnconnectedMessageType type) {

        // Ignore it we don't support unsolicited messages
    } 

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) {
        // Ignore for now but maybe useful for RTT updates if we need an event
    } 

    public void OnNetworkError(IPEndPoint endpoint, SocketError error) {
        DS.Log.LogInformation($"Error {error} occurred on endpoint {endpoint.Address}");
    } 
}
