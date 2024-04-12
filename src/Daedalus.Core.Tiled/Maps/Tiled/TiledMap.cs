namespace Daedalus.Core.Tiled.Maps;

using System.Text;
using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TiledMapType {
    map
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TiledMapOrientation {
    orthogonal
}

/* Tile Map compatible & serializable with Tiled Editor v1.8
*
*/
public class TiledMap {
    [JsonPropertyName("orientation")] 
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TiledMapOrientation Orientation { get; set; }

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
    public List<TiledMapSet> TileSets { get; set; }

    [JsonPropertyName("compressionlevel")]
    public int Compressionlevel { get; set; }

    [JsonPropertyName("infinite")]
    public bool Infinite { get; set; }

    [JsonPropertyName("nextlayerid")]
    public int NextlayerId { get; set; }   

    [JsonPropertyName("nextobjectid")] 
    public int NextObjectId { get; set; }

    [JsonPropertyName("type")] 
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TiledMapType Type { get; set; }

    [JsonPropertyName("properties")]
    public List<TiledMapProperty> Properties { get; set; }

    public TiledMap(
        int width,
        int height,
        int tilewidth,
        int tileheight
    ) {
        if (tilewidth == 0 || tileheight == 0 || width == 0 || height == 0)
            throw new Exception("Tile, and Map Width/Height CANNOT be set to zero please check the tile map templates.");

        Orientation = TiledMapOrientation.orthogonal;
        RenderOrder = "right-down";
        Type = TiledMapType.map;
        Width = width;
        Height = height;
        TileWidth = tilewidth;
        TileHeight = tileheight;
        Layers = new List<TiledMapLayer>();
        TileSets = new List<TiledMapSet>();
        Compressionlevel = -1;
        Infinite = false;
    }
}
