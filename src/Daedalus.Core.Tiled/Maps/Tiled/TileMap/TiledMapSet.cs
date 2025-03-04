namespace Daedalus.Core.Tiled.Maps;

using System.Text.Json.Serialization;

using MemoryPack;

[MemoryPackable]
public partial class TiledMapSet { 
        [JsonPropertyName("firstgid")]
        public uint FirstGid { get; }

        [JsonPropertyName("source")]
        public string Source { get; }

        public TiledMapSet(uint firstGid, string source) {
            FirstGid = firstGid;
            Source = source;
        }
    }
