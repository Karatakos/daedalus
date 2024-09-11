namespace Daedalus.Core.Tiled.Procedural;

using Daedalus.Core.Tiled.Maps;
using Daedalus.Core.Tiled.Procedural.Errors;
using Daedalus.Core.Tiled.Procedural.Extensions;
using GraphToGrid;
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

internal enum StartingTileDrawOrder {
    First,
    Last
}

internal class TiledMapDoor {
    internal uint Lid { get; private set; }
    internal uint Gid { get; private set; }
    internal bool IsFirstTile { get; private set; }
    internal bool IsLastTile { get; private set; }

    internal TiledMapDoor(uint lid, uint gid, bool isFirstTile = false, bool isLastTile = false) {
        Lid = lid;
        Gid = gid;
        IsFirstTile = isFirstTile;
        IsLastTile = isLastTile;
    }
}

internal class DoorMarker {
    internal Vector2 StartingPoint { get; set; }
    internal CardinalDirection Direction { get; set; }
    internal CardinalDirection WallDirection { get; set; }
    internal uint Length { get; set; }

    private DoorMarker(Vector2 startingPoint, CardinalDirection direction, CardinalDirection wallDirection, uint length) {
        StartingPoint = startingPoint;
        Direction = direction;
        WallDirection = wallDirection;
        Length = length;
    }

    internal Vector2 GetStartingPointWithOffsetForTileGrid(uint tileWidth) {
        var offset = new Vector2(0, 0);

        switch (Direction) {
            case CardinalDirection.North:
                offset.Y += tileWidth;
                break;

            case CardinalDirection.West:
                offset.X -= tileWidth;
                break;
        }

        switch (WallDirection) {
            case CardinalDirection.South:
                offset.Y += tileWidth;
                break;

            case CardinalDirection.East:
                offset.X -= tileWidth;
                break;
        }

        return new Vector2(Math.Max(StartingPoint.X + offset.X, 0), Math.Max(StartingPoint.Y + offset.Y, 0));
    }

    internal static Result<DoorMarker> FromLine(Line door, Vector2F relativeToVec, uint tileWidth) {
        Vector2 dirVecNormalized = Vector2F.Normalize(door.End - door.Start).ToVector2();
        Vector2 startingPoint = door.Start.ToVector2();
        Vector2 relativeToVec2 = relativeToVec.ToVector2();

        var direction = CardinalDirection.Other;
        var roomWall = CardinalDirection.Other;

        if (dirVecNormalized.X == 0 & dirVecNormalized.Y == 1)
            direction = CardinalDirection.North;

        if (dirVecNormalized.X == 0 & dirVecNormalized.Y == -1)
            direction = CardinalDirection.South;

        if (dirVecNormalized.X == 1 & dirVecNormalized.Y == 0)
            direction = CardinalDirection.East;

        if (dirVecNormalized.X == -1 & dirVecNormalized.Y == 0)
            direction = CardinalDirection.West;

        if(direction == CardinalDirection.Other)
            return Result.Fail(new TiledMapDoorInstallerValidationError(
                "Doors must be at 0, 90, 180, 270 degs"));

        // Figure out which side of the room the wall is on
        // 
        switch (direction) {
            // East or West since the line points north or south
            //
            case CardinalDirection.North:
            case CardinalDirection.South: 
                if (startingPoint.X > relativeToVec2.X)
                    roomWall = CardinalDirection.East;
                else
                    roomWall = CardinalDirection.West;
                break;

            // North or south since the line points east or west
            //
            case CardinalDirection.East:
            case CardinalDirection.West: 
                if (startingPoint.Y > relativeToVec2.Y)
                    roomWall = CardinalDirection.North;
                else
                    roomWall = CardinalDirection.South;
                break;
        }

        if(direction == CardinalDirection.Other)
            return Result.Fail(new TiledMapDoorInstallerValidationError(
                "Could not figure out which side of the room your door is on please check door lines via Dungen logs"));

        // TODO: If we trust Dungen then pass it in from props so we avoid this math.
        //       Otherwise, width or height??
        //
        var tileLength = (uint)Math.Round(
            Vector2F.Magnitude(door.GetDirection())) / tileWidth;

        return new DoorMarker(startingPoint, direction, roomWall, tileLength);
    }
}

