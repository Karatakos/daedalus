namespace Daedalus.Core.Tiled.Procedural.ContentProviders;

using Daedalus.Core.Tiled.Maps;

public record TiledMapContent (
    TiledMapGraphContent Graph,
    Dictionary<string, TiledMap> Templates,
    Dictionary<string, TiledMapGraphRoomDefinitionContent> RoomDefinitions,
    Dictionary<string, TiledMapGraphRoomBlueprintContent> RoomBlueprints  
);

public record TiledMapGraphContent (
    string Label,
    List<TiledMapGraphRoomNodeContent> Rooms,
    List<TiledMapGraphConnection> Connections
);

public record TiledMapGraphConnection (
    int From,
    int To,
    bool OneWay = false
);

public record TiledMapGraphRoomNodeContent (
    int Number,
    string Definition
);

public record TiledMapGraphRoomBlueprintContent (
    string Label,
    List<int[]> Points,
    List<string> CompatibleTilemaps
);

public record TiledMapGraphRoomDefinitionContent (
    string Label,
    string Type,
    List<string> Blueprints
);