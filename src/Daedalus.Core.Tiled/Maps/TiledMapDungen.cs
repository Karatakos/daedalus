namespace Daedalus.Core.Tiled.Maps;

using MemoryPack;

using Daedalus.Core.Tiled.Maps;

using System.Text.Json.Serialization;
using System.Security.Cryptography.X509Certificates;

[MemoryPackable]
public partial class TiledMapDungen : TiledMap {

    public List<TiledMapDungenRoom> Rooms { get; set; }

    public TiledMapDungen(
        uint width,
        uint height,
        uint tilewidth,
        uint tileheight
    ) : base (
        width,
        height,
        tilewidth,
        tileheight) {
            Rooms = new List<TiledMapDungenRoom>();
        }
}

[MemoryPackable]
public partial class TiledMapDungenRoom {
    public List<int> AccessibleRooms { get; set; }
    public List<uint> TileIndices { get; set; }
    public int Number { get; set; }

    public TiledMapDungenRoom(int number) {
        Number = number;

        AccessibleRooms = new List<int>(); 
        TileIndices = new List<uint>();
    }
}