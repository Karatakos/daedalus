namespace Daedalus.Core.Commands;

using Microsoft.Extensions.Logging;
using System.Windows.Input;

using Arch.Core;

using Daedalus.Core.Components;
using Daedalus.Core.Tiled.Maps;
using Daedalus.Core.Network.Components;

public class LoadMapCommandHandler(World world, Entity player) : ICommandHander<LoadMapCmd> {
    public void Execute(LoadMapCmd command) {
        if (player == null)
            throw new Exception("Sever shouldnt be sending commands before authentication!");

        // HACK: Testing only pre-snapshot implementation. Only the server should be setting state.
        //
        world.Get<NetPlayerComponent>(player).State = NetPlayerState.BOOTSTRAPPING;

        // TODO: Parse static objects and spawn them in our ECS
        //
        // Ignores anything dynamic as the server will be serving state
        //

        // Offload to a rendering system by tagging player with the map
        //
        world.Add(player, new BootstrapComponent() { 
            Map = command.Map,
            MapLoaded = true });
    }
}
