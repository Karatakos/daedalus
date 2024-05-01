namespace Daedalus.Core.Tiled.Maps;

using System.Text.Json.Serialization;

/* Tile Set compatible & serializable with Tiled Editor v1.8
*
*/
public class TiledSet {
    [JsonPropertyName("name")] 
    public string Name { get; set; }

    [JsonPropertyName("type")] 
    public string Type { get; set; }

    [JsonPropertyName("image")] 
    public string Image { get; set; }

    [JsonPropertyName("imageheight")] 
    public int ImageHeight { get; set; }
    
    [JsonPropertyName("imagewidth")] 
    public int ImageWidth { get; set; }
    
    [JsonPropertyName("margin")] 
    public int Margin { get; set; }
    
    [JsonPropertyName("spacing")] 
    public int Spacing { get; set; }
    
    [JsonPropertyName("tilecount")] 
    public uint TileCount { get; set; }
    
    [JsonPropertyName("tilewidth")] 
    public uint TileWidth { get; set; }
    
    [JsonPropertyName("tileheight")] 
    public uint TileHeight { get; set; }
    
    [JsonPropertyName("columns")] 
    public uint Columns { get; set; }

    [JsonPropertyName("tiles")] 
    public List<TiledSetTile> Tiles { get; set; }

    public TiledSet() {}
}