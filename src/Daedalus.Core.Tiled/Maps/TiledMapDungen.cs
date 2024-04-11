namespace Daedalus.Core.Tiled.Maps;

using System.Security.Cryptography.X509Certificates;
using Daedalus.Core.Tiled.Maps;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TiledMapDungenRoomType {
    Entrance,
    Exit,
    Normal,
    Arena,
    Corridor
}

public class TiledMapDungen : TiledMap {

    public List<TiledMapDungenRoom> Rooms { get; set; }

    public TiledMapDungen(
        int width,
        int height,
        int tilewidth,
        int tileheight
    ) : base (
        width,
        height,
        tilewidth,
        tileheight) {
            Rooms = new List<TiledMapDungenRoom>();
        }

    public int GetRoomForTileIndex(int tileIndex) {
        return 0;
    }
}

public class TiledMapDungenRoom {
    public List<int> AccessibleRooms { get; set; }
    public List<int> TileIndices { get; set; }
    public int Number { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TiledMapDungenRoomType Type;

    public TiledMapDungenRoom(int number, TiledMapDungenRoomType type) {
        Number = number;
        Type = type;

        AccessibleRooms = new List<int>(); 
        TileIndices = new List<int>();
    }
}