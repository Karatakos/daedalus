namespace Daedalus.Core.Tiled.Procedural;

using Daedalus.Core.Tiled.Maps;
using Daedalus.Core.Tiled.Procedural.Errors;
using Daedalus.Core.Tiled.Procedural.Extensions;
using Dungen;
using FluentResults;
using GraphPlanarityTesting.Graphs.Algorithms;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;

using System.Linq;

internal enum CardinalDirection {
    North,
    South,
    East,
    West,
    Other
}

internal class TiledMapDoor {
    internal uint Lid { get; private set; }
    internal uint Gid { get; private set; }
    internal bool IsFirstTile { get; private set; }

    internal TiledMapDoor(uint lid, uint gid, bool isFirstTile = false) {
        Lid = lid;
        Gid = gid;
        IsFirstTile = isFirstTile;
    }
}

internal class TiledMapDoorManager {
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory; 
    private Dictionary<CardinalDirection, List<TiledMapDoor>> _doorTiles;
    private TiledSet _doorTileSet;

    internal TiledMapDoorManager(
        ILoggerFactory loggerFactory) {

        _logger = loggerFactory.CreateLogger<TiledMapDoorManager>();
        _loggerFactory = loggerFactory;

        _doorTiles = new Dictionary<CardinalDirection, List<TiledMapDoor>>();
    }

    internal Result InstallDoors(
        TiledMap map, 
        List<TiledSet> tileSets,
        Room room,
        int minTileDistanceToCorner) {

        if (_doorTiles.Count == 0)
            CacheDoorTilesForEachCardinalDirection(tileSets);

        if (minTileDistanceToCorner < 1)
            return Result.Fail(
                new TiledMapDoorInstallerValidationError(
                    "Door installation parameter validation failed. Door must be at least 1 tile away from the edge of a room."));

        foreach (BoundaryLine line in room.Boundary) {
            if (!line.IsDoor)
                continue;

            var res = InstallDoor(map, line, room.GetCenter());
            if (res.IsFailed)
                return Result.Fail(res.Errors);
        }

        return Result.Ok();
    }

    private Result InstallDoor(
        TiledMap map, 
        BoundaryLine doorLine,
        Vector2F roomCenter) {

        if (_doorTiles.Count == 0)
            return Result.Fail(new TiledMapDoorInstallerValidationError(
                "No doors found in tile sets referenced by this map"));

        var dir = GetDoorDirectionRelativeToRoomCenter(doorLine, roomCenter);
        if(dir == CardinalDirection.Other)
            return Result.Fail(new TiledMapDoorInstallerValidationError(
                "Door location not supported. Doors must be on parallel lines at 0, 90, 180, 270 degs"));

        var doorTiles = _doorTiles[dir].ToArray();

        // TODO: If we trust Dungen then pass it in from props so we avoid this math.
        //       Otherwise, width or height??
        //
        var doorLengthInTiles = Utility.ConvertToUInt(
                Vector2F.Magnitude(doorLine.GetDirection())) / map.TileWidth;

        // Algorithm
        // 
        // Validate there are at least enough tiles to cover door line length 
        // Given a point along the door line:
        //      Get world tile index for point
        //          Get corresponding door tile from array 
        //          Populate gid at index
        //      Based on direction calculate depth - 1 for tile index
        //          Get corresponding door tile from array 
        //          Populate gid at index

        if (doorLengthInTiles > doorTiles.Length)
            return Result.Fail(new TiledMapDoorInstallerValidationError(
                "Door marker length in tiles is longer than the door tiles in the map's tile set"));

        // Relying on first door tile to be marked up via property in the tile set
        //
        // TODO: Is there a way of calculating this?
        //
        uint sourceTileLid = doorTiles.Where(t => t.IsFirstTile).Select(t => t.Lid).First();  

        // Find a starting coordinate for our door. We only work with coords here, after which we can
        // infer next tile  by direction.  
        //
        // Algo
        //  
        //  1. Flip the y coordinate. Tile space is right-down where as Dungen layout space is right-up
        //  2. South & East wall coord adjustment: x-32, y-32
        //  3. West wall coord adjustment: x, y-32
        //
        var flippedDoorVec = new Vector2F(doorLine.Start.x, (map.Height * map.TileHeight) - doorLine.Start.y);
        switch (dir) {
            case CardinalDirection.South:
            case CardinalDirection.East:
                flippedDoorVec = new Vector2F(Math.Max(flippedDoorVec.x - 32, 0), Math.Max(flippedDoorVec.y - 32, 0));
                break;

            case CardinalDirection.West:
                flippedDoorVec = new Vector2F(flippedDoorVec.x, Math.Max(flippedDoorVec.y - 32, 0));
                break;
        }

        // Grab the world map starting tile from the start of our line
        // 
        uint targetTileIndex = map.GetTileIndexContainingWorldSpacePosition(flippedDoorVec.ToVector2());

        uint sourceTilesetWidth = _doorTileSet.Columns;
        uint sourceTilesetHeight = _doorTileSet.TileCount / _doorTileSet.Columns;

        for (int i=0; i<doorLengthInTiles; i++) {
            UpdateMapTileIndexRecursively(
                sourceTileLid, 
                doorTiles, 
                sourceTilesetWidth, 
                sourceTilesetHeight, 
                dir, 
                targetTileIndex, 
                map);
            
            // Calculate next source and destination tile indexes 
            //
            switch (dir) {
                case CardinalDirection.North:
                    sourceTileLid ++;
                    targetTileIndex ++;             // Door line dir: right
                    break;

                case CardinalDirection.South:
                    sourceTileLid ++;
                    targetTileIndex --;             // Door line dir: left
                    break;

                case CardinalDirection.East:
                    sourceTileLid += sourceTilesetWidth;
                    targetTileIndex += map.Width;   // Door line dir: down
                    break;

                case CardinalDirection.West:
                    sourceTileLid += sourceTilesetWidth;
                    targetTileIndex -= map.Width;   // Door line dir: up
                    break;
            }
        }

        return Result.Ok();
    }

