namespace Daedalus.Core.Commands;

using System.Windows.Input;
using Microsoft.Extensions.Logging;

using MemoryPack;
using Arch.Core;

using Daedalus.Core.Tiled.Maps;

public static class CommandHandlerFactory {
    public static dynamic Get(CommandType type, World world, Entity player) {
        switch (type) {
            case CommandType.LoadMap: 
                return new LoadMapCommandHandler(world, player);
                
            default: 
                throw new ArgumentException($"Command type {type} not supported.");
        }
    }
}