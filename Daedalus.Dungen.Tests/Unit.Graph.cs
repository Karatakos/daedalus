namespace Dungen.Generator.Tests;

using NUnit.Framework;

using Dungen;

using GraphPlanarityTesting.Graphs.DataStructures;
using GraphPlanarityTesting.PlanarityTesting.BoyerMyrvold;

[TestFixture]
public class Graph
{
    private UndirectedAdjacencyListGraph<Vertex> _planarGraph;
    private UndirectedAdjacencyListGraph<Vertex> _planarGraphWTF;
    private UndirectedAdjacencyListGraph<int> _planarGraphSimple;
    private UndirectedAdjacencyListGraph<int> _nonPlanarGraphSimple;

    [SetUp]
    public void Setup()
    {
        /*
        *   1--2
        */
        _planarGraphWTF = new UndirectedAdjacencyListGraph<Vertex>();

        Vertex wtf0 = new Vertex(0); _planarGraphWTF.AddVertex(wtf0);
        Vertex wtf1 = new Vertex(1); _planarGraphWTF.AddVertex(wtf1);

        _planarGraphWTF.AddEdge(wtf0, wtf1);

        /*
        *   1--2--3--4--5
        *      |  |  |  |
        *      8  7--6--
        */
        _planarGraph = new UndirectedAdjacencyListGraph<Vertex>();

        Vertex v1 = new Vertex(1); _planarGraph.AddVertex(v1);
        Vertex v2 = new Vertex(2); _planarGraph.AddVertex(v2);
        Vertex v3 = new Vertex(3); _planarGraph.AddVertex(v3);
        Vertex v4 = new Vertex(4); _planarGraph.AddVertex(v4);
        Vertex v5 = new Vertex(5); _planarGraph.AddVertex(v5);
        Vertex v6 = new Vertex(6); _planarGraph.AddVertex(v6);
        Vertex v7 = new Vertex(7); _planarGraph.AddVertex(v7);
        Vertex v8 = new Vertex(8); _planarGraph.AddVertex(v8);

        _planarGraph.AddEdge(v1, v2);
        _planarGraph.AddEdge(v2, v3);
        _planarGraph.AddEdge(v3, v4);
        _planarGraph.AddEdge(v4, v5);
        _planarGraph.AddEdge(v4, v6);
        _planarGraph.AddEdge(v6, v7);
        _planarGraph.AddEdge(v6, v5);
        _planarGraph.AddEdge(v7, v3);
        _planarGraph.AddEdge(v2, v8);

        /*
        *   1--2--3--4--5
        *         |  |  |
        *         7--6--
        */
        _planarGraphSimple = new UndirectedAdjacencyListGraph<int>();

        int i1 = 1; _planarGraphSimple.AddVertex(i1);
        int i2 = 2; _planarGraphSimple.AddVertex(i2);
        int i3 = 3; _planarGraphSimple.AddVertex(i3);
        int i4 = 4; _planarGraphSimple.AddVertex(i4);
        int i5 = 5; _planarGraphSimple.AddVertex(i5);
        int i6 = 6; _planarGraphSimple.AddVertex(i6);
        int i7 = 7; _planarGraphSimple.AddVertex(i7);

        _planarGraphSimple.AddEdge(i1, i2);
        _planarGraphSimple.AddEdge(i2, i3);
        _planarGraphSimple.AddEdge(i3, i4);
        _planarGraphSimple.AddEdge(i4, i5);
        _planarGraphSimple.AddEdge(i4, i6);
        _planarGraphSimple.AddEdge(i6, i7);
        _planarGraphSimple.AddEdge(i6, i5);
        _planarGraphSimple.AddEdge(i7, i3);

        /*
        *   https://www.javatpoint.com/planar-and-non-planar-graphs
        */
        _nonPlanarGraphSimple = new UndirectedAdjacencyListGraph<int>();

        int nv1 = 1; _nonPlanarGraphSimple.AddVertex(nv1);
        int nv2 = 2; _nonPlanarGraphSimple.AddVertex(nv2);
        int nv3 = 3; _nonPlanarGraphSimple.AddVertex(nv3);
        int nv4 = 4; _nonPlanarGraphSimple.AddVertex(nv4);
        int nv5 = 5; _nonPlanarGraphSimple.AddVertex(nv5);
        int nv6 = 6; _nonPlanarGraphSimple.AddVertex(nv6);

        // Looks like we're missing edges but since this is an undirected graph
        // edges will be automatically added for the opposite direction
        //
        _nonPlanarGraphSimple.AddEdge(nv1, nv2);
        _nonPlanarGraphSimple.AddEdge(nv1, nv3);
        _nonPlanarGraphSimple.AddEdge(nv1, nv4);
        _nonPlanarGraphSimple.AddEdge(nv1, nv5);
        _nonPlanarGraphSimple.AddEdge(nv1, nv6);

        _nonPlanarGraphSimple.AddEdge(nv2, nv3);
        _nonPlanarGraphSimple.AddEdge(nv2, nv4);
        _nonPlanarGraphSimple.AddEdge(nv2, nv5);
        _nonPlanarGraphSimple.AddEdge(nv2, nv6);

        _nonPlanarGraphSimple.AddEdge(nv3, nv4);
        _nonPlanarGraphSimple.AddEdge(nv3, nv5);
        _nonPlanarGraphSimple.AddEdge(nv3, nv6);

        _nonPlanarGraphSimple.AddEdge(nv4, nv5);
        _nonPlanarGraphSimple.AddEdge(nv4, nv6);

        _nonPlanarGraphSimple.AddEdge(nv5, nv6);
    }

    [Test]
    public void IsPlanar()
    {
        var boyerMyrvold = new BoyerMyrvold<int>();

        Assert.That(boyerMyrvold.IsPlanar(_planarGraphSimple, out var embedding), Is.EqualTo(true));
    }

    [Test]
    public void HasExpectedFaces()
    {
        var boyerMyrvold = new BoyerMyrvold<Vertex>();

        boyerMyrvold.IsPlanar(_planarGraphWTF, out var embedding);
        boyerMyrvold.TryGetPlanarFaces(_planarGraphWTF, out var faces);
         
        Assert.That(faces.Faces.Count, Is.EqualTo(1));
    }

    [Test]
    public void IsNotPlanar()
    {
        var boyerMyrvold = new BoyerMyrvold<int>();

        Assert.That(boyerMyrvold.IsPlanar(_nonPlanarGraphSimple), Is.EqualTo(false));
    }

    [Test]
    public void ChainsGenerated()
    {
        GraphDecomposition decomp = GraphDecomposer.Generate(_planarGraph);

        Assert.That(decomp.Chains.Count, Is.EqualTo(4));
    }
}