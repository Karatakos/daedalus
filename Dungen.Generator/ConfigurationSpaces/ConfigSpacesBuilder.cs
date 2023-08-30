namespace Dungen.Generator;

public class ConfigSpacesBuilder {
    private Dictionary<(int, int), ConfigSpace> _cache;
    public ConfigSpacesBuilder() {
         _cache = new Dictionary<(int, int), ConfigSpace>();
    }

    public void Precompute(Room[] roomTemplates) {
        _cache.Clear();

        for (int i=0; i<roomTemplates.Count(); i++) {
            Room r1 = new Room(roomTemplates[i]);
            for (int j=0; j<roomTemplates.Count(); j++) {
                if (i == j)
                    continue;

                Room r2 = new Room(roomTemplates[j]);

                // Cache
                //
                TryFromRoom(r1, r2, out ConfigSpace cspace);
            }   
        }
    }

    public bool TryFromRooms(List<Room> fixedRooms, Room freeRoom, out ConfigSpace cspace) {
        cspace = null;
        for (int i=0; i<fixedRooms.Count; i++) {
            // BUG:  Some shapes may actually mismatch, such as if a corridor isn't rotated 
            //       for the current shape. If we want to support this we should handle it here
            //       i.e reject freeRoom in which case the caller should choose a different shape
            //       or in the case of perturbation just try again
            //
            TryFromRoom(fixedRooms[i], freeRoom, out ConfigSpace tmp);

            tmp.Translate(fixedRooms[i].GetCenter() - tmp.GetCenter());
            
            if (cspace == null)
                cspace = tmp;
            else {
                // This is a relaxed algorithm. We don't fail if we can't get a 
                // a config space for all neighbors. The idea being that we get
                // closer to connecting all neighbors as we generate new layouts
                //
                ConfigSpace tmp2 = FromConfigSpace(cspace, tmp);
                if (tmp2 == null || tmp2.Lines.Count == 0)
                    break;

                cspace = tmp2;
            }
        }

        if (cspace != null && cspace.Lines.Count > 0)
            return true;

        return false;
    }

    public bool TryFromRoom(Room fixedRoom, Room movingRoom, out ConfigSpace configSpace) {
        configSpace = null;

        ConfigSpace cspace = null;
        List<Line> clines = new List<Line>();

        if (_cache.TryGetValue((fixedRoom.GetHashCode(), movingRoom.GetHashCode()), out ConfigSpace tmp)) {
            configSpace = new ConfigSpace(tmp);

            return true;
        }

        float contactThreshold = Config.DoorWidth + (Config.DoorToCornerMinGap * 2);

        foreach (BoundaryLine fline in fixedRoom.Boundary) {
            if (!fline.IsDoorPlaceholder)
                continue;

            foreach (BoundaryLine mline in movingRoom.Boundary) {
                if (!mline.IsDoorPlaceholder)
                    continue;

                float mag = Math.Abs(Vector2F.CrossProductMagnitude(fline.GetDirection(), mline.GetDirection()));

                // Checking lines are parallel
                // 
                if (mag > Config.Tolerance)
                    continue;

                Vector2F[] posTmp = new Vector2F[4];

                posTmp[0] = fline.Start - mline.Start;
                posTmp[1] = fline.Start - mline.End;
                posTmp[2] = fline.End - mline.Start;
                posTmp[3] = fline.End - mline.End;

                Math2D.ComputeDot(posTmp);
                Vector2F[] pos = posTmp.OrderBy(x => x.SortingDot).ToArray<Vector2F>(); 

                Vector2F[] testPoints = new Vector2F[3];
                for (int k=1; k<pos.Length; k++) {
                    testPoints[0] = pos[k];
                    testPoints[1] = pos[k - 1];

                    //  Ignore if the lines have the same magnitude
                    //
                    float mag2 = Vector2F.Magnitude2(testPoints[1] - testPoints[0]);
                    if (mag2 < Config.Tolerance)
                        continue;
        
                    // Sampling the start, end and middle (testPoints[2]) of the line
                    //
                    testPoints[2] = (testPoints[0] + testPoints[1]) * 0.5f;
                    
                    bool failedTest = false;
                    for (int j=0; j<testPoints.Count(); j++) {
                        // Test by moving a room into place
                        //
                        Room test = new Room(movingRoom);
                        test.Translate(testPoints[j]);

                        // Test for no overlap between rooms (constraint #1)
                        //
                        float area = Room.ComputeCollideArea(fixedRoom, test);
                        if (area > Config.Tolerance) {   
                            failedTest = true;
                            break;
                        }

                        if (j >= 2) 
                            continue;

                        // Test for no contact between rooms (constraint #2)
                        //
                        float contact = Room.ComputeContactArea(fixedRoom, test);
                        if (contact < contactThreshold - Config.Tolerance) {
                            // Get vectors direction from the center of the line.
                            // This will make sure the offset vector has the right sign
                            // i.e, the line is always shorted when we use minus operator
                            //
                            Vector2F norm = Vector2F.Normalize(testPoints[j]-testPoints[2]);
                            Vector2F offset = norm * (contactThreshold-contact);

                            // Update the point based on this new offset
                            //
                            testPoints[j] = testPoints[j] - offset;
                        }
                    }

                    if (failedTest) 
                        continue;

                    Vector2F clineStart = movingRoom.GetCenter() + testPoints[0];
                    Vector2F clineEnd = movingRoom.GetCenter() + testPoints[1];

                    Line cline = new Line(clineStart, clineEnd);
                    clines.Add(cline);
                }
            }
        }

        clines = Line.Union(clines);
        cspace = new ConfigSpace(fixedRoom, movingRoom, clines);

        if (cspace.Lines.Count == 0)
            // BUG: For corridors, restricted doors are necessary, and there WILL be shape missmatches
            //      We need to think about another solution such as marking shapes as incompatible
            //      so we can avoid mismatching shapes when selecting a room during layout. 
            //
            //      OR, don't support missmatching shapes and layout corridors differently to rooms
            //      Edit: not taking exploring this option as it's complex, and unless there is a need
            //            to make every single edge a corridor, it's not worth the effort building a new
            //            system to handle it. We choose to rework shape selection.
            //
            throw new Exception("No configuration space lines found! Be careful if you're using restricted doors. If so, make sure ALL shapes are compatible. It's likely you have two shapes that don't fit due to the restrictions you set on doors. Also, make sure the door lines are in the same direction as the walls you defined.");

        if (!_cache.ContainsKey((fixedRoom.GetHashCode(), movingRoom.GetHashCode())))
            _cache.Add((fixedRoom.GetHashCode(), movingRoom.GetHashCode()), cspace);

        configSpace = cspace;

        return true;
    }

