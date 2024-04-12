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
    int EmptyTileGid = 0,
    int TileWidth = 32,
    int TileHeight = 32);

public class TiledMapDungenBuilder
{
    private HashSet<int> _dirtyTileIndexes;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory; 
    public readonly IContentProvider TiledMapContentProvider;

    public TiledMapDungenBuilder(
        IContentProvider inputContentProvider, 
        ILoggerFactory loggerFactory) {

        _logger = loggerFactory.CreateLogger<TiledMapDungenBuilder>();
        _loggerFactory = loggerFactory;
        _dirtyTileIndexes = new HashSet<int>();

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

        return BuildMap(dungenLayout.Value, mapData.Value.RoomBlueprints, mapData.Value.Templates, props);
    }   

    private Result<TiledMapDungen> BuildMap(
        DungenLayout layout, 
        Dictionary<string, TiledMapGraphRoomBlueprintContent> blueprints,
        Dictionary<string, TiledMap> templates,
        TiledMapDungenBuilderProps props) {

        int tileWidth = props.TileWidth;
        int tileHeight = props.TileHeight;

        var map = new TiledMapDungen(
            ConvertToInt(layout.Width),
            ConvertToInt(layout.Height),
            tileWidth,
            tileHeight);

        var worldCenter = new Vector2F(layout.Width * tileWidth / 2, layout.Height * tileHeight / 2);
        var layoutCenterScaled = layout.Center * tileWidth;

        TiledMapMerger merger = new TiledMapMerger(_loggerFactory);

        foreach (Room r in layout.Rooms) {
            Room room = TransformRoomToWorld(r, tileWidth, worldCenter, layoutCenterScaled, out Vector2 originPos);

            // TODO: We need a better equality mechanism between the two types, ID?
            //
            var blueprint = GetBlueprintMatchingShape(blueprints.Values.ToList(), room.Blueprint.Points);

            // Pseudo random template selection for now
            //
            var template = GetRandomTiledMapForBlueprint(blueprint, templates);
            
            // Returns same map instance mutated with the template data.
            //
            var res = merger.Merge(map, template, originPos, props.EmptyTileGid);
            if (res.IsFailed)
                return Result.Fail(res.Errors);

            var tiledRoom = new TiledMapDungenRoom(room.Number, DungenToTiledDungenRoomType(room.Type));

            tiledRoom.AccessibleRooms.AddRange(room.Doors.Select(x => x.ConnectingRoom.Number));
            tiledRoom.TileIndices.AddRange(merger.DirtyTileIndices);

            map.Rooms.Add(tiledRoom);
        }

        return map;
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

    private Room TransformRoomToWorld(Room room, int tileWidth, Vector2F worldCenter, Vector2F layoutCenterScaled, out Vector2 worldPosition) {
        // Transform from map to world by first scaling by tile size then moving to world center

            // 1. Start by scaling the room & position
            //
            room.Scale(tileWidth);
            room.Position = room.Position * tileWidth;  // Bug: Scale method should have updated the position

            // 2. Now transform the room to world space by moving to the world center point
            //
            var translateToWorld = worldCenter - layoutCenterScaled;
            room.Translate(translateToWorld);

            // We want to tile from the shapes top left as per a tile maps origin point
            //
            var aabb = room.GetBoundingBox();
            worldPosition = new Vector2F(aabb.Min.x, aabb.Min.y).ToVector2();

            return room;
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

    private TiledMap GetRandomTiledMapForBlueprint(TiledMapGraphRoomBlueprintContent blueprint, Dictionary<string, TiledMap> templates) {
        if (blueprint.CompatibleTilemaps.Count == 0)
            throw new Exception("No compatible tilemaps found for blueprint!");

        var rnd = new Random();
        var rndMap = rnd.Next(0, blueprint.CompatibleTilemaps.Count-1);

        return templates[blueprint.CompatibleTilemaps[rndMap]];
    }

    private int ConvertToInt(float value) {
        // Unsure how we want to handle this yet.
        // e.g. 10.8 returns 10, 11, or context dependent. Round then cast always gives us 11.
        //
        return (int)(value + 0.5f);
    }
}





