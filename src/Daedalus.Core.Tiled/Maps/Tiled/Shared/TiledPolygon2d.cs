namespace Daedalus.Core.Tiled.Maps;

using System.Text.Json.Serialization;

using MemoryPack;

[MemoryPackable]
public partial class TiledPolygon2d {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        public TiledPolygon2d(
            float x,
            float y
        ) {
            X = x;
            Y = y;
        }
    }
