namespace Daedalus.Core.Tiled.Tests;

using Daedalus.Core.Tiled.Maps;
using Daedalus.Core.Tiled.Procedural;

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Logging;

using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using FluentAssertions;

using System.Text.Json;
using System.Text.Json.Serialization;
using Dungen;

public class TileMapDoorManagerTests
{      
    private ILoggerFactory _loggerFactory;
    private TiledMap _map;
    private Dictionary<string, TiledSet> _tilesets;

    [SetUp]
    public void Setup() {
        

        
        
        _loggerFactory = LoggerFactory.Create((builder) => builder.AddSimpleConsole());
    }

    [Test]
    public void DoorInstallForRoom() {
        var opts = new JsonSerializerOptions();

        opts.Converters.Add(new JsonStringEnumConverter());
        opts.PropertyNameCaseInsensitive = true;

        // 5x5 tiled room @ 32 px. p/ tile
        //
        var room = new Room(
            new RoomBlueprint([
                new Vector2F(0, 0),
                new Vector2F(0, 64),     // Door Line A (index 1)
                new Vector2F(0, 96),     // 
                new Vector2F(0, 160),
                new Vector2F(64, 160),   // Door Line B (index 4)
                new Vector2F(96, 160),   //
                new Vector2F(160, 160),
                new Vector2F(160, 0)
            ]), RoomType.Normal, 1);

        room.Boundary[1] = new BoundaryLine(room.Boundary[1].Start, room.Boundary[1].End, true, true);
        room.Boundary[4] = new BoundaryLine(room.Boundary[4].Start, room.Boundary[4].End, true, true);

        room.Doors.Add(new Door((room.Boundary[1].Start, room.Boundary[1].End), null));
        room.Doors.Add(new Door((room.Boundary[4].Start, room.Boundary[4].End), null));

        // 5x5 tile map 
        //
        var map = JsonSerializer.Deserialize<TiledMap>(Content.TileMapTemplate, opts);
    
        var tilesets = new Dictionary<string, TiledSet>() {
            { 
                "s-sq-desert-1.tileset.json", 
                JsonSerializer.Deserialize<TiledSet>(Content.TileSetTemplate, opts) 
            }
        };

        var doorMgr = new TiledMapDoorManager(_loggerFactory);

        doorMgr.InstallDoors(map, tilesets.Values.ToList(), room, 1).Should().BeSuccess();

        map.Layers[0].Data[10].Should().Be(2147483652); // Tileset tile 4 (West)
        map.Layers[0].Data[2].Should().Be(35);          // Tileset tile 35 (North)

        //File.WriteAllText("map.tilemap.json", JsonSerializer.Serialize<TiledMap>(map, opts));
    }

    [TearDown]
    public void TearDown() {
        _loggerFactory.Dispose();
    }
}