namespace Daedalus.Core.Commands;

using Microsoft.Extensions.Logging;

using MemoryPack;

using Daedalus.Core.Tiled.Maps;

public static class CommandSerializer {
    public static ICommand Deserialize(byte[] payload) {
        return MemoryPackSerializer.Deserialize<ICommand>(payload);
    }

    public static byte[] Serialize(ICommand cmd) {
        return MemoryPackSerializer.Serialize(cmd);  
    }
}