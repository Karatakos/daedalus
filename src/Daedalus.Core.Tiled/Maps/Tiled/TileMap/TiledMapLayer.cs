
namespace Daedalus.Core.Tiled.Maps;

using System.Text.Json.Serialization;

using MemoryPack;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TiledMapLayerType {
    objectgroup,
    tilelayer,
    group
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TiledMapLayerObjectDrawOrder {
    topdown
}

[MemoryPackable]
public partial class TiledMapLayer {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TiledMapLayerType Type { get; set; }

        [JsonPropertyName("data")]
        public uint[] Data { get; set; }

        [JsonPropertyName("objects")]
        public List<TiledObject> Objects { get; set; }

        [JsonPropertyName("width")]
        public uint Width { get; set; }

        [JsonPropertyName("height")]
        public uint Height { get; set; }

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("visible")]
        public bool Visible { get; set; }

        [JsonPropertyName("opacity")]
        public int Opacity { get; set; }
        
        [JsonPropertyName("draworder")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TiledMapLayerObjectDrawOrder Draworder { get; set; }
        
        [JsonPropertyName("properties")]
        public List<TiledProperty> Properties { get; set; }

        [JsonPropertyName("layers")]
        public List<TiledMapLayer> Layers { get; set; }

        public TiledMapLayer(
            int id,
            TiledMapLayerType type,
            string name,
            uint width,
            uint height
        ) {
            Id = id;
            Type = type;
            Name = name;
            Width = width;
            Height = height;
            Visible = true;
            Opacity = 1;
        }
    }
