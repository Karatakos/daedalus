namespace Daedalus.Core.Network.Server.Auth;

using Microsoft.Extensions.Logging;

using Arch.Core;
using Arch.Core.Extensions;

using LiteNetLib;

using Daedalus.Core.Network.Errors;
using Daedalus.Core.Network.Components;

public static class PlayerRegistrarExtensions {
    public static int GetAuthenticatedPlayerCount(this PlayerRegistrar instance) {
        return instance.Players.Count(
            entity => 
                instance.ECSWorld.Get<NetPlayerComponent>(entity).State == NetPlayerState.AUTHENTICATED);
    }

    public static int GetPlayerCount(this PlayerRegistrar instance) {
        return instance.Players.Length;
    }
}

public class PlayerRegistrar (World ECSWorld) {
    private Dictionary<int, Entity> _players = new ();

    public World ECSWorld { get; set; } = ECSWorld;
    public int AuthenticatedPlayerCount { get; private set; } = 0;

    public Entity[] Players {
        get { return _players.Values.ToArray(); }
    }

    public Entity RegisterNewPlayer(NetPeer peer) {
        if (TryGetRegisteredPlayerEntity(peer, out Entity? existingEntity)) {
            DS.Log.LogInformation($"Peer {peer.Id} @ {peer.Address} already registered");

            return (Entity)existingEntity;
        }

        var entity = ECSWorld.Create();

        entity.Add(new NetPlayerComponent() {
            NetId = entity.Id,
            Peer = new DaedalusNetPeer(peer),
            State = NetPlayerState.CONNECTED
        });
        
        // not ideal but it's convenient to keep track of player
        //
        _players.Add(peer.Id, entity);

        DS.Log.LogInformation($"Peer {peer.Id} @ {peer.Address} registered as an unauthenticated player");

        return entity;
    }

    public async void AuthenticateRegisteredPlayer(NetPeer peer, IAsyncIdentityAuthenticator authProvider) {
        if (!TryGetRegisteredPlayerEntity(peer, out Entity? entity)) {
            DS.Log.LogInformation($"Peer {peer.Id} @ {peer.Address} is not registered please reconnect and try again");

            return;
        }

        DS.Log.LogInformation($"Authenticating peer {peer.Id} @ {peer.Address}");

        var identityResult = await authProvider.AuthenticateAsync();
        if (identityResult.IsFailed) {
            DS.Log.LogInformation($"Authentication failed for peer {peer.Id} @ {peer.Address}");

            // We allow the player to remain connected but revisit this as we 
            // want to throttle auth retries and also cleanup clients that 
            // have not authenticated after n seconds
            //
            peer.Send([(byte)ServerErrors.AUTHENTICATION_FAILED], DeliveryMethod.ReliableOrdered);

            return;
        }
        
        ECSWorld.Get<NetPlayerComponent>((Entity)entity).Identity = identityResult.Value;
        ECSWorld.Get<NetPlayerComponent>((Entity)entity).State = NetPlayerState.AUTHENTICATED;

        AuthenticatedPlayerCount++;

        DS.Log.LogInformation($"Player {ECSWorld.Get<NetPlayerComponent>((Entity)entity).Identity.Username} authenticated");
    }

    public async void UnregisterDisconnectedPlayer(NetPeer peer) {
        if (_players.ContainsKey(peer.Id))
            _players.Remove(peer.Id);
    }

    public bool TryGetRegisteredPlayerEntity(NetPeer peer, out Entity? entity) {
        entity = null;

        if (_players.ContainsKey(peer.Id)) {
            if (ECSWorld.IsAlive(_players[peer.Id])) {
                entity = _players[peer.Id];

                return true;
            }

            // Assuming player entity was destroyed so remove from registry
            // 
            _players.Remove(peer.Id);
        }

        return false;
    }
}
