namespace Daedalus.Core.Tiled.Maps;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TiledSetType {
    map
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TiledSetOrientation {
    orthogonal
}

/* Tile Set compatible & serializable with Tiled Editor v1.8
*
*/
public class TiledSetTile {
    [JsonPropertyName("id")] 
    public int Id { get; set; }

    [JsonPropertyName("objectgroup")] 
    public TiledSetObjectGroup ObjectGroup { get; set; }

    [JsonPropertyName("properties")] 
    public List<TiledProperty> Properties { get; set; }

    public TiledSetTile() {}
}