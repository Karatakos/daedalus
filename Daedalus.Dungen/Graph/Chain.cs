namespace Dungen;

public class Chain {
    public Chain(List<Vertex> vertices, int depth) { 
        Vertices = vertices;
        Depth = depth; 
    }

    public Chain(int depth) { 
        Depth = depth; 
        Vertices = new List<Vertex>();
    }

    public int Depth { get; set; }
    public List<Vertex> Vertices { get; set; }
}   