namespace Dungen.Generator;

using System;
using System.Collections.Generic;

using GraphPlanarityTesting.Graphs.DataStructures;

public class DungeonGeneratorProps {
    public  DungeonGraph Graph { get; set; }
    
    public float DoorWidth { get; set; } = 1f;

    public float DoorToCornerMinGap { get; set; } = 1f; 
    
    public int TargetSolutions { get; set; } = 1;
}   

public class DungeonGenerator {
    private bool _initilized = false;

    private GraphDecomposition _decomp;

    private List<Vertex> _visited;

    private ConfigSpacesBuilder _csBuilder;

    private List<Layout> Solutions { get; set; }

    private List<DungeonLayout> Dungeons { get; set; }

    private DungeonGraph Graph { get; set; }
    
    public DungeonGenerator(DungeonGeneratorProps props) {
        Graph = props.Graph;
        Config.DoorWidth = props.DoorWidth;
        Config.DoorToCornerMinGap = props.DoorToCornerMinGap;
        Config.TargetSolutionCount = props.TargetSolutions;

        Solutions = new List<Layout>();
        Dungeons = new List<DungeonLayout>();

        _visited = new List<Vertex>();
        _csBuilder = new ConfigSpacesBuilder();
    }

    public void Initialize() {
        // Generates chains from graph
        //
        _decomp = GraphDecomposer.Generate(Graph);  

        // Cache config spaces for all room template combinations
        //
        _csBuilder.Precompute(RoomTemplateFactory.GetAllTemplates());

         _initilized = true;
    }

    public bool TryGenerate() {
        if (!_initilized) 
            Initialize();

        int trialsTotal = 0;
        
        while (trialsTotal <= Config.MaxResets) {
            int layoutsTotal = 0;
            int layoutCounter = 0;
            int chainIndex = -1;
            int backtrackCounter = 0;

            Stack<Layout> layouts = new Stack<Layout>();

            // Start with an empty layout for the first chain!
            //
            layouts.Push(new Layout(chainIndex));

            // Processing each chain via a Depth First Search so we get automatic 
            // backtracking in case new layouts are not added to the tree and can stop
            // once we get the required number of solutions
            //
            while (layouts.TryPop(out Layout layout)) {
                if (layout.Depth+1 < chainIndex) {
                    backtrackCounter++;

                    Console.WriteLine();
                    Console.WriteLine(String.Format("Backtracking to depth {0}", layout.Depth+1));
                }

                // Move to the next chain
                //    
                if (layout.Depth+1 < _decomp.Chains.Count)
                    chainIndex = layout.Depth+1;

                // TODO: Find a better way, e.g. we can achieve O(n) if we store depth in vertive
                //      
                //      Clear vertices from _visited that are in lower chains 
                //      not yet placed (due to backtracking)
                //
                for (int i=chainIndex; i<_decomp.Chains.Count; i++) {
                    foreach (Vertex v in _decomp.Chains[i].Vertices) {
                        if (_visited.Contains(v))
                            _visited.Remove(v);
                    }
                }

                Chain chain = _decomp.Chains[chainIndex];

                // Track all generated layouts
                //
                layoutsTotal += layoutCounter;
       
                foreach (Layout newLayout in GenerateLayoutsForChain(chain, layout)) {
                    if (newLayout != null && newLayout.Rooms.Count > 0) {
                        if (chain.Depth == _decomp.Chains.Count-1) {  
                            Layout layoutClone = new Layout(newLayout, Graph);

                            InstallDoorsForLayout(layoutClone);

                            Solutions.Add(layoutClone);

                            // Store a copy for the client (via a simplified wrapper)
                            //
                            Dungeons.Add(new DungeonLayout(new Layout(layoutClone, Graph), Graph));

                            if (Solutions.Count == Config.TargetSolutionCount) {
                                Console.WriteLine(String.Format("Desired number of solutions generated {0}", Solutions.Count));
                                
                                for (int i=0; i<Solutions.Count; i++)
                                    Console.WriteLine(String.Format("Solution {0} energy {1}", i, Solutions[i].Energy));

                                return true;
                            }
                        }  

                        layouts.Push(newLayout);

                        if (layoutCounter++ == Config.MaxPartialLayouts) {
                            Console.WriteLine(String.Format("Desired number of layouts generated {0}", layoutCounter));
                            break;
                        }
                            
                    }
                }

                layoutCounter = 0;
            }

            Console.WriteLine();
            Console.WriteLine(String.Format("Chains processed {0}", chainIndex+1));
            Console.WriteLine(String.Format("Layouts generated {0}", layoutsTotal));
            Console.WriteLine(String.Format("Solutions generated {0}", Solutions.Count));
            Console.WriteLine(String.Format("Backtracked {0}", backtrackCounter));

            trialsTotal++;
        }

        Console.WriteLine();
        Console.WriteLine(String.Format("Resets {0}", trialsTotal));

        if (Solutions.Count == 0)
            return false;

        return true;
    } 

