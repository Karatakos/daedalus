namespace Daedalus.Tiled;

using Microsoft.Xna.Framework;
using System.Text.Json.Serialization;

/* Tile Map compatible & serializable with Tiled Editor v1.8
*
*/
public class TiledMap {
    [JsonPropertyName("orientation")]
    public string Orientation { get; set; }

    [JsonPropertyName("renderorder")]
    public string RenderOrder { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("tilewidth")]
    public int TileWidth { get; set; }

    [JsonPropertyName("tileheight")]
    public int TileHeight { get; set; }

    [JsonPropertyName("layers")]
    public List<TiledMapLayer> Layers { get; set; }

    [JsonPropertyName("tilesets")]
    public List<TiledSet> TileSets { get; set; }

    [JsonPropertyName("compressionlevel")]
    public int Compressionlevel { get; set; }

    [JsonPropertyName("infinite")]
    public bool Infinite { get; set; }

    [JsonPropertyName("nextlayerid")]
    public int NextlayerId { get; set; }   

    [JsonPropertyName("nextobjectid")] 
    public int NextObjectId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    public TiledMap(
        string orientation,
        string renderorder,
        int width,
        int height,
        int tilewidth,
        int tileheight,
        List<TiledMapLayer> layers,
        List<TiledSet> tileSets
    ) {
        if (tilewidth == 0 || tileheight == 0 || width == 0 || height == 0)
            throw new Exception("Tile, and Map Width/Height CANNOT be set to zero please check the tile map templates.");

        Orientation = orientation;
        RenderOrder = renderorder;
        Width = width;
        Height = height;
        TileWidth = tilewidth;
        TileHeight = tileheight;
        Layers = layers;
        TileSets = tileSets;

        Compressionlevel = -1;
        Infinite = false;
        Type = "map";
    }

    public Vector2 GetWorldSpacePositionForTileIndex(int tileIndex) {
        // x = ((t % w) * tw) - tw
        // y = ((t / h) * th) - th
        //
        int x = (((tileIndex % Width) + 1) * TileWidth) - TileWidth;
        int y = ((int)Math.Floor(((decimal)tileIndex / Height) + 1) * TileHeight) - TileHeight;

        return new Vector2(x, y);
    }

    public int GetTileIndexForWorldSpacePosition(Vector2 pos) {
        // t = ((y / th) * w ) + (x / tw)
        //
        int row = (int)Math.Floor((decimal)pos.Y / TileHeight) * Width;
        int col = ((int)Math.Floor((decimal)pos.X / TileWidth) % Width);
        
        return Math.Max(row + col, 0);
    }
}

public class TiledMapLayer {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("data")]
        public int[] Data { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("visible")]
        public bool Visible { get; set; }

        [JsonPropertyName("opacity")]
        public int Opacity { get; set; }

        public TiledMapLayer(
            int id,
            string name,
            int[] data,
            int width,
            int height
        ) {
            Id = id;
            Name = name;
            Data = data;
            Width = width;
            Height = height;

            Type = "tilelayer";
            Visible = true;
            Opacity = 1;
        }
    }

    public class TiledSet { 
        [JsonPropertyName("firstgid")]
        public int FirstGid { get; }

        [JsonPropertyName("source")]
        public string Source { get; }

        public TiledSet(int firstGid, string source) {
            FirstGid = firstGid;
            Source = source;
        }
    }
