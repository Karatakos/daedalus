namespace Dungen;

using GraphPlanarityTesting.Graphs.DataStructures;

public enum Direction {
    Bi,
    Uni
}

public class DungeonGraph : UndirectedAdjacencyListGraph<Vertex> {
    private Dictionary<int, Direction> edgeDirection;

    public DungeonGraph() : base() {
        edgeDirection = new Dictionary<int, Direction>();
    }
    
    public void AddRoom(int roomId, RoomDefinition definition) {
        RoomTemplateFactory.Register(definition);

        this.AddVertex(new Vertex(roomId, definition));
    }

    public void AddConnection(int roomId1, int roomId2, Direction dir = Direction.Bi) {
        edgeDirection.Add(Layout.GetUniqueRoomPairId(roomId1, roomId2), dir);

        try {
            this.AddEdge(
                this.Vertices.ElementAt(roomId1), 
                this.Vertices.ElementAt(roomId2));
        }
        catch (ArgumentOutOfRangeException) {
            throw new Exception("One of the two room vertices provided does not exist.");
        }
    }

    public Direction GetCorridorDirectionForRooms(int roomId1, int roomId2) {
        if (!edgeDirection.TryGetValue(
            Layout.GetUniqueRoomPairId(roomId1, roomId2),
            out Direction dir))
            throw new Exception($"Whoops, no direction was registered for corridor between rooms {roomId1} and {roomId2}");

        return dir;
    }
}