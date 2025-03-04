namespace Daedalus.Core.Network.Components;

using LiteNetLib;

public enum NetPlayerStateTransitions : byte {
    FINISHED_BOOTSTRAPPING = 0,
}

public enum NetPlayerState: byte {
    DISCONNECTED = 0,
    CONNECTED = 1,
    AUTHENTICATED = 3,
    BOOTSTRAPPING = 4,
    WAITING = 5,
    INGAME = 6
}

public struct NetPlayerIdentity {
    public string Username;
    public string UserId;
};

public struct NetPlayerComponent {
    public int NetId;
    public DaedalusNetPeer Peer;
    public NetPlayerState State;
    public NetPlayerIdentity Identity;
}