    private void UpdateMapTileIndexRecursively(
        uint sourceTile, 
        TiledMapDoor[] sourceTiles,
        uint sourceTilesetHeight, 
        uint sourceTilesetWidth,
        CardinalDirection dir,
        uint targetTile, 
        TiledMap map) {

        var doorTile = sourceTiles.FirstOrDefault(t => t.Lid == sourceTile, null);

        // Important: Implication of this is that if there are deeper tiles
        //            they will also be ignored, and so we expect the tiles
        //            to be drawn in such a way that front tile count >= back tiles.
        //            
        // There should be no need to solve for the above. Perhaps in cases where we have 
        // door tiles at the back that are for display only, or doors that get narrower 
        // as you move to the outer wall. 
        //
        if (doorTile == null) 
            return;

        // For simplicity we're just going to draw doors in the first tile layer
        //
        var layer = map.Layers.First(l => l.Type == TiledMapLayerType.tilelayer);

        // Write the door tile's gid to our world map
        //
        layer.Data[targetTile] = doorTile.Gid;

        var newSourceTile = sourceTile;
        var newTargetTile = targetTile;

        // Calculate source and destination tile indexes at depth-1, i.e. door tiles behind this tile
        //
        // TODO: Check values for uint if we go into negtive. Should only happen if silly doors
        //       cover the map end to end, e.g. mistake, but we should test for it.
        //
        switch (dir) {
            case CardinalDirection.North:
                sourceTile += sourceTilesetWidth;
                targetTile += map.Width;
                break;

            case CardinalDirection.South:
                sourceTile -= sourceTilesetWidth;
                targetTile -= map.Width;
                break;

            case CardinalDirection.East:
                sourceTile --;
                targetTile --;
                break;

            case CardinalDirection.West:
                sourceTile ++;
                targetTile ++;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(CardinalDirection), 
                    "Something went wrong extracting door tiles all four directions. Check the tile set.");
        }

