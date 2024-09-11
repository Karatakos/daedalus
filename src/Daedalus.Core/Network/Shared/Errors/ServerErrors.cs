namespace Daedalus.Core.Network.Errors;

public enum ServerErrors: byte {
    MAX_PLAYERS_REACHED = 0,
    AUTHENTICATION_FAILED = 1,
    PACKET_TYPE_UNSUPPORTED = 2
}
