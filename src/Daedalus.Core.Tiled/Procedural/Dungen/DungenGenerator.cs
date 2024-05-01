namespace Daedalus.Core.Tiled.Procedural;

using Daedalus.Core.Tiled.Procedural.ContentProviders;
using Daedalus.Core.Tiled.Procedural.Errors;

using Dungen;

using Microsoft.Extensions.Logging;
using FluentResults;

public class DungenGenerator {
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    public DungenGenerator(ILoggerFactory loggerFactory) {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DungenGenerator>();
    }

    /* Converts input graph to Dungen friendly graph and calls into the
    *  Dungen framework to attempt map generation. 
    *
    *  Important: CPU and time intensive and so run on a seperate [threadpool] thread
    *
    */
    public async Task<Result<Layout>> GenerateAsync(
        TiledMapContent inputGraph,
        TiledMapDungenBuilderProps inputProps) {

        var props = CreateDungenGeneratorProps(inputProps);

        var graph = CreateGraph(inputGraph);
        if (graph.IsFailed)
            return Result.Fail(graph.Errors);

        props.Graph = graph.Value;

        return await Task.Run(() => GenerateDungenLayout(props));
    }

    private Result<DungenGraph> CreateGraph(TiledMapContent graphInputData) {
        DungenGraph graph = new DungenGraph();

        Dictionary<string, RoomDefinition> definitions = new Dictionary<string, RoomDefinition>();
        Dictionary<string, RoomBlueprint> blueprints = new Dictionary<string, RoomBlueprint>();


        foreach (TiledMapGraphRoomNodeContent inputNode in graphInputData.Graph.Rooms) {
            if (!definitions.ContainsKey(inputNode.Definition)) {
                TiledMapGraphRoomDefinitionContent inputRoomDefinition = 
                    graphInputData.GraphDependencies.RoomDefinitions[inputNode.Definition];

                // Process the blueprints 
                //
                List<RoomBlueprint> blueprintsTmp = new List<RoomBlueprint>();
                foreach (string inputBlueprintLabel in inputRoomDefinition.Blueprints) {
                    if (!blueprints.ContainsKey(inputBlueprintLabel)) {
                        var inputBlueprint = graphInputData.GraphDependencies.RoomBlueprints[inputBlueprintLabel];

                        var points = new List<Vector2F>();
                        foreach (int[] point in inputBlueprint.Points) 
                            points.Add(new Vector2F(point[0], point[1]));

                        // Cache this for other room defintitions
                        //
                        blueprints.Add(inputBlueprintLabel, new RoomBlueprint(points));
                    }

                    blueprintsTmp.Add(blueprints[inputBlueprintLabel]);
                }

                if (!Enum.TryParse(inputRoomDefinition.Type, out RoomType type))
                    return Result.Fail(new DungenGraphValidationError($"Room type {type} does not match any room type supported by the Dungen API"));

                // Cache a new room definition. 
                //
                definitions.Add(inputNode.Definition, new RoomDefinition(blueprintsTmp, type));
            }

            graph.AddRoom(inputNode.Number, definitions[inputNode.Definition]);
        }

        // We can now safetly add room connections
        //
        foreach (TiledMapGraphConnection connection in graphInputData.Graph.Connections)
            // TODO: Adds by vertex index NOT vertex ID. This has to be fixed.
            //
            graph.AddConnection(connection.From, connection.To, connection.OneWay ? Direction.Uni : Direction.Bi );

        return graph;
    }

    private DungenGeneratorProps CreateDungenGeneratorProps(TiledMapDungenBuilderProps props) {
        return new DungenGeneratorProps() {
            LoggerFactory = _loggerFactory,
            DoorWidth = props.DoorWidth,
            DoorToCornerMinGap = props.DoorMinDistanceFromCorner,
            TargetSolutions = 1,
        };
    }

    private Result<Layout> GenerateDungenLayout(DungenGeneratorProps props) {
        Dungen.DungenGenerator generator = new(props);

        // Compute config spaces, validate planar graph, etc.
        //
        generator.Initialize(); 

        // Tries to generate a layout
        //
        if (!generator.TryGenerate()) {
            _logger.LogError("Not able to find a solution to the input graph.");

            return Result.Fail(new DungenSolutioNotFoundError());
        }
        
        return generator.Vend();
    }
}