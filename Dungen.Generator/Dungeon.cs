namespace Dungen.Generator;

public class DungeonLayout {
    public DungeonGraph Graph { get; private set; }
    public Room[] Rooms { get; private set; }

    public DungeonLayout (Layout layout, DungeonGraph graph) {
        Rooms = layout.Rooms.Values.ToArray();
        Graph = graph;
    }
}
