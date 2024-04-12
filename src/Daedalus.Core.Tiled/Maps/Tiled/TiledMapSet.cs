namespace Daedalus.Core.Tiled.Maps;

using System.Text.Json.Serialization;

public class TiledMapSet { 
        [JsonPropertyName("firstgid")]
        public int FirstGid { get; }

        [JsonPropertyName("source")]
        public string Source { get; }

        public TiledMapSet(int firstGid, string source) {
            FirstGid = firstGid;
            Source = source;
        }
    }
