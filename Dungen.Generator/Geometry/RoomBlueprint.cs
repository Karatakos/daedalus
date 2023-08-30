namespace Dungen.Generator;

public class RoomBlueprint {
    public List<Vector2F> Points { get; set; }
    
    public DoorContraint DoorConstraint { get; set; }

    public RoomBlueprint(List<Vector2F> points) : this(points, new DoorContraint()) {}  

    public RoomBlueprint(List<Vector2F> points, DoorContraint doors) {
        Points = points;
        DoorConstraint = doors;
    }     
}