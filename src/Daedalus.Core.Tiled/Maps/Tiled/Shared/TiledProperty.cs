namespace Daedalus.Core.Tiled.Maps;

using System.Text.Json.Serialization;

using MemoryPack;

[MemoryPackable]
public partial class TiledProperty {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        public TiledProperty(
            string name,
            string type,
            string value
        ) {
            Name = name;
            Type = type;
            Value = value;
        }
    }
