namespace Dungen;

using GraphPlanarityTesting.Graphs.DataStructures;

public class Layout {
    public Dictionary<Vertex, Room> Rooms { get; private set; }
    public UndirectedAdjacencyListGraph<Vertex> Graph { get; set; }
    public Energy Energy { get; set; }
    public int Depth { get; set; }
    public bool Valid { get; private set; }
    private float _sigma = 0.0f;
    private Dictionary<int, Energy> _roomPairEnergyCache;
    private Dictionary<int, Energy> _roomEnergyCache;

    public Layout(Layout source, UndirectedAdjacencyListGraph<Vertex> graph) {
        Graph = graph;
    
        Rooms = new Dictionary<Vertex, Room>();
        foreach (KeyValuePair<Vertex, Room> kvp in source.Rooms)
            Rooms.Add(kvp.Key, new Room(kvp.Value));

        _roomPairEnergyCache = new Dictionary<int, Energy>();
        foreach (KeyValuePair<int, Energy> kvp in source._roomPairEnergyCache)
            _roomPairEnergyCache.Add(kvp.Key, new Energy(kvp.Value.Collision, kvp.Value.Connectivity, kvp.Value.E));
            
        _roomEnergyCache = new Dictionary<int, Energy>();
        foreach (KeyValuePair<int, Energy> kvp in source._roomEnergyCache)
            _roomEnergyCache.Add(kvp.Key, new Energy(kvp.Value.Collision, kvp.Value.Connectivity, kvp.Value.E));

        Energy = new Energy(source.Energy.Collision, source.Energy.Connectivity, source.Energy.E); 
        Depth = source.Depth; 
        Valid = source.Valid;     
    }

    public Layout(int depth) {
        Rooms = new Dictionary<Vertex, Room>();
        Energy = new Energy();
        Depth = depth;
        Valid = false;

        _roomPairEnergyCache = new Dictionary<int, Energy>();
        _roomEnergyCache = new Dictionary<int, Energy>();
    }

    public List<Room> GetRoomsForVertices(IEnumerable<Vertex> vertices) {
        List<Room> rooms = new List<Room>();
        foreach (Vertex vertex in vertices) {
            if (Rooms.TryGetValue(vertex, out Room room))
                rooms.Add(room);          
        }

        return rooms;
    }

    public float ComputeRoomDistance() {
        float distance = 0f;

        foreach (Room room1 in Rooms.Values) {
            foreach (Room room2 in Rooms.Values) {
                if (room1 == room2)
                    continue;
                    
                distance += Room.GetRoomCenterDistance(room1, room2);
            }
        }
        
        return distance;
    }

    public static bool AreSignificantlyDifferent(Layout layout1, Layout layout2) {
        float d1 = layout1.ComputeRoomDistance();
        float d2 = layout2.ComputeRoomDistance();

        return Math.Abs(d1 - d2) > Config.SignificantDistanceThreshold;
    }

    public bool TryGetRoomEnergy(Vertex vertex, Room room, out Energy e) {
        int roomKey = vertex.Id;

        if (!_roomEnergyCache.TryGetValue(roomKey, out e))
            return false;

        return true;
    }

    public Energy Update() {
        foreach (KeyValuePair<Vertex, Room> kvp in Rooms)
            Update(kvp.Key, kvp.Value);  

        return Energy;         
    }

