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

    internal TiledMapDoorManager(
        ILoggerFactory loggerFactory) {

        _logger = loggerFactory.CreateLogger<TiledMapDoorManager>();
        _loggerFactory = loggerFactory;

        _doorTiles = new Dictionary<CardinalDirection, List<TiledMapDoor>>();
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

        // Relying on first door tile to be marked up via property in the tile set
        //
        // TODO: Is there a way of calculating this?
        //
        uint sourceTileLid = doorTiles.Where(t => t.IsFirstTile).Select(t => t.Lid).First();  

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

        for (int i=0; i<marker.Length; i++) {
            UpdateMapTileIndexRecursively(
                sourceTileLid, 
                doorTiles, 
                sourceTilesetWidth, 
                sourceTilesetHeight, 
                marker.WallDirection, 
                targetTileIndex, 
                map);
            
            // Calculate next source and destination tile indexes 
            //
            switch (marker.Direction) {
                case CardinalDirection.North:           // East or West wall
                    sourceTileLid += sourceTilesetWidth;
                    targetTileIndex -= map.Width;            
                    break;

                case CardinalDirection.South:
                    sourceTileLid += sourceTilesetWidth;
                    targetTileIndex += map.Width;           
                    break;

                case CardinalDirection.East:            // North or South wall
                    sourceTileLid ++;
                    targetTileIndex --;   
                    break;

                case CardinalDirection.West:
                    sourceTileLid ++;
                    targetTileIndex ++;  
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
        CardinalDirection doorOnRoomSide,
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
        switch (doorOnRoomSide) {
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
            doorOnRoomSide, 
            targetTile, 
            map);
    }

    private Result CacheDoorTilesForEachCardinalDirection(
        TiledMap map,
        Dictionary<string, TiledSet> tilesets) {

        foreach (KeyValuePair<string, TiledSet> kvp in tilesets) {
            var source = kvp.Key;
            var ts = kvp.Value;
            
            foreach (TiledSetTile t in ts.Tiles) {
                bool isFirstTile = false;
                bool isDoor = false;
                CardinalDirection wall = CardinalDirection.Other;

                foreach (TiledProperty p in t.Properties) {
                    if (p.Name == "Type")
                        isDoor = p.Value == "Door";

                    if (p.Name == "IsFirstDoor")
                        isFirstTile = p.Value == "true";

                    if (p.Name == "Direction") {
                        if (!Enum.TryParse<CardinalDirection>(p.Value, out wall))
                            return Result.Fail(new TiledMapDoorInstallerValidationError(
                                "Incorrect value for Direction property. Should be North, South, East or West"));
                    }
                }

                if (!isDoor)
                    continue;

                if (!_doorTiles.ContainsKey(wall))     
                    _doorTiles.Add(wall, new List<TiledMapDoor>());

                // Get first Gid for this tile
                //
                var firstGid = map.TileSets.First(kvp => Path.GetFileName(kvp.Source) == source).FirstGid;
                var tileGid = firstGid + t.Id;

                // Gid is just the local tile index since there is no transform to apply
                //
                _doorTiles[wall].Add(new TiledMapDoor(t.Id, tileGid, isFirstTile));
            }

            // Break if we've processed doors from this tileset. We're done.
            // 
            // Context: We only support doors on a single tile set. Fine uunless we start supporting 
            //          multiple tilesets per map, e.g. rooms painted via differing tilesets but that 
            //          would require a bigger overhaul incl. significant work in the tilemap merger.
            //
            if (_doorTiles.Count > 0) { 

                // We only need door tiles for two directions (N and E) on the tile sheet.
                // For S we rotate E by flipping horizontally then diagonally)
                // For W we flip E horizontally
                //
                // TODO: This is very specific to a single use case -- come up with a flexibile system
                //
                if (!_doorTiles.ContainsKey(CardinalDirection.North) || !_doorTiles.ContainsKey(CardinalDirection.East))
                    return Result.Fail(new TiledMapDoorInstallerValidationError(
                        "North or East facing door's have not been defined in the tileset -- though South and West can be dynamically added, North and East are mandatory"));

                if (!_doorTiles.ContainsKey(CardinalDirection.West)) {
                    _doorTiles.Add(CardinalDirection.West, new List<TiledMapDoor>());

                    foreach (TiledMapDoor d in _doorTiles[CardinalDirection.East]) {
                        _doorTiles[CardinalDirection.West].Add(
                            new TiledMapDoor(d.Lid, d.Gid | Constants.FLIPPED_HORIZONTALLY_FLAG, d.IsFirstTile));
                    }
                }

                if (!_doorTiles.ContainsKey(CardinalDirection.South)) {
                    _doorTiles.Add(CardinalDirection.South, new List<TiledMapDoor>());

                    foreach (TiledMapDoor d in _doorTiles[CardinalDirection.East]) {
                        _doorTiles[CardinalDirection.South].Add(
                            new TiledMapDoor(
                                d.Lid, 
                                d.Gid | Constants.FLIPPED_HORIZONTALLY_FLAG | Constants.FLIPPED_DIAGONALLY_FLAG, 
                                d.IsFirstTile));
                    }
                }

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
}

