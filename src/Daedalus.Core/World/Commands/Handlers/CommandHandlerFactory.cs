namespace Daedalus.Core.Commands;

using Arch.Core;

public static class CommandHandlerFactory {
    public static dynamic Get(CommandType type, World world, Entity player) {
        switch (type) {
            case CommandType.LoadMap: 
                return new LoadMapCommandHandler(world, player);
                
            default: 
                throw new ArgumentException($"Command {type} not supported.");
        }
    }
}