    private IEnumerable<Layout> GenerateLayoutsForChain(Chain chain, Layout previousLayout) {
        int chainVerticesVisited = 0;

        // First chain so the first room will be fixed in place as an anchor
        //
        Layout bestLayout = new Layout(previousLayout, Graph);

        // Otherwise jump straight to our broken perturbation :p
        // 
        if (previousLayout.Depth < chain.Depth) {
            // Each layout needs to keep track of it's chain depth so we know 
            // how many chains a layout contains -- required for backtracking in calling function
            //
            // TODO: Can we just use room count instead of requiring a 'depth' mechanism?
            //
            bestLayout.Depth = chain.Depth;

            if (previousLayout.Rooms.Count == 0) {
                _visited.Add(chain.Vertices[0]);

                bestLayout.Rooms.Add(
                    chain.Vertices[0], 
                    RoomTemplateFactory.VendRandomTemplate(
                        chain.Vertices[0].Id, 
                        chain.Vertices[0].Definition.Type));

                chainVerticesVisited++;
            }

            Random rnd = new Random();

            while (chainVerticesVisited < chain.Vertices.Count) {
                List<Vertex> candidateVertices = new List<Vertex>();
                foreach (Vertex v in chain.Vertices) 
                    if (!_visited.Contains(v))
                        candidateVertices.Add(v);

                if (candidateVertices.Count == 0)
                    continue;
                    
                Vertex vertex = candidateVertices[rnd.Next(0, candidateVertices.Count)];

                List<Room> neighbours = new List<Room>();
                foreach (Vertex neighbour in Graph.GetNeighbours(vertex)) {
                    // Goes without saying we need rooms already laid out
                    //
                    if (_visited.Contains(neighbour))
                        neighbours.Add(bestLayout.Rooms[neighbour]);
                }

                if (neighbours.Count == 0)
                    continue;

                // Grab config space making sure to shuffle neighbours for additional randomness 
                //
                Room room = RoomTemplateFactory.VendRandomTemplate(vertex.Id, vertex.Definition.Type);
                if (!_csBuilder.TryFromRooms(neighbours.Shuffle(), room, out ConfigSpace cspace))
                    continue;

                float e = 1e10f;
                Vector2F bestSample = new Vector2F(0, 0);
                
                cspace.RandomlySample(out Vector2F[] samples);
                Layout candidate = null;

                foreach (Vector2F point in samples) {
                    Room roomTmp = new Room(room);
                    roomTmp.Translate(point);

                    Layout tmpLayout = new Layout(bestLayout, Graph);
                    tmpLayout.Rooms.Add(vertex, roomTmp);

                    // Compute energy for room update
                    //
                    tmpLayout.Update(vertex, roomTmp);

                    if (tmpLayout.Energy.E < e) {
                        e = tmpLayout.Energy.E;
                        
                        candidate = tmpLayout;
                    }
                }

                bestLayout = candidate;

                _visited.Add(vertex);

                chainVerticesVisited++; 
            }

            Console.WriteLine();
            Console.WriteLine(String.Format("Chain {0}", bestLayout.Depth+1));
            Console.WriteLine(String.Format("Best guess layout energy {0}", bestLayout.Energy));
        }
        
        return RunAnnealingOnLayout(bestLayout, chain);
    }

