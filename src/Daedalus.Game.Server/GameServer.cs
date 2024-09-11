namespace Daedalus.Server;

using System.Data;
using System.Security.Principal;
using System.Text.Unicode;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.IO.Abstractions;

using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;

using Arch;
using Arch.Core;

using Daedalus.Core;
using Daedalus.Core.Systems;
using Daedalus.Core.Commands;
using Daedalus.Core.Network;
using Daedalus.Core.Network.Server.Auth;
using Daedalus.Core.Network.Errors;
using Daedalus.Core.Network.Server;
using Daedalus.Core.Tiled.Maps;
using Daedalus.Core.Tiled.Procedural;
using Daedalus.Core.Tiled.Procedural.ContentProviders;

using LiteNetLib;
using LiteNetLib.Utils;

using FluentResults;
using Daedalus.Core.Network.Components;

public class GameServer: DaedalusGameHeadless {
    private World _world;
    private NetManager _server;
    private PlayerRegistrar _playerRegistrar;
    private ServerState _state = ServerState.StartingUp;
    private SmartTiledMapGenerator _mapGen;
    private BootstrapSystem _playerBootstrapSystem;

    public GameServer(
        ILoggerFactory logFactory,
        MatchmakerToken matchmakerToken, 
        string contentDir,
        int fixedStepInFrames): base(fixedStepInFrames) {
            
        // Initialize config & logging
        //
        DS.Config = new StaticConfiguration() { MatchmakerToken = matchmakerToken };
        DS.LogFactory = logFactory;
        DS.Log = logFactory.CreateLogger("Server");

        // TODO: Get rid of this junk class
        //
        _mapGen = new SmartTiledMapGenerator(new FileSystem(), contentDir);
    }

    public GameServer(
        ILoggerFactory logFactory, 
        MatchmakerToken matchmakerToken,
        string contentDir): this(logFactory, matchmakerToken, contentDir, 30) {
    }

    protected override void Initialize() {
        // Our ECS engine
        //
        _world = World.Create();

        // Initialize player registry: authenticates and registers new players
        //
        _playerRegistrar = new PlayerRegistrar(_world);

        // Initialize systems
        //

        // Kick off map generation asynchronously
        //
        _mapGen.GenerateAsync("simple-loop");

        var handler = new GameServerNetHandler(_playerRegistrar);

        handler.OnClientError += (client, code, message) => HandleClientError(client, code, message);
        handler.OnClientStatusUpdate += (client, code) => HandleClientStatusUpdate(client, code);
        handler.OnClientCommand += (client, type, payload) => HandleClientCommand(client, type, payload);

        // Initialize network listener
        // 
        _server = new NetManager(handler);

        // Start accepting connections
        //
        _server.Start(DS.Config.Port);

        _state = ServerState.Initializing;
        DS.Log.LogInformation($"Server state changed to: {_state}");
    }

    protected override void LoadContent() {}

    protected override void VariableUpdate(GameTime gameTime) {
        //Logger.LogInformation($"Dt: {gameTime.ElapsedGameTime.TotalMilliseconds}");
    }

    protected override void FixedUpdate(GameTime gameTime) {
        // Listener will:
        //  - Handle connections including authentication and player entity creation 
        //  - Route snapshot and actions back to ECS by assigning components to player entieis
        //  
        // TODO: "Poll Events" infers something different
        //
        _server.PollEvents();

        switch (_state) {
            case ServerState.Initializing:
                if (_mapGen.Success) {
                    _playerBootstrapSystem = new BootstrapSystem(_world, _mapGen.Map);

                    _state = ServerState.InLobby;
                    DS.Log.LogInformation($"Server state changed to: {_state}");
                }

                break;

            case ServerState.InLobby:
                _playerBootstrapSystem.Update(gameTime);

                // Are we all ready?
                //
                if (_playerRegistrar.Players.Count(
                    entity => 
                        _world.Get<NetPlayerComponent>(entity).State == NetPlayerState.WAITING ) == 1) {
        
                    _state = ServerState.InGame;       

                    DS.Log.LogInformation($"Got enough (1) players to start game");
                    DS.Log.LogInformation($"Server state changed to: {_state}");
                }

                break;

            case ServerState.InGame:
                break;

            case ServerState.ShuttingDown:
                break;

            default:
                break;
        }

        //Logger.LogInformation($"Fixed Dt: {gameTime.ElapsedGameTime.TotalMilliseconds} Interpolant: {Interpolant}");
    }

    protected void HandleClientCommand(DaedalusNetPeer client, byte type, byte[] payload){
        var cmdType = (Daedalus.Core.Commands.CommandType)type;

        DS.Log.LogError($"Handling command {cmdType} received from client");

        var cmd = CommandFactory.Deserialize(cmdType, payload);

        if(!_playerRegistrar.TryGetRegisteredPlayerEntity(client.Peer, out var entity))
            return;

        if (!CommandHandlerFactory.Get(cmd.Type, _world, (Entity)entity).Execute(cmd)) {
            DS.Log.LogError($"Failed to execute server command {cmd.Type}. Informing server.");

            client.SendError((byte)ClientErrors.COMMAND_EXECUTION_FAILED, "Command could not be executed on client");
        }
    }

    protected void HandleClientStatusUpdate(DaedalusNetPeer client, byte code){
        var transition = (NetPlayerStateTransitions)code;

        DS.Log.LogError($"Handling transition update {transition} from client");

        if (!_playerRegistrar.TryGetRegisteredPlayerEntity(client.Peer, out var entity))
            return;

        switch (transition) {
            case NetPlayerStateTransitions.FINISHED_BOOTSTRAPPING:
                _world.Get<NetPlayerComponent>((Entity)entity).State = NetPlayerState.WAITING;
                
                break;
        }
    }

    protected void HandleClientError(DaedalusNetPeer client, byte code, string message){
        DS.Log.LogError($"Client error code: {(ClientErrors)code} message: {message}");
    }
}

public class SmartTiledMapGenerator {
    public bool Success { get; private set; } = false;
    public bool Fail { get; private set; } = false;
    public string Error { get; private set; }
    public TiledMapDungen Map { get; private set; }

    private int _retries;
    private string _contentDir;
    private FileSystem _fs;

    public SmartTiledMapGenerator (FileSystem fs, string contentDir, int retries = 0) {
        _retries = retries;
        _contentDir = contentDir;
        _fs = fs;
    }

    public async void GenerateAsync(string graph) {
        TiledMapDungenBuilder builder = new (new LocalDiskContentProvider(DS.LogFactory, _fs, _contentDir), DS.LogFactory);

        var res = await builder.BuildAsync(graph, 
            new TiledMapDungenBuilderProps() { EmptyTileGid = 30, DoorWidth = 2 });

        if (res.IsFailed) {
            DS.Log.LogError(res.Errors[0].Message);

            if (_retries > 0) {
                _retries--;

                GenerateAsync(graph);
            }
            else {
                Error = res.Errors[0].Message;
                Fail = true;

                return;
            }
        }

        Success = true;
        Map = res.Value;
    }
}