        // Try update map for depth-1 door tile!
        //
        UpdateMapTileIndexRecursively(
            sourceTile, 
            sourceTiles, 
            sourceTilesetWidth, 
            sourceTilesetHeight, 
            dir, 
            targetTile, 
            map);
    }

    private Result CacheDoorTilesForEachCardinalDirection(
        List<TiledSet> tilesets) {

        foreach (TiledSet ts in tilesets) {
            foreach (TiledSetTile t in ts.Tiles) {
                bool isFirstTile = false;
                bool isDoor = false;
                CardinalDirection dir = CardinalDirection.Other;

                foreach (TiledProperty p in t.Properties) {
                    if (p.Name == "Type")
                        isDoor = p.Value == "Door";

                    if (p.Name == "IsFirstDoor")
                        isFirstTile = p.Value == "true";

                    if (p.Name == "Direction") {
                        if (!Enum.TryParse<CardinalDirection>(p.Value, out dir))
                            return Result.Fail(new TiledMapDoorInstallerValidationError(
                                "Incorrect value for Direction property. Should be North, South, East or West"));
                    }
                }

                if (!isDoor)
                    continue;

                if (!_doorTiles.ContainsKey(dir))     
                    _doorTiles.Add(dir, new List<TiledMapDoor>());

                // Gid is just the local tile index since there is no transform to apply
                //
                _doorTiles[dir].Add(new TiledMapDoor(t.Id, t.Id, isFirstTile));

                // Generate a door for the opposite direction.
                //
                // Context: We only need daws drawn for two directions on the tile sheet since we can
                // flip the tiles to get the opposite direction gids automatically
                //
                switch (dir) {
                    case CardinalDirection.North: 
                        if (!_doorTiles.ContainsKey(CardinalDirection.South))     
                            _doorTiles.Add(CardinalDirection.South, new List<TiledMapDoor>());

                        _doorTiles[CardinalDirection.South].Add(
                            new TiledMapDoor(t.Id, t.Id | Constants.FLIPPED_VERTICALLY_FLAG, isFirstTile));
                        break;

                    case CardinalDirection.South: 
                        if (!_doorTiles.ContainsKey(CardinalDirection.North))     
                            _doorTiles.Add(CardinalDirection.North, new List<TiledMapDoor>());

                        _doorTiles[CardinalDirection.North].Add(
                            new TiledMapDoor(t.Id, t.Id | Constants.FLIPPED_VERTICALLY_FLAG, isFirstTile));
                        break;

                    case CardinalDirection.East: 
                        if (!_doorTiles.ContainsKey(CardinalDirection.West))     
                            _doorTiles.Add(CardinalDirection.West, new List<TiledMapDoor>());

                        _doorTiles[CardinalDirection.West].Add(
                            new TiledMapDoor(t.Id, t.Id | Constants.FLIPPED_HORIZONTALLY_FLAG, isFirstTile));
                        break;

                    case CardinalDirection.West: 
                        if (!_doorTiles.ContainsKey(CardinalDirection.East))     
                            _doorTiles.Add(CardinalDirection.East, new List<TiledMapDoor>());

                        _doorTiles[CardinalDirection.East].Add(
                            new TiledMapDoor(t.Id, t.Id | Constants.FLIPPED_HORIZONTALLY_FLAG, isFirstTile));
                        break;

                    default:
                        return Result.Fail(new TiledMapDoorInstallerValidationError(
                            "Ensure Direction property is set for each door tile"));
                }
            }

            // Break if we've processed doors from this tileset. We're done.
            // 
            // Context: We only support doors on a single tile set. Fine uunless we start supporting 
            //          multiple tilesets per map, e.g. rooms painted via differing tilesets but that 
            //          would require a bigger overhaul incl. significant work in the tilemap merger.
            //
            if (_doorTiles.Count > 0) { 
                // Make sure we do have all doors.
                //
                if (_doorTiles.Count != 4)
                    return Result.Fail(new TiledMapDoorInstallerValidationError(
                            "Please ensure doors for North OR South AND East OR West are including in the same tile set."));
                
                _doorTileSet = ts;
                break;
            }
        }

        return Result.Ok();
    }

    private CardinalDirection GetDoorDirectionRelativeToRoomCenter(BoundaryLine door, Vector2F relativeToVec) {
        var dirVecNormalized = Vector2F.Normalize(door.GetDirection());

        CardinalDirection dir = CardinalDirection.Other;
        // East or West wall since the line points north or south
        //
        if (Math.Abs(dirVecNormalized.x) == 0 && Math.Abs(dirVecNormalized.y) == 1) 
            if (door.Start.x < relativeToVec.x)
                dir = CardinalDirection.West;
            else
                dir = CardinalDirection.East;
            
        // North or south wall since the line points east or west
        //
        else if (Math.Abs(dirVecNormalized.x) == 1 && Math.Abs(dirVecNormalized.y) == 0)
            if (door.Start.y < relativeToVec.y)
                dir = CardinalDirection.South;
            else
                dir = CardinalDirection.North;

            return dir;
    }
}

