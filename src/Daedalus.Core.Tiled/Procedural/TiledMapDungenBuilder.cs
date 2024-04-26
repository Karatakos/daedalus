namespace Daedalus.Core.Tiled.Procedural;

using Daedalus.Core.Tiled.Maps;
using Daedalus.Core.Tiled.Procedural.ContentProviders;
using Daedalus.Core.Tiled.Procedural.Extensions;
using Daedalus.Core.Tiled.Procedural.Errors;

using Dungen;
using FluentResults;

using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using System.Linq;

public record TiledMapDungenBuilderProps (
    int DoorWidth = 1,
    int DoorMinDistanceFromCorner = 1,
    uint EmptyTileGid = 0,
    uint TileWidth = 32,
    uint TileHeight = 32);

public class TiledMapDungenBuilder
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory; 
    public readonly IContentProvider TiledMapContentProvider;

    public TiledMapDungenBuilder(
        IContentProvider inputContentProvider, 
        ILoggerFactory loggerFactory) {

        _logger = loggerFactory.CreateLogger<TiledMapDungenBuilder>();
        _loggerFactory = loggerFactory;

        TiledMapContentProvider = inputContentProvider;
    }

    /* Generates a map from a given input graph. Runs asyncronously on the 
    *  threadpool since this operation is CPU bound. If graph changes call again.
    */
    public async Task<Result<TiledMapDungen>> BuildAsync(
        string graphLabel,
        TiledMapDungenBuilderProps props) {

        var mapData = await TiledMapContentProvider.LoadAsync(graphLabel);
        if (mapData.IsFailed)
            return Result.Fail(mapData.Errors);

        var dungenGenerator = new DungenGenerator(_loggerFactory);

        var dungenLayout = await dungenGenerator.GenerateAsync(mapData.Value, props);
        if (dungenLayout.IsFailed)
            return Result.Fail(dungenLayout.Errors);

        return BuildMap(
            dungenLayout.Value, 
            mapData.Value.GraphDependencies.RoomBlueprints, 
            mapData.Value.GraphDependencies.Templates, 
            mapData.Value.GraphDependencies.TileSets,
            props);
    }   

    private Result<TiledMapDungen> BuildMap(
        DungenLayout layout, 
        Dictionary<string, TiledMapGraphRoomBlueprintContent> blueprints,
        Dictionary<string, TiledMap> templates,
        Dictionary<string, TiledSet> tilesets,
        TiledMapDungenBuilderProps props) {

        uint tileWidth = props.TileWidth;
        uint tileHeight = props.TileHeight;

        // Start: This should be a Dungen responsibility, we want a layout back that is already snapped to grid

        float maxX = -100000;
        float maxY = -100000;
        float minX = 100000;
        float minY = 100000;

        foreach (Room room in layout.Rooms) {
            room.SnapToGrid();

            AABB2F rBoundingBox = room.GetBoundingBox();
            maxX = Math.Max(maxX, rBoundingBox.Max.x);
            maxY = Math.Max(maxY, rBoundingBox.Max.y);
            minX = Math.Min(minX, rBoundingBox.Min.x);
            minY = Math.Min(minY, rBoundingBox.Min.y);
        }

        var width = (uint)Math.Round(Math.Abs(maxX - minX), MidpointRounding.AwayFromZero);
        var height = (uint)Math.Round(Math.Abs(maxY - minY), MidpointRounding.AwayFromZero);

        var center = new Vector2F((maxX + minX) / 2, (maxY + minY) / 2);
        var bb = new AABB2F(new Vector2F(minX, minY), new Vector2F(maxX, maxY)); 

        // End

        var map = new TiledMapDungen(
            width,
            height,
            tileWidth,
            tileHeight);

        var mapCenterS = new Vector2F(width * tileWidth / 2, height * tileHeight / 2);
        var layoutCenterS = center * tileWidth;
        
        var templateMerger = new TiledMapMerger(_loggerFactory);
        var doorManager = new TiledMapDoorManager(_loggerFactory);

        foreach (Room room in layout.Rooms) {
            // Grab a random template that fits one of the room's blueprints
            //
            var template = GetRandomTiledMapForRoom(room, blueprints, templates);

            // Start by scaling the room by tile size since rooms are defined in tile units
            //
            room.Scale(tileWidth);
            room.Position = room.Position * tileWidth;  // BUG: Scale method should handle this

            // Move the room to map space using map center
            //
            room.Translate(mapCenterS - layoutCenterS);

            // We want to tile from the shapes top left but we also have to flip on y to get a position
            // that maps to the tiles right-down coordinate system
            //
            var roomRectangle = room.GetBoundingBox();
            var roomPositionTopLeft = new Vector2(roomRectangle.Min.x, roomRectangle.Max.y);
            var flippedRoomPosition = new Vector2(roomPositionTopLeft.X, (map.Height * map.TileHeight) - roomPositionTopLeft.Y);

            // Merge selected room template into map for a given room's position
            //
            var mergeRes = templateMerger.Merge(map, template, flippedRoomPosition, props.EmptyTileGid);
            if (mergeRes.IsFailed)
                return Result.Fail(mergeRes.Errors);

            // Installs a room's doors directly into our room template
            //
            var installDoorsRes = doorManager.InstallDoors(
                map, 
                tilesets,  
                room, 
                props.DoorMinDistanceFromCorner);
            if (installDoorsRes.IsFailed)
                return Result.Fail(installDoorsRes.Errors);

            // Keep track of tile indices p/room for easy tile->room lookup for map consumers
            //
            var tiledRoom = new TiledMapDungenRoom(room.Number, DungenToTiledDungenRoomType(room.Type));
            tiledRoom.AccessibleRooms.AddRange(room.Doors.Select(x => x.ConnectingRoom.Number));
            tiledRoom.TileIndices.AddRange(templateMerger.DirtyTileIndices);

            map.Rooms.Add(tiledRoom);
        }

        return map;
    }

    private TiledMap GetRandomTiledMapForRoom(Room room, Dictionary<string, TiledMapGraphRoomBlueprintContent> blueprints, Dictionary<string, TiledMap> templates) {
        // TODO: We need a better equality mechanism between the two types, ID?
        //
        var blueprint = GetBlueprintMatchingShape(blueprints.Values.ToList(), room.Blueprint.Points);
        
        if (blueprint.CompatibleTilemaps.Count == 0)
            throw new Exception("No compatible tilemaps found for blueprint!");

        var rnd = new Random();
        var rndMap = rnd.Next(0, blueprint.CompatibleTilemaps.Count-1);

        // TODO: Clone
        //
        return templates[blueprint.CompatibleTilemaps[rndMap]];
    }

    private TiledMapGraphRoomBlueprintContent GetBlueprintMatchingShape(List<TiledMapGraphRoomBlueprintContent> blueprints, List<Dungen.Vector2F> points) {
            foreach(TiledMapGraphRoomBlueprintContent blueprint in blueprints) {
                bool match = true;
                for (int i=0; i<points.Count; i++) {
                    if (points[i].x != blueprint.Points[i][0] || points[i].y != blueprint.Points[i][1]) {
                        match = false;
                        break;
                    }
                }

                if (match) 
                    return blueprint;
            }

            return null;
    }

    private TiledMapDungenRoomType DungenToTiledDungenRoomType(RoomType type) {
        switch (type) {
            case RoomType.Entrance: 
                return TiledMapDungenRoomType.Entrance;
            
            case RoomType.Exit: 
                return TiledMapDungenRoomType.Exit;

            case RoomType.Arena: 
                return TiledMapDungenRoomType.Arena;

            case RoomType.Corridor: 
                return TiledMapDungenRoomType.Corridor;

            default: 
                return TiledMapDungenRoomType.Normal;
        }
    }
}