    public Energy Update(Vertex updatedVertex, Room updatedRoom) {
        if (Rooms[updatedVertex] != updatedRoom)
            throw new Exception("Room not found in layout collection");

        int updatedRoomKey = updatedVertex.Id;

        // Reset or initialize energy for this room
        //
        Energy roomE = new Energy();
        if (!_roomEnergyCache.ContainsKey(updatedRoomKey))
            _roomEnergyCache.Add(updatedRoomKey, roomE);
        else 
            _roomEnergyCache[updatedRoomKey] = roomE;

        List<Vertex> neighbours = Graph.GetNeighbours(updatedVertex).ToList();
        foreach (KeyValuePair<Vertex, Room> kvp in Rooms) {
            Vertex v = kvp.Key;
            Room r = kvp.Value;

            if (r == updatedRoom)
                continue;

            float ca = ComputeCollisionArea(updatedRoom, r);

            float c = 0f;
            if (neighbours.Contains(v))
                // BUG: Connectivity can be < 0 there is an issue somewhere
                //
                c = ComputeConnectivity(updatedRoom, r);

            Energy roomPairE = new Energy(ca, c);

            // We want commutivity here because ab.energy == ba.energy and so we 
            // only want to track one entry
            //
            int roomPairKey = Layout.GetUniqueRoomPairId(updatedVertex.Id, v.Id);

            if (!_roomPairEnergyCache.ContainsKey(roomPairKey)) {
                _roomPairEnergyCache.Add(roomPairKey, roomPairE);

                Energy.Collision += roomPairE.Collision;
                Energy.Connectivity += roomPairE.Connectivity;
            }
            else {
                float cDiff = roomPairE.Connectivity - _roomPairEnergyCache[roomPairKey].Connectivity;
                float caDiff = roomPairE.Collision - _roomPairEnergyCache[roomPairKey].Collision;

                float tmpPrevCollision = Energy.Collision;
                float tmpPrevConnectivity = Energy.Collision;

                // BUG: Collision can be less than 0. May be some rounding error somewhere
                // 
                Energy.Collision += caDiff;
                Energy.Connectivity += cDiff;

                Energy.Collision = Math.Abs(Energy.Collision) < Config.Tolerance * 10 ? 0f : Energy.Collision;
                Energy.Connectivity = Math.Abs(Energy.Connectivity) < Config.Tolerance * 10 ? 0f : Energy.Connectivity;

                _roomPairEnergyCache[roomPairKey].Connectivity += cDiff;
                _roomPairEnergyCache[roomPairKey].Collision += caDiff;
            }  

            // BUG: All rooms need updating
            //
            // Aggregate & cache room energies as we need this later when looking 
            // for non-zero energy rooms to perturb. We don't need to worry about delta
            // here since we reset at the top of this function.
            //
            _roomEnergyCache[updatedRoomKey].Collision += _roomPairEnergyCache[roomPairKey].Collision;
            _roomEnergyCache[updatedRoomKey].Connectivity += _roomPairEnergyCache[roomPairKey].Connectivity;
        }

        // Finally, compute the layout's energy
        //
        Energy.E = ComputeEnergy(Energy.Collision, Energy.Connectivity);

        Valid = Energy.E == 0;

        return Energy;
    }

    private float ComputeEnergy(float collisionArea, float connectivity) {
        float e = 1f;

        _sigma = 100 * (50 * 50);
        
        e *= (float)Math.Pow(Math.E, collisionArea / (_sigma));
        e *= (float)Math.Pow(Math.E, connectivity / (_sigma));
        
        return e-1;
    }

    private float ComputeCollisionArea(Room r1, Room r2) {
        return Room.ComputeRoomCollisionArea(r1, r2);
    }

    private float ComputeConnectivity(Room r1, Room r2) {
        float contact = Room.ComputeRoomContactArea(r1, r2);
        if (contact < ((Config.DoorWidth - Config.Tolerance) + (Config.DoorToCornerMinGap * 2))) {
            // Looks a bit weird but semi-arbitrarily using distance from centers
            // as a penalty metric where two rooms are not adequately connected
            //
            return Math.Abs(Room.GetRoomCenterDistance(r1, r2));
        }
    
        return 0f;
    }

    public static int GetUniqueRoomPairId(int id1, int id2) {
        int min = Math.Min(id1, id2);
        int max = Math.Max(id1, id2);

        // Cantor's pairing https://en.wikipedia.org/wiki/Pairing_function
        //
        // Replaces less efficient (Math.Pow(2, updatedVertex.Id) * Math.Pow(3, v.Id));
        //
        return (((min + max) * (min + max + 1)) / 2) + max;
    }
}

public class Energy {
    public Energy(float collisionArea = 0f, float connectivity = 0f, float energy = 0f) {
        Collision = collisionArea;
        Connectivity = connectivity;
        E = energy; 
    }

    public float Connectivity = 0f;
    public float Collision = 0f;
    public float E = 0f;

    public override string ToString()
    {
        return String.Format("E: {0} CA: {1} C: {2}", 
            E.ToString(), Collision.ToString(), Connectivity.ToString());
    }
}