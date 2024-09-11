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
using GraphToGrid;

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
    public void BullshitTest_CheckingRoundingIssueWhenScalingForMultipleRooms() {
        var opts = new JsonSerializerOptions();

        opts.Converters.Add(new JsonStringEnumConverter());
        opts.PropertyNameCaseInsensitive = true;

        // 5x5 tiled room @ 32 px. p/ tile
        //
        var roomA = new Room(
            new RoomBlueprint([
                new Vector2F(-5, -5),
                new Vector2F(-5, 5),     
                new Vector2F(0.9f, 5),     // Door Line A (index 1)
                new Vector2F(1.9f, 5),     //
                new Vector2F(5, 5),  
                new Vector2F(5, -5)
            ]), 1);

        // Mark/add door line
        //
        roomA.Boundary[1] = new BoundaryLine(roomA.Boundary[1].Start, roomA.Boundary[1].End, true, true);
        roomA.Doors.Add(new Door(new Line(roomA.Boundary[1]), 1));

        var roomB = new Room(
            new RoomBlueprint([
                new Vector2F(1.9f, 5),      // Door Line A (index 0)
                new Vector2F(0.9f, 5),      //
                new Vector2F(-2.2f, 5),     
                new Vector2F(-2.2f, 15),   
                new Vector2F(7.8f, 15),  
                new Vector2F(7.8f, 5)
            ]), 2);

        roomB.Boundary[0] = new BoundaryLine(roomB.Boundary[0].Start, roomB.Boundary[0].End, true, true);
        roomB.Doors.Add(new Door(new Line(roomB.Boundary[0]), 2));

        // Pretend we already have a map and a layout based on the above room dimensions
        //
        var mapCenter = new Vector2F(12.8f / 2, 20 / 2);
        var layoutCenter = new Vector2F(1.4f, 5);

        var mapCenterS = mapCenter * 32;
        var layoutCenterS = layoutCenter * 32;

        roomA.Scale(32);
        roomA.Position = roomA.Position * 32; 
        roomA.Translate(mapCenterS - layoutCenterS);

        roomB.Scale(32);
        roomB.Position = roomA.Position * 32; 
        roomB.Translate(mapCenterS - layoutCenterS);

        var rAtmpAABB = roomA.GetBoundingBox();
        var rBtmpAABB = roomB.GetBoundingBox();

        // Basic test to ensure scaling math is correct 
        //
        rAtmpAABB.Min.x.Should().BeGreaterThanOrEqualTo(0);
        rAtmpAABB.Min.y.Should().BeGreaterThanOrEqualTo(0);

        rBtmpAABB.Min.x.Should().BeGreaterThanOrEqualTo(0);
        rBtmpAABB.Min.y.Should().BeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void DoorInstallForRoomWithNorthWall() {
        var opts = new JsonSerializerOptions();

        opts.Converters.Add(new JsonStringEnumConverter());
        opts.PropertyNameCaseInsensitive = true;

        // 5x5 tiled room @ 32 px. p/ tile
        //
        var roomA = new Room(
            new RoomBlueprint([
                new Vector2F(-5, -5),
                new Vector2F(-5, 5),     
                new Vector2F(0.9f, 5),     // Door Line A (index 2); Wall-North; Line-East
                new Vector2F(1.9f, 5),     //
                new Vector2F(5, 5),  
                new Vector2F(5, -5)
            ]), 1);

        // Mark/add door line
        //
        roomA.Boundary[2] = new BoundaryLine(roomA.Boundary[2].Start, roomA.Boundary[2].End, true, true);
        roomA.Doors.Add(new Door(new Line(roomA.Boundary[2]), 1));
        
        roomA.Scale(32);
        roomA.Translate(new Vector2F(5 * 32, 5 * 32) - roomA.GetCenter());

        // 10x10 tile map room template
        //
        var map = JsonSerializer.Deserialize<TiledMap>(Content.TileMapTemplate10x10, opts);
    
        var tilesets = new Dictionary<string, TiledSet>() {
            { 
                "s-sq-desert-1.tileset.json", 
                JsonSerializer.Deserialize<TiledSet>(Content.TileSetTemplate, opts) 
            }
        };

        var doorMgr = new TiledMapDoorManager(_loggerFactory);

        doorMgr.InstallDoors(map, tilesets, roomA, 1).Should().BeSuccess();

        map.Layers[0].Data[5].Should().Be(36);          // Tileset tile 35 (North) + 1 (first GID) 

        //File.WriteAllText("map.tilemap.json", JsonSerializer.Serialize<TiledMap>(map, opts));
    }

    [Test]
    public void DoorInstallForRoomWithSouthWall() {
        var opts = new JsonSerializerOptions();

        opts.Converters.Add(new JsonStringEnumConverter());
        opts.PropertyNameCaseInsensitive = true;

        // 5x5 tiled room @ 32 px. p/ tile
        //
        var roomA = new Room(
            new RoomBlueprint([
                new Vector2F(-5, -5),
                new Vector2F(-5, 5),
                new Vector2F(5, 5),  
                new Vector2F(5, -5),
                new Vector2F(1.9f, -5),   // Door Line A (index 2); Wall-South; Line-West
                new Vector2F(0.9f, -5)    //
            ]), 1);

        // Mark/add door line
        //
        roomA.Boundary[4] = new BoundaryLine(roomA.Boundary[4].Start, roomA.Boundary[4].End, true, true);
        roomA.Doors.Add(new Door(new Line(roomA.Boundary[4]), 1));
        
        roomA.Scale(32);
        roomA.Translate(new Vector2F(5 * 32, 5 * 32) - roomA.GetCenter());

        // 10x10 tile map room template
        //
        var map = JsonSerializer.Deserialize<TiledMap>(Content.TileMapTemplate10x10, opts);
    
        var tilesets = new Dictionary<string, TiledSet>() {
            { 
                "s-sq-desert-1.tileset.json", 
                JsonSerializer.Deserialize<TiledSet>(Content.TileSetTemplate, opts) 
            }
        };

        var doorMgr = new TiledMapDoorManager(_loggerFactory);

        doorMgr.InstallDoors(map, tilesets, roomA, 1).Should().BeSuccess();

        map.Layers[0].Data[95].Should().Be(2684354565);   // Tileset tile 35v (South) + 1 (first GID)

        //File.WriteAllText("map.tilemap.json", JsonSerializer.Serialize<TiledMap>(map, opts));
    }

    [TearDown]
    public void TearDown() {
        _loggerFactory.Dispose();
    }
}