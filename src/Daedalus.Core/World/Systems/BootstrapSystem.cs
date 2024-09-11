namespace Daedalus.Core.Systems;

using Arch;
using Arch.Core;

using System.Data;

using Microsoft.Xna.Framework;
using Microsoft.Extensions.Logging;

using Daedalus.Core;
using Daedalus.Core.Tiled.Maps;
using Daedalus.Core.Network.Components;

using MemoryPack;
using Daedalus.Core.Network;
using Daedalus.Core.Commands;

using LiteNetLib;
using Daedalus.Core.Network.Errors;

public class BootstrapSystem(World world, TiledMapDungen map) : System<GameTime>(world) {
    private QueryDescription _queryDesc = new QueryDescription()
        .WithAll<NetPlayerComponent>();

    public override void Update(in GameTime gameTime) {
        world.Query(in _queryDesc, (ref NetPlayerComponent player) => {
            // We're only looking for players still in AUTHENTICATED state
            //
            switch (player.State) {
                case NetPlayerState.AUTHENTICATED:
                    player.State = NetPlayerState.BOOTSTRAPPING;

                    player.Peer.SendCommand(
                        (byte)Commands.CommandType.LoadMap, 
                        CommandFactory.Serialize(new LoadMapCmd(map)));

                    DS.Log.LogInformation($"Command {Commands.CommandType.LoadMap} issued to player [NetId]: {player.NetId} [Username]: {player.Identity.Username}");

                    break;

                case NetPlayerState.BOOTSTRAPPING:
                    // TODO: We should time out
                    
                    break;
            }   
        });
    }
}