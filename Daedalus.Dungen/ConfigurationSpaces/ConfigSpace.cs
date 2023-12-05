namespace Dungen;

public class ConfigSpace {
    public Polygon2d FixedShapeType { get; set; }
    public Polygon2d MovingShapeType { get; set; }

    public List<Line> Lines { get; set; }

    public ConfigSpace(Polygon2d fixedShapeType, Polygon2d movingShapeType, List<Line> clines) {
        FixedShapeType = fixedShapeType;
        MovingShapeType = movingShapeType;
        Lines = clines;
    }

    public ConfigSpace(List<Line> clines) : this(null, null, clines) {}

    public ConfigSpace(ConfigSpace source) {
        Lines = new List<Line>();
        foreach (Line l in source.Lines) 
            Lines.Add(new Line(l));
    }

    public ConfigSpace() {
        Lines = new List<Line>();
    }

    public Vector2F GetCenter() {
        Vector2F min = new Vector2F(100000, 100000);
        Vector2F max = new Vector2F(-100000, -10000);

        foreach (Line l in Lines) {
            min = Vector2F.MinUnion(min, l.Start);
            max = Vector2F.MaxUnion(max, l.Start);

            min = Vector2F.MinUnion(min, l.End);
            max = Vector2F.MaxUnion(max, l.End);
        }

        return (min + max) * 0.5F;
    }

    public void Translate(Vector2F t) {
        foreach (Line l in Lines) 
            l.Translate(t);
    }

    public void RandomlySample(out Vector2F[] samples, bool discrete = false) {
        Random rnd = new Random();

        // Sampling in multiples of 1/10th of a line
        //
        float t = (float)Math.Round(rnd.NextDouble(), 1);

        samples = new Vector2F[Lines.Count];
        for (int i=0; i<Lines.Count; i++) {
            Vector2F p = t < 0.5f ? Lines[i].Start : Lines[i].End;

            if (!discrete) {
                // (𝐴𝑥+𝑡(𝐵𝑥−𝐴𝑥),𝐴𝑦+𝑡(𝐵𝑦−𝐴𝑦)) where t is between 0 and 1, i.e. %
                //
                samples[i] = Lines[i].Start + (Lines[i].GetDirection() * t);
            }
        }
    } 

    public Vector2F RandomlySample(bool discrete = false) {
        Random rnd = new Random();

        RandomlySample(out Vector2F[] samples);

        if (samples.Length == 0)
            throw new Exception("Cannot get sample from config space.");

        // Randomly pick one for caller convience
        //
        int rl = rnd.Next(0, Lines.Count-1);

        return samples[rl];
    } 
}