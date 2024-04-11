namespace Daedalus.Core.Tiled.Maps;

using Microsoft.Xna.Framework;

public static class TiledMapExtensions {
    public static Vector2 GetWorldSpacePositionForTileIndex(this TiledMap map, int tileIndex) {
        // x = ((t % w) * tw) - tw
        // y = ((t / h) * th) - th
        //
        int x = (((tileIndex % map.Width) + 1) * map.TileWidth) - map.TileWidth;
        int y = ((int)Math.Floor(((decimal)tileIndex / map.Height) + 1) * map.TileHeight) - map.TileHeight;

        return new Vector2(x, y);
    }

    public static int GetTileIndexContainingWorldSpacePosition(this TiledMap map, Vector2 pos) {
        // t = ((y / th) * w ) + (x / tw)
        //
        int row = (int)Math.Floor((decimal)pos.Y / map.TileHeight) * map.Width;
        int col = ((int)Math.Floor((decimal)pos.X / map.TileWidth) % map.Width);
        
        return Math.Max(row + col, 0);
    }
}