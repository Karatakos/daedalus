namespace Dungen;

public class BoundaryLine : Line {
    public bool IsDoorPlaceholder { get; private set; }

    public bool IsDoor { get; private set; }

    public BoundaryLine (Vector2F start, Vector2F end, bool canPutDoor = true, bool isDoor = false) : base(start, end) {
        IsDoorPlaceholder = canPutDoor;
        IsDoor = isDoor;
    }
    public BoundaryLine (Vector2F start, bool canPutDoor = true, bool isDoor = false) : base(start) {
        IsDoorPlaceholder = canPutDoor;
        IsDoor = isDoor;
    }
    public BoundaryLine (BoundaryLine copy) : base(copy) {
        IsDoorPlaceholder = copy.IsDoorPlaceholder;
    }
}

public class Line {
    public Vector2F Start { get; set; }
    
    public Vector2F End { get; set; }

    public Line(Vector2F start, Vector2F end) {
        Start = new Vector2F(start);
        End = new Vector2F(end);
    }

    public Line(Vector2F start) {
        Start = start;
        End = start;
    }

    public Line(Line copy) {
        Start = new Vector2F(copy.Start);
        End = new Vector2F(copy.End);
    }

    public Vector2F GetDirection() {
        return End - Start;
    }

    public float GetLength() {
        return Vector2F.Magnitude(Start - End);
    }

    public float GetLength2() {
        return Vector2F.Magnitude2(Start - End);
    }

    public void Translate(Vector2F v) {
        Start = Start + v;
        End = End + v;
    }

    public void Scale(float s) {
        Start = Start * s;
        End = End * s;
    }

    public override string ToString()
    {
        return String.Format("P: ({0}) Q: ({1})", Start, End);
    }

    public static bool TryGetOverlappingLine(Line l1, Line l2, out Line contact) {
        // Handle parallel (potentially colinear) lines
        //
        //
        Vector2F l1Min = Vector2F.MinUnion(l1.Start, l1.End);
        Vector2F l1Max = Vector2F.MaxUnion(l1.Start, l1.End);
        Vector2F l2Min = Vector2F.MinUnion(l2.Start, l2.End);
        Vector2F l2Max = Vector2F.MaxUnion(l2.Start, l2.End);

        contact = null;

        // Self evident but we ignore if they're not overlapping
        //
        if ((l1Max.x < l2Min.x - Config.Tolerance || l1Min.x > l2Max.x + Config.Tolerance) ||
            (l1Max.y < l2Min.y - Config.Tolerance || l1Min.y > l2Max.y + Config.Tolerance))
            return false;

        // They're overlapping but are they colinear? If not then ignore
        //
        if (Math2D.PointToLineSqDistance(l2.Start, l1) > Config.Tolerance ||
            Math2D.PointToLineSqDistance(l2.End, l1) > Config.Tolerance)
            return false;

        // Colinear and overlapping lines - so extract the overlapping line
        //
        // BUG: Might need to min/max x/y dimensions seperately? :/
        //
        Vector2F p1 = Vector2F.MaxUnion(Vector2F.MinUnion(l1.Start, l1.End), Vector2F.MinUnion(l2.Start, l2.End));
        Vector2F p2 = Vector2F.MinUnion(Vector2F.MaxUnion(l1.Start, l1.End), Vector2F.MaxUnion(l2.Start, l2.End));

        contact = new Line(p1, p2);

        return true;
    }