internal class TiledMapDoorManager {
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory; 
    private Dictionary<CardinalDirection, List<TiledMapDoor>> _doorTiles;
    private TiledSet _doorTileSet;
    private Dictionary<CardinalDirection, CardinalDirection> _doorTileTransformMap;

    internal TiledMapDoorManager(
        ILoggerFactory loggerFactory) {

        _logger = loggerFactory.CreateLogger<TiledMapDoorManager>();
        _loggerFactory = loggerFactory;

        _doorTiles = new Dictionary<CardinalDirection, List<TiledMapDoor>>();
        _doorTileTransformMap = new Dictionary<CardinalDirection, CardinalDirection>();
    }

    internal Result InstallDoors(
        TiledMap map, 
        Dictionary<string, TiledSet> tileSets,
        Room room,
        int minTileDistanceToCorner) {

        if (_doorTiles.Count == 0)
            CacheDoorTilesForEachCardinalDirection(map, tileSets);

        if (minTileDistanceToCorner < 1)
            return Result.Fail(
                new TiledMapDoorInstallerValidationError(
                    "Door installation parameter validation failed. Door must be at least 1 tile away from the edge of a room."));

        foreach (Door door in room.Doors) {
            var marker = DoorMarker.FromLine(door.Marker, room.GetCenter(), map.TileWidth);
            if (marker.IsFailed)
                return Result.Fail(marker.Errors);
            
            var res = InstallDoor(map, marker.Value);
            if (res.IsFailed)
                return Result.Fail(res.Errors);
        }

        return Result.Ok();
    }

    private Result InstallDoor(
        TiledMap map, 
        DoorMarker marker) {

        if (_doorTiles.Count == 0)
            return Result.Fail(new TiledMapDoorInstallerValidationError(
                "No doors found in tile sets referenced by this map"));

        var doorTiles = _doorTiles[marker.WallDirection].ToArray();

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

        if (marker.Length > doorTiles.Length)
            return Result.Fail(new TiledMapDoorInstallerValidationError(
                "Door marker length in tiles is longer than the door tiles in the map's tile set"));

        // Whether or not we start with the first or last door tile depends on the originating tile set wall 
        // oritentation in relation to the door orientation. 
        //
        // Note: Relying on first and last door tile to be marked up via property in the tile set 
        //
        var tileDrawOrder = GetTileDrawOrder(marker.Direction);
        var sourceTile = doorTiles.Where(t => {
                return tileDrawOrder == StartingTileDrawOrder.First ? t.IsFirstTile : t.IsLastTile; 
            }).First();  
        
        // BUG: The above won't work as we need the last tile not the first tile where lines are reversed.
        //  
        //      How best can we do this? 
        //

        // Applies an offset to account for otherwise invalid tile positions due to a door's line direction
        //
        var startingPoint = marker.GetStartingPointWithOffsetForTileGrid(map.TileWidth);

        // Dungen layout axis points up and to the right where as a tile grid corrds are handled down and 
        // and to the right. Hence we need to flip on Y to get the tile grid coord. 
        //
        // TODO: Is there a reason GetTileIndexContainingWorldSpacePosition can't use up and right?  
        //
        var startingPointTileGrid = new Vector2(startingPoint.X, (map.Height * map.TileHeight) - startingPoint.Y);

        // Grab the world map starting tile from the start of our line
        // 
        uint targetTileIndex = map.GetTileIndexContainingWorldSpacePosition(startingPointTileGrid);

        uint sourceTilesetWidth = _doorTileSet.Columns;
        uint sourceTilesetHeight = _doorTileSet.TileCount / _doorTileSet.Columns;

        uint sourceTileLid = sourceTile.Lid;

        for (int i=0; i<marker.Length; i++) {
            UpdateMapTileIndexRecursively(
                sourceTileLid, 
                doorTiles, 
                sourceTilesetWidth, 
                sourceTilesetHeight, 
                marker.WallDirection, 
                targetTileIndex, 
                map);

            sourceTileLid = CalculateNextTileLaterally(_doorTileTransformMap[marker.WallDirection], marker.Direction, sourceTileLid, sourceTilesetWidth);
            targetTileIndex = CalculateNextTileLaterally(marker.WallDirection, marker.Direction, targetTileIndex, map.Width);
        }

        return Result.Ok();
    }

