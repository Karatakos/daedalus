namespace Dungen.Generator;

public enum RoomType {
    Normal,
    Corridor,
    Entrance,
    Exit,
    Arena
}

public class RoomDefinition {
    public RoomType Type { get; set; }

    public List<RoomBlueprint> Blueprints { get; set; }

    public RoomDefinition(List<RoomBlueprint> blueprints, RoomType type = RoomType.Normal) {
        Type = type;
        Blueprints = blueprints;
    }
}