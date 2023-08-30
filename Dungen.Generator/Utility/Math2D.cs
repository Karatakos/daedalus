namespace Dungen.Generator;

public static class Math2D {
    public static float _scalefactor = 1e10F;

    public static long FloatToInt64(float value) {
        return (long)(value * _scalefactor + 0.5F);
    }

    public static float Int64ToFloat(Int64 value) {
        return (float)(value / _scalefactor); 
    }

    public static float DoubleToFloat(double value) {
        return (float)(value / (double)(_scalefactor * _scalefactor));
    }

    public static void ComputeDot(Vector2F[] vertices) {
        if (vertices.Length < 2)
            return;

        Vector2F pd = vertices[1] - vertices[0];
        for (int i=0; i<vertices.Length; i++)
            vertices[i].SortingDot = Vector2F.Dot(pd, vertices[i] - vertices[0]);
    }

    public static float PointToLineSegmentSqDistance(Vector2F p, Line line) {
        // If line is on a single point so return point distance
        //
        if (line.GetLength2() < Config.Tolerance * Config.Tolerance)
            return Vector2F.Magnitude2(p - line.Start);
        
        float d1 = Vector2F.Magnitude2(p - line.Start);
        float d2 = Vector2F.Magnitude2(p - line.End);

        Vector2F pe = line.End - line.Start;
        Vector2F pd = p - line.Start;

        float dp = Vector2F.Dot(pe, pd);
        float r = dp / Vector2F.Magnitude2(pe);

        float d;
        if (r >= 1F)
            d = d2;
        else if (r <= 0F)
            d = d1;
        else
        {
            Vector2F peNew = new Vector2F(pe.y, -pe.x);
            d = Math.Abs(Vector2F.Dot(pd, peNew) / Vector2F.Magnitude(peNew));
            d = d * d;
        }

        return d;
    }

    public static float PointToLineSqDistance(Vector2F p, Line line)
    {
        Vector2F pe = line.GetDirection();
        Vector2F peNorm = Vector2F.Normalize(pe);
        Vector2F pr = p - line.Start;

        float cross = Vector2F.CrossProductMagnitude(peNorm, pr);
        
        return cross * cross;
    }

    // t = (q − p) × s / (r × s)
    // u = (q − p) × r / (r × s)
    //
    // https://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect
    //
    public static bool FindIntersection(Vector2F p, Vector2F p2, Vector2F q, Vector2F q2, out Vector2F intersect) {
        intersect = new Vector2F();

        Vector2F r = p2 - p;
        Vector2F s = q2 - q;

        float rxs = Vector2F.CrossProductMagnitude(r, s);
        float qmpxr = Vector2F.CrossProductMagnitude((q - p), r);
        float qmpxs = Vector2F.CrossProductMagnitude((q - p), s);

        if (rxs == 0) 
            return false;

        float t = qmpxs / rxs;
        float u = qmpxr / rxs;

        if ((0 <= t && t <= 1) && (0 <= u && u <= 1)) {
            intersect = p + (r * t);

            return true;
        }

        return false;
    }
}