    private IEnumerable<Layout> RunAnnealingOnLayout(Layout bestLayout, Chain chain) {
        // We're going to lower the temp every cycle
        //
        // TODO: I don't understand the math behind the temp incrementor (frac)
        //
        float t1 = -1.0f/(float)Math.Log(Config.AcceptProbabilityStart);
        float t0 = -1.0f/(float)Math.Log(Config.AcceptProbabilityEnd);
        float frac = (float)Math.Pow((t0 / t1), 1.0f / (float)(Config.MaxCycles - 1.0f));
        float temp = t1;       

        Console.WriteLine(String.Format("Starting prob of accept {0}", t1));
        Console.WriteLine(String.Format("Ending prob of accept {0}", t0));
        Console.WriteLine(String.Format("Fractional increase in temp {0}", frac));

        int acceptedLayouts = 1;
        int totalLayouts = 1;
        int failures = 0;

        float deltaEnergy = 0f;
        float deltaEnergyAbs = 0f;
        float deltaEnergyAvg = 0f;
        float probability = 0f;
        float bestEnergy = 1e10f;
        float worstEnergy = 0f;

        Random rnd2 = new Random();
    
        for (int i=0; i<Config.MaxCycles; i++) {
            bool wasAccepted = false;

            for (int j=0; j<Config.MaxTrialsPerCycle; j++) {
                // Note: We could yield return our best guess layout first if it's
                //       valid, but in case of lucky first layouts (high chance) we want
                //       to avoid this initially whilst we test this function
                //
                // BUG: This just doesn't seem to work well at all
                //
                if (!TryPerturbRandomRoom(bestLayout, chain, out Layout newLayout))
                    continue;

                bool accepted = false;

                float eTmp = newLayout.Energy.E;
                float bestETmp = bestLayout.Energy.E;

                if (newLayout.Valid) {
                    // TODO: We should be checking that the layouts are significantly different
                    //       but need to review RoomLayout.AreSignificantlyDifferent(bestLayout, newLayout)
                    //
                    yield return newLayout;
                }

                // Delta energy
                //
                deltaEnergy = eTmp - bestETmp;
                deltaEnergyAbs = Math.Abs(deltaEnergy);

                // Acceptance criteria #1: Lower energy
                //
                if (eTmp < bestETmp) {
                    accepted = true;
                }
                // Acceptance criteria #2: Rnd number < Boltzman probability 
                //
                else {
                    // Boltzman constant k (avg of delta energy)
                    //
                    if (i == 0 && j == 0) 
                        deltaEnergyAvg = deltaEnergyAbs * 35f;

                    // Boltzman probability
                    //
                    probability = (float)Math.Pow(Math.E, -deltaEnergy / (deltaEnergyAvg * temp));

                    if (rnd2.NextDouble() < probability)
                        accepted = true;
                }

                if (accepted) {
                    if (deltaEnergyAbs != 0.0f) {
                        acceptedLayouts++;
                        deltaEnergyAvg = (float)(deltaEnergyAvg * ((float)acceptedLayouts - 1.0f) + deltaEnergyAbs) / (float)acceptedLayouts;
                    }

                    bestLayout = newLayout;
                    
                    // For stats only
                    //
                    bestEnergy = bestLayout.Energy.E < bestEnergy ? bestLayout.Energy.E : bestEnergy;
                    worstEnergy = bestLayout.Energy.E > worstEnergy ? bestLayout.Energy.E : worstEnergy;

                    wasAccepted = true;
                }

                totalLayouts++;
            }

            if (!wasAccepted)
                failures++;

            temp *= frac;
        }

        Console.WriteLine(String.Format("Temperature at finish {0}", temp));
        Console.WriteLine(String.Format("Annealing accepted layouts {0} of {1} ", acceptedLayouts, totalLayouts));
        Console.WriteLine(String.Format("Failed cycles {0}", failures));
        Console.WriteLine(String.Format("Lowest accepted energy layout {0}", bestEnergy));
        Console.WriteLine(String.Format("Highest accepted energy layout {0}", worstEnergy));
    }

