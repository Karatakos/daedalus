namespace Daedalus.Core.Network.Server;

using System.Net;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;

using Arch.Core;
using LiteNetLib;

using Daedalus.Core.Network.Server.Auth;
using Daedalus.Core.Network.Errors;

/*  Implements INetEventListener to procss snapshot deltas, actions and authentication
*
*   
*/
public class GameServerNetHandler(PlayerRegistrar playerRegistrar): INetEventListener {
    public event Action<DaedalusNetPeer, byte, byte[]> OnClientCommand;
    public event Action<DaedalusNetPeer, byte, string> OnClientError;
    public event Action<DaedalusNetPeer, byte> OnClientStatusUpdate;

    public void OnConnectionRequest(ConnectionRequest request) {
        if(playerRegistrar.GetPlayerCount() < 4 /* max players */) {
            request.AcceptIfKey(DS.Config.MatchmakerToken.ConnectionKey);
        }
        else {
            request.Reject([(byte)ServerErrors.MAX_PLAYERS_REACHED]);
        }
    } 

    public void OnPeerConnected(NetPeer peer) {
        DS.Log.LogInformation($"Peer {peer.Id} @ {peer.Address} connected but not yet registered");

        playerRegistrar.RegisterNewPlayer(peer);
    } 

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info) {
        DS.Log.LogInformation($"Peer {peer.Id} @ {peer.Address} disconnected.");

        playerRegistrar.UnregisterDisconnectedPlayer(peer);
    } 

    public void OnNetworkReceive(
        NetPeer peer, 
        NetPacketReader reader, 
        byte firstByte, 
        DeliveryMethod method) {

        var header = PacketHeader.Deserialize(reader); 

        DS.Log.LogInformation($"Processing packet type {header.Type} from peer {peer.Address}");

        var client = new DaedalusNetPeer(peer);

        switch (header.Type) {
            case PacketType.Authentication: 
                var token = reader.GetString();

                playerRegistrar.AuthenticateRegisteredPlayer(
                    peer, new JWTIdentityAuthenticator(token));

                break;

            case PacketType.Status: 
                var status = reader.GetByte();

                DS.Log.LogInformation($"Client status update: {status}");

                OnClientStatusUpdate(client, status);

                break;

            case PacketType.Error: 
                var code = reader.GetByte();
                var message = reader.GetString();

                DS.Log.LogInformation($"Client responded with error: {message} code: {code}");

                OnClientError(client, code, message);

                break;

            case PacketType.Command:
                var type = reader.GetByte();

                OnClientCommand(client, type, reader.GetRemainingBytes());

                break;

            default: 
                peer.Send(
                    [(byte)PacketType.Error, (byte)ServerErrors.PACKET_TYPE_UNSUPPORTED],
                    DeliveryMethod.ReliableOrdered);

                DS.Log.LogInformation($"Net packet type {header.Type} not supported on the server");

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
