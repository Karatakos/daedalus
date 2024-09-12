namespace Daedalus.Game.Client;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;

using LiteNetLib;
using LiteNetLib.Utils;

using Arch.Core;

using Daedalus.Core;
using Daedalus.Core.Commands;
using Daedalus.Core.Network;
using Daedalus.Core.Network.Client;
using Daedalus.Core.Network.Errors;
using Daedalus.Core.Network.Components;
using Daedalus.Core.Systems;
using Daedalus.Core.Components;

public class GameClient: DaedalusGame {
    private NetManager _net;
    private World _world;
    private DaedalusNetPeer _serverPeer;
    private Entity _player;
    private MapRenderingSystem _mapRenderingSystem;

    private GraphicsDeviceManager _graphics;

    public GameClient(ILoggerFactory logFactory, int fixedStepInFrames): base(fixedStepInFrames) {
        // Initialize config & logging
        //
        DS.Config = new StaticConfiguration() { MatchmakerToken = new MatchmakerToken("somekey") };
        DS.LogFactory = logFactory;
        DS.Log = logFactory.CreateLogger("Client");

        _graphics = new GraphicsDeviceManager(this) {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720
        };
        
        _graphics.ApplyChanges();

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    public GameClient(ILoggerFactory logFactory): this(logFactory, 30) {}

    protected override void Initialize(){
        _world = World.Create();

        // Intialize systems
        //
        _mapRenderingSystem = new MapRenderingSystem(_world, _graphics);

        var listener = new GameClientNetHandler("some-jwt");

        listener.OnServerError += (code, message) => DS.Log.LogError($"Server error code: {(ServerErrors)code} message: {message}");
        listener.OnServerStatusUpdate += (status) => DS.Log.LogInformation($"Server status changed to {status}");
        listener.OnWorldStateUpdated += () => DS.Log.LogInformation("Snapshot processed");
        listener.OnServerCommand += (payload) => HandleServerCommand(payload);
        listener.OnAuthenticated += (peer) => HandleAuthenticated(peer);

        _net = new NetManager(listener);
        
        _net.Start();

        DS.Log.LogInformation($"Attempting to connect to server localhost:{DS.Config.Port}"); 

        _serverPeer = new DaedalusNetPeer(
            _net.Connect("localhost", DS.Config.Port, DS.Config.MatchmakerToken.ConnectionKey));
        
        base.Initialize();
    }
    protected override void LoadContent(){
        base.LoadContent();
    }

    protected override void Draw(GameTime gameTime){
        if (!_world.IsAlive(_player) || !_world.Has<NetPlayerComponent>(_player))
            return;

        switch (_world.Get<NetPlayerComponent>(_player).State) {
            case NetPlayerState.INGAME: 
                _mapRenderingSystem.Update(gameTime);

                break;
        }

        base.Draw(gameTime);
    }

    protected override void VariableUpdate(GameTime gameTime){
    }

    protected override void FixedUpdate(GameTime gameTime){
        _net.PollEvents();

        if (!_world.IsAlive(_player) || !_world.Has<NetPlayerComponent>(_player))
            return;

        switch (_world.Get<NetPlayerComponent>(_player).State) {
            case NetPlayerState.BOOTSTRAPPING: 
                // TODO: Move this into a system?

                var bootstrappingComponent = _world.Get<BootstrapComponent>(_player);

                // Signal the server with a bootstrapped transition if we're ready
                //
                if (bootstrappingComponent.MapLoaded) {
                    _serverPeer.SendStatus((byte)NetPlayerStateTransitions.FINISHED_BOOTSTRAPPING);

                    // HACK: Server should be setting state. We only request state transitions 
                    //       Waiting for snapshot implementation until then skip waiting state and go straight to ingame
                    // 
                    _world.Get<NetPlayerComponent>(_player).State = NetPlayerState.INGAME;
                }

                break;

            case NetPlayerState.WAITING: 
                break;

            case NetPlayerState.INGAME: 
                break;
        }
    }

    protected void HandleAuthenticated(DaedalusNetPeer peer) {
        // Create a player entity
        //
        _player = _world.Create(new NetPlayerComponent() {
            NetId = peer.Peer.Id,
            Peer = peer,
            // State will from hereonout be controlled by the server
            //
            State = NetPlayerState.AUTHENTICATED,  
            Identity = new NetPlayerIdentity() {  Username = "Beefcake", UserId = "1" }
        });
    }

    protected void HandleServerCommand(byte[] payload){
        var cmd = CommandSerializer.Deserialize(payload);

        DS.Log.LogInformation($"Received Command {cmd.Type} of size {(float)payload.Length/1024} kb");

        CommandHandlerFactory.Get(cmd.Type, _world, _player).Execute((dynamic)cmd);
    }
}
