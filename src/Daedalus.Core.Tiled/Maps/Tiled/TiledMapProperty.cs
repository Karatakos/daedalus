namespace Daedalus.Core.Tiled.Maps;

using System.Text.Json.Serialization;

public class TiledMapProperty {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        public TiledMapProperty(
            string name,
            string type,
            string value
        ) {
            Name = name;
            Type = type;
            Value = value;
        }
    }