    private void UpdateMapTileIndexRecursively(
        uint sourceTile, 
        TiledMapDoor[] sourceTiles, 
        uint sourceTilesetWidth,
        uint sourceTilesetHeight,
        CardinalDirection wall,
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

        _logger.LogDebug($"Updated map tile index {targetTile} on layer {layer.Id} for door source tile gid {doorTile.Gid}");

        var newSourceTile = sourceTile;
        var newTargetTile = targetTile;

        // Calculate source and destination tile indexes at depth-1, i.e. door tiles behind this tile
        //
        // TODO: Check values for uint if we go into negtive. Should only happen if silly doors
        //       cover the map end to end, e.g. mistake, but we should test for it.
        //
        sourceTile = CalculateNextTileMedially(_doorTileTransformMap[wall], sourceTile, sourceTilesetWidth);
        targetTile = CalculateNextTileMedially(wall, targetTile, map.Width);

        // Try update map for depth-1 door tile!
        //
        UpdateMapTileIndexRecursively(
            sourceTile, 
            sourceTiles, 
            sourceTilesetWidth, 
            sourceTilesetHeight, 
            wall, 
            targetTile, 
            map);
    }

    private Result CacheDoorTilesForEachCardinalDirection(
        TiledMap map,
        Dictionary<string, TiledSet> tilesets) {

        foreach (KeyValuePair<string, TiledSet> kvp in tilesets) {
            var source = kvp.Key;
            var ts = kvp.Value;

            // Get first Gid for this tile
            //
            var firstGid = map.TileSets.First(kvp => Path.GetFileName(kvp.Source) == source).FirstGid;
        
            foreach (TiledSetTile t in ts.Tiles) {
                bool isFirst = false;
                bool isLast = false;
                bool isDoor = false;

                var walls = new List<CardinalDirection>();

                foreach (TiledProperty p in t.Properties) {
                    if (p.Name == "Type")
                        isDoor = p.Value.ToLower() == "door";

                    if (p.Name == "IsFirst")
                        isFirst = p.Value.ToLower() == "true";

                    if (p.Name == "IsLast")
                        isLast = p.Value.ToLower() == "true";

                    if (p.Name == "Direction") {
                        var wallStrings = p.Value.Split(";").Select(s => s.Trim().ToLower()).ToList();
                        if (wallStrings.Count == 0)
                            return Result.Fail(new TiledMapDoorInstallerValidationError(
                                "No wall direction provided for tile"));

                        for (int i=0; i<wallStrings.Count; i++)
                            walls.Add(ParseDirection(wallStrings[i]));    
                    }
                }

                if (!isDoor)
                    continue;

                CardinalDirection primaryWall = walls[0];

                for (int i=0; i<walls.Count; i++) {
                    var wall = walls[i];
                    if (!_doorTiles.ContainsKey(wall))     
                        _doorTiles.Add(wall, new List<TiledMapDoor>());

                    var tileGid = firstGid + t.Id;

                    // Apply a transform to the tile relative to it's primary wall
                    //
                    tileGid = ApplyTileTransformRelativeToWall(primaryWall, wall, tileGid);

                    _doorTiles[wall].Add(new TiledMapDoor(t.Id, tileGid, isFirst, isLast));

                    // Keep track of original wall direction in tilset so we know how to reference 
                    //
                    if (!_doorTileTransformMap.Contains(new KeyValuePair<CardinalDirection, CardinalDirection>(wall, primaryWall)))
                        _doorTileTransformMap.Add(wall, primaryWall);
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
                        "Something went wrong. Ensure at a minimum NORTH AND EAST are including in the same tile set."));
                
                _doorTileSet = ts;
                break;
            }
        }

