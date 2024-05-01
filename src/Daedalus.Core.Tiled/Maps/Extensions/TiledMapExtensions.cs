namespace Daedalus.Core.Tiled.Maps;

using Microsoft.Xna.Framework;

public static class TiledMapExtensions {
    public static Vector2 GetWorldSpacePositionForTileIndex(this TiledMap map, uint tileIndex) {
        // x = ((t % w) * tw) - tw
        // y = ((t / h) * th) - th
        //
        uint x = (((tileIndex % map.Width) + 1) * map.TileWidth) - map.TileWidth;
        uint y = ((uint)Math.Floor(((decimal)tileIndex / map.Height) + 1) * map.TileHeight) - map.TileHeight;

        return new Vector2(x, y);
    }

    public static uint GetTileIndexContainingWorldSpacePosition(this TiledMap map, Vector2 pos) {
        // t = ((y / th) * w ) + (x / tw)
        //
        uint row = (uint)Math.Floor((decimal)pos.Y / map.TileHeight) * map.Width;
        uint col = (uint)Math.Floor((decimal)pos.X / map.TileWidth) % map.Width;
        
        return Math.Max(row + col, 0);
    }

    public static Vector2 GetMapCenter(this TiledMap map) {
        return new Vector2(map.Width * map.TileWidth / 2, map.Height * map.TileHeight / 2);
    }
}