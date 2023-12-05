namespace Dungen;

public class Vertex : IEquatable<Vertex>  {
    public int Id;

    public RoomDefinition Definition { get; set; }

    public Vertex(int id) { 
        Id = id; 
    }

    public Vertex(int id, RoomDefinition definition) { 
        Id = id; 
        Definition = definition;
    }

    public override string ToString()
    {
        return String.Format("V{0}", Id.ToString());
    }

    public override bool Equals(object obj)
    {
        var vertex = obj as Vertex;

        return vertex != null && Vertex.Equals(Id, vertex.Id);
    }

    public static bool operator ==(Vertex vertex1, Vertex vertex2)
    {
        return EqualityComparer<Vertex>.Default.Equals(vertex1, vertex2);
    }

    public static bool operator !=(Vertex vertex1, Vertex vertex2)
    {
        return !(vertex1 == vertex2);
    }

    public bool Equals(Vertex other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        
        return Vertex.Equals(Id, other.Id);
    }
}