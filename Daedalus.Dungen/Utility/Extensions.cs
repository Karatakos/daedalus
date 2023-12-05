using GraphPlanarityTesting.Graphs.DataStructures;

public static class ListExtensions {
    /*
    *   Knuth Shuffle
    */
    public static List<T> Shuffle<T>(this List<T> unshuffled) {
        Random r = new Random();
        List<T> shuffled = unshuffled;

        //Step 1: For each unshuffled item in the collection
        for (int n = shuffled.Count - 1; n > 0; --n)
        {
            //Step 2: Randomly pick an item which has not been shuffled
            int k = r.Next(n + 1);

            //Step 3: Swap the selected item with the last "unstruck" letter in the collection
            T temp = shuffled[n];
            shuffled[n] = shuffled[k];
            shuffled[k] = temp;
        }

        return shuffled;
    } 
}

public static class UndirectedAdjacencyListGraphExtensions {
    public static IEnumerable<IEdge<T>> GetNeighbouringEdges<T>(this IGraph<T> graph, T vertex)
		{
			foreach (IEdge<T> edge in graph.Edges) {
				// TODO: Figure out a value compare?
				//
				if (edge.Source.Equals(vertex) || edge.Target.Equals(vertex)) 
					yield return edge;
			}
		}
}