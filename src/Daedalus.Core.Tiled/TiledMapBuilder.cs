namespace Daedalus.Tiled;

using Daedalus.Tiled.ContentProviders;

using Dungen;
using FluentResults;

using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;

public record TiledMapBuilderProps (
    int DoorWidth = 1,
    int DoorMinDistanceFromCorner = 1);

public class TiledMapBuilder
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    public readonly IContentProvider TiledDungenDataProvider;

    public TiledMapBuilder(
        IContentProvider inputGraphProvider, 
        ILoggerFactory loggerFactory) {

        _logger = loggerFactory.CreateLogger<TiledMapBuilder>();
        _loggerFactory = loggerFactory;

        TiledDungenDataProvider = inputGraphProvider;
    }

    /* Generates a map from a given input graph. Runs asyncronously on the 
    *  threadpool since this operation is CPU bound. If graph changes call again.
    */
    public async Task<Result<TiledMap>> BuildAsync(
        string graphLabel,
        TiledMapBuilderProps props) {

        var mapData = await TiledDungenDataProvider.LoadAsync(graphLabel);
        if (mapData.IsFailed)
            return Result.Fail(mapData.Errors);

        // Ideally we only interact with a set of interfaces here such as ILayoutAdapter
        // which gets passed in as a dependency. For now, thats premature optimization.
        // 
        // This particular adapter still makes sense since it's helful to abstract away the 
        // responsibility for actually running Dungen and generating a layout.
        //
        var layoutAdapter = new DungenAdapter(_loggerFactory);

        var dungenLayout = await layoutAdapter.GenerateAsync(mapData.Value, props);
        if (dungenLayout.IsFailed)
            return Result.Fail(dungenLayout.Errors);

        return BuildMap(dungenLayout.Value, mapData.Value.RoomBlueprints, mapData.Value.Templates);
    }   

    private Result<TiledMap> BuildMap(
        DungenLayout layout, 
        Dictionary<string, TiledMapGraphRoomBlueprintContent> blueprints,
        Dictionary<string, TiledMap> templates) {

        int tileWidth = 32, tileHeight = 32;

        TiledMap map = new TiledMap(
            "orthogonal",
            "right-down",
            ConvertToInt(layout.Width),
            ConvertToInt(layout.Height),
            tileWidth,
            tileHeight,
            new List<TiledMapLayer>(),
            new List<TiledSet>());

        // Algo: 
        //
        //  Enumerate layout's rooms
        //  Find compatible tile map template
        //  Enumerate tiles for each tile layer of the template
        //  Get local position for tile index as well as the tile value
        //  Translate the position according to the room's position
        //  Get tile index in map for new position and store the tile value at this index in composite map
        //

        // New composite map based on the layout size. Assuming 0 represents empty tile.
        //
        var dataSize = map.Width * map.Height;
        var data = new int[dataSize];

        // TODO: Some tileset property for the tile we actually want
        //
        Array.Fill<int>(data, 30);

        map.Layers.Add(new TiledMapLayer(
            1,
            $"Tile map layer {1}",
            data,
            map.Width,
            map.Height));

        var worldCenter = new Vector2F(layout.Width * tileWidth / 2, layout.Height * tileHeight / 2);
        var layoutCenterScaled = layout.Center * tileWidth;

        foreach (Room room in layout.Rooms) {
            // Transform from map to world by first scaling by tile size then moving to world center

            // 1. Start by scaling the room & position
            //
            room.Scale(tileWidth);
            room.Position = room.Position * tileWidth;  // Bug: Scale method should have updated the position

            // 2. Now transform the room to world space by moving to the world center point
            //
            var translateToWorld = worldCenter - layoutCenterScaled;
            var newPos = room.Position + translateToWorld;
            room.Translate(translateToWorld);

            // We want to tile from the shapes top left as per a tile maps origin point
            //
            var aabb = room.GetBoundingBox();
            var originPos = new Vector2F(aabb.Min.x, aabb.Min.y).ToVector2();

            // TODO: This is really nasty we need a real eqality mechanism between the two types, ID??
            //
            var blueprint = GetBlueprintMatchingShape(blueprints.Values.ToList(), room.Blueprint.Points);

            var template = GetRandomTiledMapForBlueprint(blueprint, templates);

            // TODO: Handle layers
            //
            for (int i=0; i<1; i++) {
                var templateLayer = template.Layers[i];

                for (int j=0; j<templateLayer.Data.Length; j++) {
                    var localpos = template.GetWorldSpacePositionForTileIndex(j);

                    var worldpos = originPos + localpos;

                    int worldTileIndex = map.GetTileIndexForWorldSpacePosition(worldpos);

                    data[worldTileIndex] = templateLayer.Data[j];
                }   
            }

            // All map templates must use the same tile sets (in the same order).
            //
            if (map.TileSets.Count == 0)
                foreach (TiledSet tileset in template.TileSets)
                    map.TileSets.Add(tileset);
        }

        return map;
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





