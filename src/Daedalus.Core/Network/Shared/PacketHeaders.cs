namespace Daedalus.Core.Network;

using LiteNetLib.Utils;

public enum PacketType: byte {
    Error,
    Authentication,
    Command,
    SnapshotDelta,
    Status
}

public partial struct PacketHeader {
    public PacketType Type;
};

public partial struct PacketHeader {
    public static void Serialize(PacketHeader instance, NetDataWriter writer) {
        writer.Put((byte)instance.Type);
    }

    public static PacketHeader Deserialize(NetDataReader reader) {
        return new PacketHeader() {
            Type = (PacketType)reader.GetByte()
        };
    }
}
