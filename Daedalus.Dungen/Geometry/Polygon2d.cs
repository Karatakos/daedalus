namespace Dungen;

using ClipperLib;

public abstract class Polygon2d {
    private int _hash;
    
    private bool _isDirty;

    private List<Line> _boundary;

    public List<Vector2F> Points { get; private set; }

    public List<Line> Boundary { 
        get {
            if (_isDirty) {
                // Lazily regenerate the boundary in case of any change 
                //
                // TODO: Perhaps there is a more efficient method
                //
                GenerateBoundary();

                _isDirty = false;
            }
            return _boundary;
        } 

        protected set => _boundary = value;
    }

    public Polygon2d(List<Vector2F> points) {
        Points = points;
        
        GenerateHash();

        // Used to flag as changed if a translation happens and boundary needs updating
        //
        _isDirty = true;
    }

    public Polygon2d(Polygon2d copy) {
        Points = new List<Vector2F>();
        foreach (Vector2F p in copy.Points)
            Points.Add(new Vector2F(p));

        // Maintain riginal shape's hash pre-translation
        //
        _hash = copy._hash;

        // We can't generate lines here as it's a virtual function
        //
        _isDirty = true;
    }

    public override bool Equals(object obj)
    {
        return obj is Polygon2d other && Points.SequenceEqual(other.Points);
    }

    public override int GetHashCode()
    {
        return _hash;
    }

    public override string ToString()
    {
        return String.Format("{0}", string.Join(",", Points.Select(x => x.ToString())));
    }

    public Vector2F GetCenter() {
        Vector2F min = new Vector2F(100000, 100000);
        Vector2F max = new Vector2F(-100000, -10000);

        foreach (Vector2F p in Points) {
            min = Vector2F.MinUnion(min, p);
            max = Vector2F.MaxUnion(max, p);
        }

        return (min + max) * 0.5F;
    }

    public AABB2F GetBoundingBox() {
        Vector2F min = new Vector2F(100000, 100000);
        Vector2F max = new Vector2F(-100000, -10000);

        foreach (Vector2F p in Points) {
            min = Vector2F.MinUnion(min, p);
            max = Vector2F.MaxUnion(max, p);
        }

        return new AABB2F(min, max);
    }

    public virtual void Translate(Vector2F v) {
        for (int i=0; i<Points.Count; i++) 
            Points[i] = Points[i] + v;      // TODO: Overload +/-=

        _isDirty = true;
    }

    protected abstract void GenerateBoundary();

    private void GenerateHash() {
        _hash = 17;
		Points.ForEach(x => _hash = _hash * 23 + (int)x.x + (int)x.y);
    }

    public static float ComputeContactArea(Polygon2d shape1, Polygon2d shape2) {
        float contact = 0f;

        foreach (Line l1 in shape1.Boundary) {
            foreach (Line l2 in shape2.Boundary) {
                contact += Line.ComputeLineContactArea(l1, l2);
            }
        }

        return contact;
    }

    public static float ComputeCollideArea(Polygon2d fixedShape, Polygon2d movingShape) {
        List<IntPoint> fixedShapePoints = new List<IntPoint>();
        foreach (Vector2F v in fixedShape.Points) 
            fixedShapePoints.Add(new IntPoint(Math2D.FloatToInt64(v.x), Math2D.FloatToInt64(v.y)));

        List<IntPoint> movingShapePoints = new List<IntPoint>();
        foreach (Vector2F v in movingShape.Points) {
            //Console.WriteLine(String.Format("Clip x: {0} clip y: {1}", 
            //  MathUtil.FloatToInt64(v.x), MathUtil.FloatToInt64(v.y)));
            movingShapePoints.Add(new IntPoint(Math2D.FloatToInt64(v.x), Math2D.FloatToInt64(v.y)));
        }
        
        Clipper clip = new Clipper();

        clip.AddPath(fixedShapePoints, PolyType.ptSubject, true);
        clip.AddPath(movingShapePoints, PolyType.ptClip, true);

        List<List<IntPoint>> solution = new List<List<IntPoint>>();
        clip.Execute(ClipType.ctIntersection, solution, PolyFillType.pftNonZero);

        float collideArea = 0f;
        //Console.WriteLine(String.Format("Clip solutions: {0}", solution.Count));
        for (int i=0; i<solution.Count; i++) {
            //Console.WriteLine(Clipper.Area(solution[i]));
            collideArea += Math.Abs(Math2D.DoubleToFloat(Clipper.Area(solution[i])));
        }

        if (collideArea < Config.Tolerance){
            collideArea = 0f;
        }

        return collideArea;
    }
}

public struct AABB2F {
    public AABB2F(Vector2F min, Vector2F max) {
        Min = min;
        Max = max;
    }

    public Vector2F Min { get; set; }
    public Vector2F Max { get; set; }
}