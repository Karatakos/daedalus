namespace Daedalus.Core.Systems;

using Microsoft.Xna.Framework;
using Microsoft.Extensions.Logging;
using System.Data;

using MemoryPack;
using LiteNetLib;
using Arch;
using Arch.Core;

using Daedalus.Core;
using Daedalus.Core.Network;
using Daedalus.Core.Commands;
using Daedalus.Core.Network.Errors;
using Daedalus.Core.Components;
using Daedalus.Core.Tiled.Maps;
using Daedalus.Core.Network.Components;

public class MapRenderingSystem(World world, GraphicsDeviceManager graphics) : System<GameTime>(world) {
    private QueryDescription _queryDesc = new QueryDescription()
        .WithAll<BootstrapComponent>();

    public override void Update(in GameTime gameTime) {
        world.Query(in _queryDesc, (ref BootstrapComponent map) => {
            // TODO: Render :)

            DS.Log.LogInformation($"Map rendered with tile width {map.Map.Width} and height {map.Map.Height}");
        });
    }
}