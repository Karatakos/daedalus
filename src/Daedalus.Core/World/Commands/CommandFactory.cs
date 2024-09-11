namespace Daedalus.Core.Commands;

using Microsoft.Extensions.Logging;

using MemoryPack;

using Daedalus.Core.Tiled.Maps;

public static class CommandFactory {
    public static ICommand Deserialize(CommandType type, byte[] payload) {
        switch (type) {
            case CommandType.LoadMap: 
                try {
                    var cmd = new LoadMapCmd(
                        MemoryPackSerializer.Deserialize<TiledMapDungen>(payload));
                    
                    DS.Log.LogInformation($"Received map of width {cmd.Map.Width} from the server server");

                    return cmd;
                }
                catch (Exception ex) {
                    DS.Log.LogInformation($"Error parsing map data from server load map action");
                    
                    return null;    
                }
                
            default: 
                throw new ArgumentException($"Command type {type} not supported.");
        }
    }

    public static byte[] Serialize(ICommand cmd) {
        switch (cmd.Type) {
            case CommandType.LoadMap: 
                return MemoryPackSerializer.Serialize<LoadMapCmd>((LoadMapCmd)cmd);  

                break;

            default: 
                throw new ArgumentException($"Command type {cmd.Type} not supported.");
        }
    }
}