namespace Dungen.Generator;

using GraphPlanarityTesting.Graphs.DataStructures;
using GraphPlanarityTesting.PlanarityTesting.BoyerMyrvold;

public class GraphDecomposition {
    public GraphDecomposition(UndirectedAdjacencyListGraph<Vertex> graph) {
        Graph = graph;
        Chains = new SortedList<int, Chain>();
    }
    public SortedList<int, Chain> Chains { get; set; }
    public UndirectedAdjacencyListGraph<Vertex> Graph { get; set; }
}

public static class GraphDecomposer
{
    public static GraphDecomposition Generate(UndirectedAdjacencyListGraph<Vertex> graph) { 
        List<Vertex> unchainedVertices = new List<Vertex>(graph.Vertices);
        BoyerMyrvold<Vertex> boyerMyrvold = new BoyerMyrvold<Vertex>();

        GraphDecomposition decomp = new GraphDecomposition(graph);

        if (!boyerMyrvold.IsPlanar(graph, out var embedding))
            throw new Exception("Graph is not planar.");

        if (!boyerMyrvold.TryGetPlanarFaces(graph, out var facestmp)) 
            throw new Exception("Could not extract faces from graph.");

        // Let's have a look at the embedding for other faces, i.e all edges for each vertex
        //
        for (int i=0; i<facestmp.Faces.Count; i++) {
            Console.WriteLine(String.Format("Detected face {0}", i+1));
            for (int j=0; j<facestmp.Faces[i].Count; j++) {
                Console.WriteLine(String.Format("{0}: {1}",
                    facestmp.Faces[i][j],
                    embedding.GetEdgesAroundVertex(facestmp.Faces[i][j]).Aggregate(
                        "", 
                        (a, b) => String.Format("{0}; E({1},{2})", a, b.Source, b.Target))));
            }
            Console.WriteLine("");
        }

        List<List<Vertex>> faces = facestmp.Faces.GetRange(1, facestmp.Faces.Count-1);

        int depth = 0;
        

        // Keep generating chains until we've accounted for all vertices
        // 
        while (unchainedVertices.Count != 0) {
            // There maybe cases where we don't add a chain, known cases:
            //
            // 1. Sometimes a smaller face is generated that is already contained within another 
            //    larger face. In these cases, some or all vertices from a face may have already 
            //    been added to an earlier chain.
            //
            bool incrementDepth = true;

            // First chains should be the shortest cyclic part of the graph
            //
            if (decomp.Chains.Count == 0) {
                if (TryGetSmallestFace(faces, out var face)) {
                    if(!TryAddChainWithNewVertices(unchainedVertices, decomp, face, depth))
                        incrementDepth = false;

                    faces.Remove(face);
                } 
            }
            // Next priority is checking if we have another face that neighbors a chain vertex
            //
            else if (TryGetFaceNeighboringChain(faces, decomp, out var face)) {
                if(!TryAddChainWithNewVertices(unchainedVertices, decomp, face, depth))
                    incrementDepth = false;

                faces.Remove(face);
            }
            // Lastly we will attempt to find an uncovered vertex that neighbours a chain's vertex
            //
            else {
                Vertex vertex = null;
                if (TryGetVertexNeighboringAnyChain(unchainedVertices, decomp, out vertex)) {
                    Chain chain = new Chain(depth);

                    // DFS -- process all vertices as a new chain 
                    // Finishes if we find a node already processed (in a chain) or to be processed (in a face)
                    //
                    while (vertex != null && unchainedVertices.Contains(vertex) && !IsVertexInFace(vertex, faces)) { 
                        // Add it to the chain a and mark as processed!
                        //
                        chain.Vertices.Add(vertex);
                        unchainedVertices.Remove(vertex);

                        // We wil process other branches later -- greedy DFS
                        //
                        vertex = graph.GetNeighbours(vertex).First();
                    }

                    // TODO: Depending on graph shape we may end up with lots of single node
                    //       chains. See file:///Users/adrian/Downloads/bachelor_thesis.pdf (pg. 23)
                    //
                    decomp.Chains.Add(depth, chain);
                }
            }

            if (incrementDepth)
                depth++;
        }

        // Let's have a look at 
        //
        for (int i=0; i<decomp.Chains.Count; i++) {
            Console.WriteLine(String.Format("Extracted chain {0}", i+1));
            Console.WriteLine(String.Format("Vertices: {0}",
                decomp.Chains[i].Vertices.Aggregate(
                    "", 
                    (a, b) => String.Format("{0}; {1}", a, b))));
            Console.WriteLine("");
        }

        return decomp;
    }

    private static bool TryAddChainWithNewVertices(List<Vertex> unchainedVertices, GraphDecomposition decomp, List<Vertex> vertices, int depth) {
        Chain chain = new Chain(depth);

        foreach (Vertex v in vertices) {
            if (unchainedVertices.Contains(v)) {
                chain.Vertices.Add(v);
                unchainedVertices.Remove(v);
            }
        }

        if (chain.Vertices.Count > 0) {
            decomp.Chains.Add(depth, chain);

            return true;
        }

        return false;
    }

    private static void MarkFaceVerticesProcessed(List<Vertex> vertices, List<Vertex> face) {
        foreach (Vertex vertex in face)
            vertices.Remove(vertex);
    }

    private static bool IsVertexInFace(Vertex vertex, List<List<Vertex>> faces) {
        foreach (List<Vertex> face in faces) {
            if (face.Contains(vertex))
                return true;
        }

        return false;
    }

    private static bool TryGetSmallestFace(List<List<Vertex>> faces, out List<Vertex> smallestFace) {
        List<Vertex> winningFace = null;
        try {
            int minVertices = 0;
        
            foreach (List<Vertex> face in faces) {
                int count = face.Count();
                if (count > 0 && (minVertices <= 0 || count <= minVertices)) {
                    minVertices = count;
                    winningFace = face;
                }
            }

            if (winningFace != null) {
                smallestFace = winningFace;
                return true;
            }
        }
        catch (Exception) {
            
        }

        smallestFace = null;

        return false;
    }

    private static bool TryGetFaceNeighboringChain(List<List<Vertex>> faces, GraphDecomposition decomp, out List<Vertex> neighboringFace) {
        try {
            if (faces.Count > 0) {
                // TODO: Optimize!!
                //
                for (int i=0; i<decomp.Chains.Count; i++) {
                    Chain chain = decomp.Chains[i];
                    foreach (Vertex chainV in chain.Vertices) {
                        foreach (List<Vertex> face in faces) {
                            foreach (Vertex v in face) {
                                if (decomp.Graph.GetNeighbours(chainV).Contains(v)) {
                                    neighboringFace = face;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception) {

        }

        neighboringFace = null;

        return false;
    }

    private static bool TryGetVertexNeighboringAnyChain(List<Vertex> remainingVertexes, GraphDecomposition decomp, out Vertex neighboringVertex) {
        try { 
            // TODO: Optimize!!
            //
            for (int i=0; i<decomp.Chains.Count; i++) {
                Chain chain = decomp.Chains[i];
                foreach (Vertex chainV in chain.Vertices) {
                    foreach (Vertex v in remainingVertexes) {
                        if (decomp.Graph.GetNeighbours(chainV).Contains(v)) {
                            neighboringVertex = v;
                            return true;
                        }
                    }
                }
            }
        }
        catch (Exception) {

        }

        neighboringVertex = null;

        return false;
    }
}