    private bool TryPerturbRandomRoom(Layout layout, Chain chain, out Layout newLayout) {
        newLayout = new Layout(layout, Graph);

        List<(Vertex, Room)> candidates = new List<(Vertex, Room)>();

        // We will select cadidates only from the current chain and non-zero energy
        // rooms from the layout
        //
        foreach (KeyValuePair<Vertex, Room> kvp in newLayout.Rooms) {
            Vertex v = kvp.Key;
            Room r = kvp.Value;

            // Any vertex in the chain
            //
            if (chain.Vertices.Contains(v)) {
                candidates.Add((v, r));
            } 
            // As well as high energy rooms
            //
            else if (newLayout.TryGetRoomEnergy(v, r, out Energy roomE)) {
                if (roomE.Collision > Config.Tolerance || roomE.Connectivity > Config.Tolerance)
                    candidates.Add((v, r));
            }
        }

        Random rnd = new Random();
        int k = rnd.Next(0, candidates.Count);

        Vertex vertex = candidates[k].Item1;
        Room room = candidates[k].Item2;

        rnd = new Random();
        double x = rnd.NextDouble();

        Room roomTemplate = new Room(room);

        // less than 30% of the time we perturb the shape
        //
        bool perturbShape = x >= 0.7f;

        if (perturbShape)
            roomTemplate = RoomTemplateFactory.VendRandomTemplate(
                vertex.Id, 
                vertex.Definition.Type, 
                roomTemplate);

        if (!_csBuilder.TryFromRooms(
            newLayout.GetRoomsForVertices(Graph.GetNeighbours(vertex)), 
            roomTemplate, 
            out ConfigSpace cspace))
            // Better luck in the next trial
            //
            return false;

        roomTemplate.Translate(cspace.RandomlySample() - roomTemplate.GetCenter());

        // Update the layout with the new room
        //
        newLayout.Rooms[vertex] = roomTemplate;

        // Computes energy based on the room update
        //
        newLayout.Update(vertex, roomTemplate);

        return true;
    }

    private void InstallDoorsForLayout(Layout layout) {
        HashSet<int> visitedEdges = new HashSet<int>();

        Console.WriteLine();

        for (int i=0; i<layout.Rooms.Count; i++) {
            KeyValuePair<Vertex, Room> kvp = layout.Rooms.ElementAt(i);
            Vertex v = kvp.Key;
            Room r = kvp.Value;

            List<IEdge<Vertex>> neighbours = Graph.GetNeighbouringEdges(v).ToList();
            for (int j=0; j<neighbours.Count; j++) {
                IEdge<Vertex> edge = neighbours[j];

                Vertex vN = edge.Target;
                // We can be source or target on this edge since it's an undirected graph
                //
                if (edge.Target == v) {
                    vN = edge.Source; 
                }

                Room rN = layout.Rooms[vN];

                int pairKey = Layout.GetUniqueRoomPairId(r.Number, rN.Number);
                if (visitedEdges.Contains(pairKey))
                    continue;

                visitedEdges.Add(pairKey);

                Console.WriteLine();
                Console.WriteLine(String.Format(
                    "Installing door for pair {0}, room: {1}, neighbour: {2}",
                    pairKey,
                    r.Number,
                    rN.Number));

                // Since we're opting for immutable (shape) rooms this function
                // will return references to new rooms rather than mutating
                //
                InstallDoors(ref r, ref rN, v, vN, edge);

                // Replace with references to new rooms
                //
                layout.Rooms[vN] = rN;
                layout.Rooms[v] = r;
            }
        }
    }

    private void InstallDoors(ref Room r1, ref Room r2, Vertex v1, Vertex v2, IEdge<Vertex> edge) {
        foreach (BoundaryLine l1 in r1.Boundary) {
            if (!l1.IsDoorPlaceholder || l1.IsDoor)
                continue;

            foreach (BoundaryLine l2 in r2.Boundary) {
                if (!l2.IsDoorPlaceholder || l2.IsDoor)
                    continue;

                if (!Line.TryGetOverlappingLine(l1, l2, out Line overlap))
                    continue;
                    
                if (Vector2F.Magnitude(overlap.GetDirection()) < 
                    ((Config.DoorWidth - Config.Tolerance) + (Config.DoorToCornerMinGap * 2)))
                    continue;
                    
                Console.WriteLine();
                Console.WriteLine("Overlap: {0}", overlap);
                Console.WriteLine("Line 1: {0}", l1);
                Console.WriteLine("Line 2: {0}", l2);

                r1 = InstallDoor(r1, r2, v1, edge, l1, overlap);
                r2 = InstallDoor(r2, r1, v2, edge, l2, overlap);

                return;
            }
        }
    }

