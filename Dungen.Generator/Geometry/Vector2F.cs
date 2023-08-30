namespace Dungen.Generator;

public struct Vector2F {
    public Vector2F(float x, float y) {
        _x = x;
        _y = y;
        _sortingDot = 0;
    }

    public Vector2F(Vector2F v) {
        _x = v.x;
        _y = v.y;
        _sortingDot = v.SortingDot;
    }

    private float _x;

    private float _y;

    private float _sortingDot;

    public float x {
        get => _x;
        set {
            _x = value;
        }
    }

    public float y {
        get => _y;
        set {
            _y = value;
        }
    }

    public float SortingDot {
        get => _sortingDot;
        set {
            _sortingDot = value;
        }
    }

    public static bool operator== (Vector2F v1, Vector2F v2) {
        return (v1.x == v2.x && v1.y == v2.y);
    }

    public static bool operator!= (Vector2F v1, Vector2F v2) {
        return (v1.x != v2.x || v1.y != v2.y);
    }

    public static Vector2F operator- (Vector2F v1, Vector2F v2) {
        return new Vector2F(v1.x - v2.x, v1.y - v2.y);
    }

    public static Vector2F operator+ (Vector2F v1, Vector2F v2) {
        return new Vector2F(v1.x + v2.x, v1.y + v2.y);
    }

    public static Vector2F operator/ (Vector2F v, float s) {
        s = 1.0F / s;
        return new Vector2F(v.x * s, v.y * s);
    }

    public static Vector2F operator* (Vector2F v, float s) {
        return new Vector2F(v.x * s, v.y * s);
    }

    public static Vector2F operator* (Vector2F v1, Vector2F v2) {
        return new Vector2F(v1.x * v2.x, v1.y * v2.y);
    }

    public static Vector2F operator- (Vector2F v) {
        return new Vector2F(-v.x, -v.y);
    }

    public static float Dot(Vector2F v1, Vector2F v2) {
        return v1.x * v2.x + v1.y * v2.y;
    }

    /* Returns magnitude of what would be the Z dimenion of a 3D vector
    *
    */
    public static float CrossProductMagnitude(Vector2F v1, Vector2F v2)
    {
        return (v1.x * v2.y) - (v1.y * v2.x);
    }

    public static float Magnitude(Vector2F v) {
        return (float)Math.Sqrt(Magnitude2(v));
    }

    public static float Magnitude2(Vector2F v) {
        return v.x * v.x + v.y * v.y;
    }

    public static Vector2F Normalize(Vector2F v) {
        return v / Vector2F.Magnitude(v);
    }

    public static Vector2F MinUnion(Vector2F v1, Vector2F v2) {
        return new Vector2F(
            Math.Min(v1.x, v2.x),
            Math.Min(v1.y, v2.y));
    }

    public static Vector2F MaxUnion(Vector2F v1, Vector2F v2) {
        return new Vector2F(
            Math.Max(v1.x, v2.x),
            Math.Max(v1.y, v2.y));
    }

    public override string ToString()
    {
        return _x + " " + _y;
    }
}