        return Result.Ok();
    }

    private StartingTileDrawOrder GetTileDrawOrder(CardinalDirection door) {
        // Assumes right-down tile draw order
        //
        return door switch {
            CardinalDirection.North => StartingTileDrawOrder.Last,
            CardinalDirection.South => StartingTileDrawOrder.First,
            CardinalDirection.East => StartingTileDrawOrder.First,
            CardinalDirection.West => StartingTileDrawOrder.Last
        };
    }

    private uint CalculateNextTileLaterally(CardinalDirection wall, CardinalDirection door, uint currentTile, uint columns) {
        var nextTile = currentTile;

        // Assumes right-down tile draw order
        //
        switch (wall) {
            case CardinalDirection.North: 
            case CardinalDirection.South: 
                return door switch {
                    CardinalDirection.North =>  --nextTile,
                    CardinalDirection.South => ++nextTile,
                    CardinalDirection.East => ++nextTile,
                    CardinalDirection.West => --nextTile
                };

            case CardinalDirection.East:
            case CardinalDirection.West:
                return door switch {
                    CardinalDirection.North => nextTile -= columns,
                    CardinalDirection.South => nextTile += columns,
                    CardinalDirection.East => nextTile += columns,
                    CardinalDirection.West => nextTile -= columns
                };

            default:
                throw new Exception("Something went wrong calculating next tile, either wall or door direction incorrect");
        }
    }

    private uint CalculateNextTileMedially(CardinalDirection wall, uint currentTile, uint columns) {
        var nextTile = currentTile;

        return wall switch {
            CardinalDirection.North => nextTile += columns,
            CardinalDirection.South => nextTile -= columns,
            CardinalDirection.East => --nextTile,
            CardinalDirection.West => ++nextTile
        };
    }


    private uint ApplyTileTransformRelativeToWall(CardinalDirection primaryWall, CardinalDirection wall, uint tileGid) {
        // No transform to apply to first wall (primary)
        //
        if (primaryWall == wall)
            return tileGid;

        var tileGidTrans = tileGid;

        // Transformed in such a way that First and Last indexes remain in the same order
        //
        switch (primaryWall) {
            case CardinalDirection.North: 
                tileGidTrans = 
                    wall switch {
                        CardinalDirection.South => tileGid | Constants.FLIPPED_VERTICALLY_FLAG,
                        CardinalDirection.East => tileGid | Constants.FLIPPED_HORIZONTALLY_FLAG | Constants.FLIPPED_DIAGONALLY_FLAG, // 90d clock
                        CardinalDirection.West => tileGid | Constants.FLIPPED_DIAGONALLY_FLAG 
                    };

                break;

            case CardinalDirection.South: 
                tileGidTrans = 
                    wall switch {
                        CardinalDirection.North => tileGid | Constants.FLIPPED_VERTICALLY_FLAG,
                        CardinalDirection.East => tileGid | Constants.FLIPPED_DIAGONALLY_FLAG, 
                        CardinalDirection.West => tileGid | Constants.FLIPPED_HORIZONTALLY_FLAG | Constants.FLIPPED_DIAGONALLY_FLAG // 90d clock
                    };

                break;

            case CardinalDirection.East: 
                tileGidTrans = 
                    wall switch {
                        CardinalDirection.North => tileGid | Constants.FLIPPED_VERTICALLY_FLAG | Constants.FLIPPED_DIAGONALLY_FLAG, // 90d anti-clock
                        CardinalDirection.South => tileGid | Constants.FLIPPED_DIAGONALLY_FLAG,
                        CardinalDirection.West => tileGid | Constants.FLIPPED_HORIZONTALLY_FLAG
                    };

                break;

            case CardinalDirection.West: 
                tileGidTrans = 
                    wall switch {
                        CardinalDirection.North => tileGid | Constants.FLIPPED_DIAGONALLY_FLAG, 
                        CardinalDirection.South => tileGid | Constants.FLIPPED_VERTICALLY_FLAG | Constants.FLIPPED_DIAGONALLY_FLAG, // 90d anti-clock
                        CardinalDirection.East => tileGid | Constants.FLIPPED_HORIZONTALLY_FLAG
                    };

                break;
        }

        return tileGidTrans;
    }

    private CardinalDirection ParseDirection(string direction) {
        switch (direction) {
            case "north":
            case "n":
                return CardinalDirection.North;

            case "south":
            case "s":
                return CardinalDirection.South;

            case "east":
            case "e":
                return CardinalDirection.East;

            case "west":
            case "w":
                return CardinalDirection.West;

            default: 
                return CardinalDirection.Other;
        }
    }
}

