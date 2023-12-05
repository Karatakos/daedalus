namespace Dungen;

public class Room : Polygon2d {
    public RoomBlueprint Blueprint;

    public RoomType Type { get; set; }

    public int Number { get; set; } = -1;

    public Vector2F Position { get; set; } = new Vector2F(0, 0);

    public List<Door> Doors { get; set; }

    public Room(Room copy) : base(copy) {
        Number = copy.Number;
        Type = copy.Type;
        Blueprint = copy.Blueprint;
        Doors = copy.Doors;
    }

    public Room(RoomBlueprint blueprint, RoomType type = RoomType.Normal, int number = -1) : 
        this (blueprint, new Vector2F(0, 0), type, number) {}

    public Room(RoomBlueprint blueprint, Vector2F pos, RoomType type = RoomType.Normal, int id = -1) : base(blueprint.Points) {
        Number = id;
        Type = type;
        Blueprint = blueprint;
        Position = pos;
        Doors = new List<Door>();
    }

    public static float ComputeRoomDistance(Room room1, Room room2) {
        float distance = 1e10F;
        foreach (Vector2F p in room1.Points)
            foreach (Line line in room2.Boundary) 
                distance = Math.Min(distance, Math2D.PointToLineSegmentSqDistance(p, line));
        
        return (float)Math.Sqrt(distance);
    }

    public static float GetRoomCenterDistance(Room room1, Room room2) {
        return Vector2F.Magnitude(room1.GetCenter() - room2.GetCenter());
    }

    // Are (and by how many units) are the room's walls overlapping
    //
    public static float ComputeRoomContactArea(Room room1, Room room2) {
        return Polygon2d.ComputeContactArea(room1, room2);
    }

    // Are (and by what area) are the rooms colliding
    //
    public static float ComputeRoomCollisionArea(Room room1, Room room2) {
        AABB2F aabb1 = room1.GetBoundingBox();
        AABB2F aabb2 = room1.GetBoundingBox();

        // Simple AABB check since getting accurate collision area is expensive
        //
        if ((aabb1.Max.x < aabb2.Min.x || aabb1.Min.x > aabb2.Max.x) || 
            (aabb1.Max.y < aabb2.Min.y || aabb1.Min.y > aabb2.Max.y))
            return 0F;

        return Polygon2d.ComputeCollideArea(room1, room2);
    }

    public override void Translate(Vector2F v) {
        Position += v;

        base.Translate(v);
    }

    public Door GetDoorForLine(ValueTuple<Vector2F, Vector2F> line) {
        foreach (Door door in Doors)
            if (line == ValueTuple.Create<Vector2F, Vector2F>(
                    door.Position.Item1 + Position,
                    door.Position.Item2 + Position))
                return door;

        return null;
    }

    protected override void GenerateBoundary() {
        List<Line> tmp = new List<Line>(); 
        for (int i=1; i< Points.Count; i++)
            tmp.Add(CreateNewBoundaryLine(ValueTuple.Create(Points[i-1], Points[i])));

        tmp.Add(CreateNewBoundaryLine(ValueTuple.Create(Points[Points.Count-1], Points[0])));

        Boundary = tmp;
    }

    private Line CreateNewBoundaryLine(ValueTuple<Vector2F, Vector2F> line) {
        Line newLine;
        if (BoundaryLineIsDoor(line))
            newLine = new BoundaryLine(line.Item1, line.Item2, false, true);
        else
            newLine = new BoundaryLine(line.Item1, line.Item2, BoundaryLineCanContainDoors(line));

        return newLine;
    }

    private bool BoundaryLineIsDoor(ValueTuple<Vector2F, Vector2F> line) {
        // TODO: Optimize! Just store Door reference in Boundary Line, better stil;
        //       implement a new line type for a door. Clients can use this type 
        //       to get more information on a door when drawing
        //
        return (GetDoorForLine(line) != null);
    }

    private bool BoundaryLineCanContainDoors(ValueTuple<Vector2F, Vector2F> line) {
        return LineSatisfiesDoorConstraintType(line, DoorConstraintType.Placeholder, true);
    }

    private bool LineSatisfiesDoorConstraintType(ValueTuple<Vector2F, Vector2F> line, DoorConstraintType type, bool defaultValue) {
        if (Blueprint.DoorConstraint.HasRestrictedDoor && 
            Blueprint.DoorConstraint.Type == type) {     
            for (int i=0; i<Blueprint.DoorConstraint.AllowedPositions.Count; i++) {
                if (line == ValueTuple.Create<Vector2F, Vector2F>(
                    Blueprint.DoorConstraint.AllowedPositions[i].Item1 + Position,
                    Blueprint.DoorConstraint.AllowedPositions[i].Item2 + Position)) {
                    return true;
                }
            }

            return false;
        }

        return defaultValue;
    }
}