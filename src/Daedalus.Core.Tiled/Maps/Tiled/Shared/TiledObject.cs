namespace Daedalus.Core.Tiled.Maps;

using System.Text.Json.Serialization;

public class TiledObject {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("width")]
        public float Width { get; set; }

        [JsonPropertyName("height")]
        public float Height { get; set; }

        [JsonPropertyName("rotation")]
        public float Rotation { get; set; }

        [JsonPropertyName("point")]
        public bool IsPoint { get; set; }

        [JsonPropertyName("ellipse")]
        public bool IsEllipse { get; set; }

        [JsonPropertyName("polygon")]
        public List<TiledPolygon2d> Polygon { get; set; }

        [JsonPropertyName("gid")]
        public int Gid { get; set; }

        [JsonPropertyName("properties")]
        public List<TiledProperty> Properties { get; set; }

        public TiledObject(
            int id,
            string name,
            string type
        ) {
            Id = id;
            Name = name;
            Type = type;
        }
    }