    public ConfigSpace FromConfigSpace(ConfigSpace spaceA, ConfigSpace spaceB) {
        List<Line> clines = new List<Line>();
        foreach (Line l1 in spaceA.Lines) {
            foreach (Line l2 in spaceB.Lines) {
                // Points not lines 
                //
                if (l1.GetLength2() < Config.Tolerance && l2.GetLength2() < Config.Tolerance) {
                    // If points overlap then add either! 
                    //
                    if (Vector2F.Magnitude2(l1.Start - l2.Start) < Config.Tolerance)
                        clines.Add(l1);

                    continue;
                }

                // If l1 is the point then this is what we will add 
                //  
                if (l1.GetLength2() < Config.Tolerance) {
                    if (Math2D.PointToLineSegmentSqDistance(l1.Start, l2) < Config.Tolerance)
                        clines.Add(l1);

                    continue;
                }

                // Otherwise a last check in case l2 is a point (in which case we add that)
                //
                // TODO: Remove the duplication here
                //
                if (l2.GetLength2() < Config.Tolerance) {
                    if (Math2D.PointToLineSegmentSqDistance(l2.Start, l1) < Config.Tolerance)
                        clines.Add(l2);
                             
                    continue;
                }

                float l1xl2Mag2 = Math.Abs(Vector2F.CrossProductMagnitude(l1.GetDirection(), l2.GetDirection()));

                // Not parallel and so we use our standard line intersect algorithm to check 
                //
                if (l1xl2Mag2 > Config.Tolerance) {
                    // Handles non-colinear lines and returns intersecting point
                    //
                    if (Math2D.FindIntersection(l1.Start, l1.End, l2.Start, l2.End, out Vector2F intersect))
                        clines.Add(new Line(intersect));

                    continue;
                }
                
                // Handle parallel (potentially colinear) lines
                //
                // TODO: Currently the above ignores collinear lines but what if we handled them and 
                //       returned a line instead of a point? We could handle all cases with one call?
                //
                if (Line.TryGetOverlappingLine(l1, l2, out Line contact))
                    clines.Add(contact);
            }
        }

        List<Line> lines = Line.Union(clines);
        ConfigSpace intersection = new ConfigSpace(lines);

        return intersection;
    }
}