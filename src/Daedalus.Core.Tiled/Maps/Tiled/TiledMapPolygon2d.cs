namespace Daedalus.Core.Tiled.Maps;

using System.Text.Json.Serialization;

public class TiledMapPolygon2d {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        public TiledMapPolygon2d(
            float x,
            float y
        ) {
            X = x;
            Y = y;
        }
    }