    private Room InstallDoor(Room room, Room connectingRoom, Vertex vertex, IEdge<Vertex> edge, Line boundaryLine, Line overlap) {
        Vector2F[] doorPoints = new Vector2F[2];

        float m1 = Vector2F.Magnitude2(overlap.Start - boundaryLine.Start);
        float m2 = Vector2F.Magnitude2(overlap.End - boundaryLine.Start);

        Vector2F p1 = m1 < m2 ? overlap.Start : overlap.End;
        Vector2F p2 = m1 < m2 ? overlap.End : overlap.Start;

        // We will find the center of the overlapping line and generate points 
        // either side based on the width of the door
        //
        // (𝐴𝑥+𝑡(𝐵𝑥−𝐴𝑥),𝐴𝑦+𝑡(𝐵𝑦−𝐴𝑦)) where t is between 0 and 1, i.e. %
        //
        Vector2F center = p1 + ((p2 - p1) * 0.5f);

        Vector2F offset = Vector2F.Normalize(p2 - p1) * (Config.DoorWidth/2);

        Vector2F c1 = center - offset;
        Vector2F c2 = center + offset;

        Line doorLine = new Line(c1, c2);

        Console.WriteLine();
        Console.WriteLine("Room: {0}", room.Number);
        Console.WriteLine("Door line: {0}", doorLine);
        Console.WriteLine("Wall: {0}", boundaryLine);

        List<Vector2F> newPolygon = 
            MergeLineIntoBoundary(new List<Vector2F>(room.Points), doorLine, boundaryLine);

        // Taking the semi-immutable approach of not allowing mutation of a rooms shape
        // but allowing translation. So due to the shape mutation we opt here to create
        // a new blueprint and generate a new room 
        //
        RoomBlueprint rBP = new RoomBlueprint(
            points: newPolygon, 
            doors: room.Blueprint.DoorConstraint);

        // Construct a new room
        //
        Room tmp = new Room(rBP, room.Position, room.Type, room.Number);

        // Place existing doors
        //
        foreach(Door door in room.Doors)
            tmp.Doors.Add(door);

        // Our graph will keep track of special features, in this case it tracks direction
        // even though our underlying graph is undirected.
        //
        Direction dir = Graph.GetCorridorDirectionForRooms(edge.Source.Id, edge.Target.Id);

        // If Edge is uni-directional and this is the target room then it's a 1-way door
        // and inacessible from this room
        //
        DefaultDoorAccess defaultDoorAccess = DefaultDoorAccess.Accessible;
        if (vertex == edge.Target && dir == Direction.Uni)
            defaultDoorAccess = DefaultDoorAccess.Inaccessible;

        // Add new door
        //
        tmp.Doors.Add(
            new Door(
                ValueTuple.Create<Vector2F, Vector2F>(doorLine.Start, doorLine.End),
                connectingRoom,
                defaultDoorAccess));

       return tmp;
    }

    private List<Vector2F> MergeLineIntoBoundary(List<Vector2F> points, Line doorLine, Line containerLine) {
        List<Vector2F> newPoints = new List<Vector2F>(points);

        for (int i=0; i<newPoints.Count; i++) {
            int end = i;
            int start = i-1;

            if (start == -1)
                start = newPoints.Count-1;

            // Looking for points for the wall in question
            //
            if (containerLine.Start == newPoints[start] && containerLine.End == newPoints[end]) {
                // Insert door points before line end
                //
                newPoints.Insert(end, doorLine.End);
                newPoints.Insert(end, doorLine.Start);
            }   
        }

        return newPoints;
    }

    public DungeonLayout Vend() {
        Random rnd = new Random();
        int index = rnd.Next(0, Dungeons.Count);

        return Dungeons[index];
    }
}