    public static List<Line> Union(List<Line> lines) {
        List<Line> union = new List<Line>();

        foreach (Line newLine in lines.OrderBy(x => x.GetLength2()).ToList()) {
            float newLineLenSq = newLine.GetLength2();

            // The goal is to drop duplicate lines and merge lines (with below constraints)
            //      1. Are colinear
            //      2. Are touching (or within tolerance)
            //
            bool include = true;
            foreach (Line existingLine in union) {
                float existingLineLenSq = existingLine.GetLength2();

                // Ignore duplicates, e.g. opposite direction
                //
                if ((newLine.Start == existingLine.Start && newLine.End == existingLine.End) ||
                    (newLine.Start == existingLine.End && newLine.End == existingLine.Start)) {
                    include = false;
                    break;
                }

                // Angle must be 0, i.e. segments on the same line
                //
                float cross = Vector2F.CrossProductMagnitude(existingLine.GetDirection(), newLine.GetDirection());
                if (Math.Abs(cross) > Config.Tolerance)
                    continue;

                // Identify distance between line start/end and the new line
                //
                float ds = Math2D.PointToLineSegmentSqDistance(existingLine.Start, newLine);
                float de = Math2D.PointToLineSegmentSqDistance(existingLine.End, newLine);

                // If points touch along the line we're now ready for a merge
                //
                if (ds < Config.Tolerance || de < Config.Tolerance)
                {
                    //Console.WriteLine(String.Format("Existing line: {0}", existingLine));
                    //Console.WriteLine(String.Format("New line 2: {0}", newLine));

                    Vector2F posMin1 = Vector2F.MinUnion(existingLine.Start, existingLine.End);
                    Vector2F posMax1 = Vector2F.MaxUnion(existingLine.Start, existingLine.End);
                    Vector2F posMin2 = Vector2F.MinUnion(newLine.Start, newLine.End);
                    Vector2F posMax2 = Vector2F.MaxUnion(newLine.Start, newLine.End);

                    Vector2F posMin = Vector2F.MinUnion(posMin1, posMin2);
                    Vector2F posMax = Vector2F.MaxUnion(posMax1, posMax2);

                    Vector2F pos1, pos2;
                    if (newLine.Start.x == posMin1.x) {
                        pos1 = posMin;   
                        pos2 = posMax;
                    }
                    else {
                        pos1 = posMax;   
                        pos2 = posMin;
                    }

                    existingLine.Start = pos1;
                    existingLine.End = pos2;

                    //Console.WriteLine(String.Format("New line: {0}", existingLine));

                    include = false;
                    break;
                }

                //Console.WriteLine(String.Format("WTF ds: {0} de: {1}", ds, de));
            }

            if (include)
                union.Add(newLine);
        }

        return union;
    }

    public static float ComputeLineContactArea(Line l1, Line l2) {
        // Ignore if not parallel
        //
        float l1xl2Mag2 = Vector2F.CrossProductMagnitude(l1.GetDirection(), l2.GetDirection());
        if (Math.Abs(l1xl2Mag2) > Config.Tolerance)
            return 0F;

        Vector2F l1Min = Vector2F.MinUnion(l1.Start, l1.End);
        Vector2F l1Max = Vector2F.MaxUnion(l1.Start, l1.End);
        Vector2F l2Min = Vector2F.MinUnion(l2.Start, l2.End);
        Vector2F l2Max = Vector2F.MaxUnion(l2.Start, l2.End);

        // Self evident but we ignore if they're not overlapping
        //
        if ((l1Max.x < l2Min.x - Config.Tolerance || l1Min.x > l2Max.x + Config.Tolerance) ||
            (l1Max.y < l2Min.y - Config.Tolerance || l1Min.y > l2Max.y + Config.Tolerance))
            return 0F;

        // They're overlapping but are they colinear? 
        //
        float d1 = Math2D.PointToLineSqDistance(l2.Start, l1);
        float d2 = Math2D.PointToLineSqDistance(l2.End, l1);
        if (d1 > Config.Tolerance || d2 > Config.Tolerance)
            return 0F;

        Vector2F p1 = Vector2F.MaxUnion(Vector2F.MinUnion(l1.Start, l1.End), Vector2F.MinUnion(l2.Start, l2.End));
        Vector2F p2 = Vector2F.MinUnion(Vector2F.MaxUnion(l1.Start, l1.End), Vector2F.MaxUnion(l2.Start, l2.End));

        Line line = new Line(p1, p2);

        float contactArea = Vector2F.Magnitude(line.GetDirection());

        // https://github.com/chongyangma/LevelSyn/blob/4db9b14dfa31752cbf7d0a48f64d66366a0a51ef/src/LevelMath.cpp#L141
        //
        // Note: Not using as the math is complicated and the output is the same as the above...
        //
        /*
        V2F l1Dir = l1.GetDirection();
        V2F l2Dir = l2.GetDirection();

        float len1 = V2F.Magnitude(l1Dir);
        float len2 = V2F.Magnitude(l2Dir);

        float d11 = V2F.Magnitude2(l1.Start - l2.Start);
        float d21 = V2F.Magnitude2(l1.End - l2.Start);
        float d12 = V2F.Magnitude2(l1.Start - l2.End);
        float d22 = V2F.Magnitude2(l1.End - l2.End);

        float dMax = (float)Math.Sqrt(Math.Max(Math.Max(d11, d21), Math.Max(d12, d22)));
        dMax = Math.Max(dMax, Math.Max(len1, len2));
        float contactArea = len1 + len2 - dMax;*/
        
        return contactArea;
    }
}
