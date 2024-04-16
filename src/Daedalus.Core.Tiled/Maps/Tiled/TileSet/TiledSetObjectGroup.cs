namespace Daedalus.Core.Tiled.Maps;

using System.Text.Json.Serialization;

public class TiledSetObjectGroup {
    [JsonPropertyName("id")] 
    public int Id { get; set; }

    [JsonPropertyName("name")] 
    public string Name { get; set; }

    [JsonPropertyName("type")] 
    public string Type { get; set; }

    [JsonPropertyName("draworder")] 
    public string DrawOrder { get; set; }

    [JsonPropertyName("opacity")] 
    public int Opacity { get; set; }

    [JsonPropertyName("visible")] 
    public bool Visible { get; set; }

    [JsonPropertyName("x")] 
    public float X { get; set; }

    [JsonPropertyName("y")] 
    public float Y { get; set; }

    [JsonPropertyName("objects")] 
    public List<TiledObject> Objects { get; set; }

    public TiledSetObjectGroup() {}
}