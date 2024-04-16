namespace Daedalus.Core.Tiled.Tests;

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Logging;

using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using FluentAssertions;

using Daedalus.Core.Tiled.Procedural.ContentProviders;
using Daedalus.Core.Tiled.Maps;
using System.Text.Json;
using System.Text.Json.Serialization;

public class LocalDiskGraphProviders
{
    private IFileSystem _fs;
    private ILoggerFactory _logger;

    const string simplemaptest = 
        """
        { 
            "compressionlevel":-1,
            "height":5,
            "infinite":false,
            "layers":[
                    {
                        "data":[1, 2, 2, 2, 3,
                            9, 10, 10, 10, 11,
                            9, 10, 10, 10, 11,
                            9, 10, 10, 10, 11,
                            17, 18, 18, 18, 19],
                        "height":5,
                        "id":1,
                        "name":"Tile Layer 1",
                        "opacity":1,
                        "type":"tilelayer",
                        "visible":true,
                        "width":5,
                        "x":0,
                        "y":0
                    }],
            "nextlayerid":4,
            "nextobjectid":15,
            "orientation":"orthogonal",
            "renderorder":"right-down",
            "tiledversion":"1.8.5",
            "tileheight":32,
            "tilesets":[
                    {
                    "firstgid":1,
                    "source":"tilesets/s-sq-desert-1.tileset.json"
                    }],
            "tilewidth":32,
            "type":"map",
            "version":"1.8",
            "width":5
        }
        """;

    [SetUp]
    public void Setup() {
        const string simpleGraphJson = 
            """
            {
                "label": "my-simple",
                "rooms": [
                    { "number": 1, "definition": "entrance" },
                    { "number": 2, "definition": "normal" }
                ],
                "connections": [
                    { "from": 1, "to": 2, "one-way": true}
                ]
            }
            """;

        const string emptyGraphJson = @"";

        const string malformedGraphJson = 
        """
        {
            "label": "malformed",
            "rooms": [
                {
                    "number": 1,
                    "connections": []
                }
            ]
        }
        """;

        const string s1TemplateJson = 
        """
        { 
            "compressionlevel":-1,
            "height":5,
            "infinite":false,
            "layers":[
                    {
                        "data":[1, 2, 2, 2, 3,
                            9, 10, 10, 10, 11,
                            9, 10, 10, 10, 11,
                            9, 10, 10, 10, 11,
                            17, 18, 18, 18, 19],
                        "height":5,
                        "id":1,
                        "name":"Tile Layer 1",
                        "opacity":1,
                        "type":"tilelayer",
                        "visible":true,
                        "width":5,
                        "x":0,
                        "y":0
                    },
                    {
                        "id":6,
                        "layers":[
                        {
                        "draworder":"topdown",
                        "id":5,
                        "name":"Object Layer 1",
                        "objects":[
                                {
                                "height":0,
                                "id":1,
                                "name":"",
                                "point":true,
                                "properties":[
                                        {
                                        "name":"EnemyHealth",
                                        "type":"string",
                                        "value":"100"
                                        }, 
                                        {
                                        "name":"ObjectId",
                                        "type":"int",
                                        "value":"1"
                                        }, 
                                        {
                                        "name":"ObjectType",
                                        "type":"string",
                                        "value":"enemy"
                                        }, 
                                        {
                                        "name":"Type",
                                        "type":"string",
                                        "value":"spawn"
                                        }],
                                "rotation":0,
                                "type":"",
                                "visible":true,
                                "width":0,
                                "x":208,
                                "y":146
                                }, 
                                {
                                "height":18,
                                "id":3,
                                "name":"",
                                "rotation":0,
                                "type":"",
                                "visible":true,
                                "width":20,
                                "x":225,
                                "y":49
                                }, 
                                {
                                "ellipse":true,
                                "height":26,
                                "id":4,
                                "name":"",
                                "rotation":0,
                                "type":"",
                                "visible":true,
                                "width":25,
                                "x":114,
                                "y":45
                                }, 
                                {
                                "height":0,
                                "id":5,
                                "name":"",
                                "polygon":[
                                        {
                                        "x":0,
                                        "y":0
                                        }, 
                                        {
                                        "x":23,
                                        "y":30
                                        }, 
                                        {
                                        "x":-8,
                                        "y":31
                                        }],
                                "rotation":0,
                                "type":"",
                                "visible":true,
                                "width":0,
                                "x":66,
                                "y":131
                                }],
                        "opacity":1,
                        "type":"objectgroup",
                        "visible":true,
                        "x":0,
                        "y":0
                        }],
                        "name":"Group 1",
                        "opacity":1,
                        "type":"group",
                        "visible":true,
                        "x":0,
                        "y":0
                        }],
            "nextlayerid":4,
            "nextobjectid":15,
            "orientation":"orthogonal",
            "renderorder":"right-down",
            "tiledversion":"1.8.5",
            "tileheight":32,
            "tilesets":[
                    {
                    "firstgid":1,
                    "source":"tilesets/s-sq-desert-1.tileset.json"
                    }],
            "tilewidth":32,
            "type":"map",
            "version":"1.8",
            "width":5
        }
        """;

        const string s1TilesetJson = 
        """
        {   
            "columns":8,
            "image":"..\/tmw_desert_spacing.png",
            "imageheight":199,
            "imagewidth":265,
            "margin":1,
            "name":"desert",
            "spacing":1,
            "tilecount":48,
            "tiledversion":"1.8.5",
            "tileheight":32,
            "tiles":[
                    {
                    "id":0,
                    "objectgroup":
                        {
                        "draworder":"index",
                        "id":2,
                        "name":"",
                        "objects":[
                                {
                                "height":16.8421,
                                "id":1,
                                "name":"",
                                "rotation":0,
                                "type":"",
                                "visible":true,
                                "width":15.7255,
                                "x":16.0047,
                                "y":15.0741
                                }],
                        "opacity":1,
                        "type":"objectgroup",
                        "visible":true,
                        "x":0,
                        "y":0
                        },
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":1,
                    "objectgroup":
                        {
                        "draworder":"index",
                        "id":2,
                        "name":"",
                        "objects":[
                                {
                                "height":16.1814,
                                "id":1,
                                "name":"",
                                "rotation":0,
                                "type":"",
                                "visible":true,
                                "width":32,
                                "x":0,
                                "y":15.8186
                                }],
                        "opacity":1,
                        "type":"objectgroup",
                        "visible":true,
                        "x":0,
                        "y":0
                        },
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":2,
                    "objectgroup":
                        {
                        "draworder":"index",
                        "id":2,
                        "name":"",
                        "objects":[
                                {
                                "height":0,
                                "id":2,
                                "name":"",
                                "rotation":0,
                                "type":"",
                                "visible":true,
                                "width":0.0930503,
                                "x":10.2355,
                                "y":22.0529
                                }, 
                                {
                                "height":16.9352,
                                "id":4,
                                "name":"",
                                "rotation":0,
                                "type":"",
                                "visible":true,
                                "width":16.2838,
                                "x":0.0930503,
                                "y":15.0741
                                }],
                        "opacity":1,
                        "type":"objectgroup",
                        "visible":true,
                        "x":0,
                        "y":0
                        },
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":3,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":4,
                    "properties":[
                            {
                            "name":"Direction",
                            "type":"string",
                            "value":"East"
                            }, 
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Door"
                            }]
                    }, 
                    {
                    "id":5,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":6,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":7,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":8,
                    "objectgroup":
                        {
                        "draworder":"index",
                        "id":2,
                        "name":"",
                        "objects":[
                                {
                                "height":32,
                                "id":1,
                                "name":"",
                                "rotation":0,
                                "type":"",
                                "visible":true,
                                "width":16.375,
                                "x":15.625,
                                "y":0
                                }],
                        "opacity":1,
                        "type":"objectgroup",
                        "visible":true,
                        "x":0,
                        "y":0
                        },
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":9,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":10,
                    "objectgroup":
                        {
                        "draworder":"index",
                        "id":2,
                        "name":"",
                        "objects":[
                                {
                                "height":32,
                                "id":1,
                                "name":"",
                                "rotation":0,
                                "type":"",
                                "visible":true,
                                "width":16.375,
                                "x":0,
                                "y":0
                                }],
                        "opacity":1,
                        "type":"objectgroup",
                        "visible":true,
                        "x":0,
                        "y":0
                        },
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":11,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":12,
                    "properties":[
                            {
                            "name":"Direction",
                            "type":"string",
                            "value":"East"
                            }, 
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Door"
                            }]
                    }, 
                    {
                    "id":13,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":14,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":15,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":16,
                    "objectgroup":
                        {
                        "draworder":"index",
                        "id":2,
                        "name":"",
                        "objects":[
                                {
                                "height":17,
                                "id":1,
                                "name":"",
                                "rotation":0,
                                "type":"",
                                "visible":true,
                                "width":16.25,
                                "x":15.75,
                                "y":0
                                }],
                        "opacity":1,
                        "type":"objectgroup",
                        "visible":true,
                        "x":0,
                        "y":0
                        },
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":17,
                    "objectgroup":
                        {
                        "draworder":"index",
                        "id":2,
                        "name":"",
                        "objects":[
                                {
                                "height":16.875,
                                "id":1,
                                "name":"",
                                "rotation":0,
                                "type":"",
                                "visible":true,
                                "width":32,
                                "x":0,
                                "y":0
                                }],
                        "opacity":1,
                        "type":"objectgroup",
                        "visible":true,
                        "x":0,
                        "y":0
                        },
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":18,
                    "objectgroup":
                        {
                        "draworder":"index",
                        "id":2,
                        "name":"",
                        "objects":[
                                {
                                "height":17.25,
                                "id":1,
                                "name":"",
                                "rotation":0,
                                "type":"",
                                "visible":true,
                                "width":16,
                                "x":0,
                                "y":0
                                }],
                        "opacity":1,
                        "type":"objectgroup",
                        "visible":true,
                        "x":0,
                        "y":0
                        },
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":19,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":20,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":21,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":22,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":23,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":24,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":25,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":26,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":27,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":28,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":29,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":30,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Object"
                            }]
                    }, 
                    {
                    "id":31,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Object"
                            }]
                    }, 
                    {
                    "id":32,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":33,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":34,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Floor"
                            }]
                    }, 
                    {
                    "id":35,
                    "properties":[
                            {
                            "name":"Direction",
                            "type":"string",
                            "value":"North"
                            }, 
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Door"
                            }]
                    }, 
                    {
                    "id":36,
                    "properties":[
                            {
                            "name":"Direction",
                            "type":"string",
                            "value":"North"
                            }, 
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Door"
                            }]
                    }, 
                    {
                    "id":37,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Object"
                            }]
                    }, 
                    {
                    "id":38,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Object"
                            }]
                    }, 
                    {
                    "id":39,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Object"
                            }]
                    }, 
                    {
                    "id":40,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":41,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":42,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Wall"
                            }]
                    }, 
                    {
                    "id":43,
                    "properties":[
                            {
                            "name":"Direction",
                            "type":"string",
                            "value":"North"
                            }, 
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Door"
                            }]
                    }, 
                    {
                    "id":44,
                    "properties":[
                            {
                            "name":"Direction",
                            "type":"string",
                            "value":"North"
                            }, 
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Door"
                            }]
                    }, 
                    {
                    "id":45,
                    "objectgroup":
                        {
                        "draworder":"index",
                        "id":2,
                        "name":"",
                        "objects":[
                                {
                                "height":0,
                                "id":1,
                                "name":"",
                                "polygon":[
                                        {
                                        "x":1.5,
                                        "y":-4.75
                                        }, 
                                        {
                                        "x":-7,
                                        "y":2.875
                                        }, 
                                        {
                                        "x":-6.75,
                                        "y":14.625
                                        }, 
                                        {
                                        "x":0.125,
                                        "y":10.5
                                        }, 
                                        {
                                        "x":0.375,
                                        "y":24.625
                                        }, 
                                        {
                                        "x":7.375,
                                        "y":23.875
                                        }, 
                                        {
                                        "x":7.25,
                                        "y":8.875
                                        }, 
                                        {
                                        "x":15.125,
                                        "y":4.5
                                        }, 
                                        {
                                        "x":15.125,
                                        "y":-4.5
                                        }],
                                "rotation":0,
                                "type":"",
                                "visible":true,
                                "width":0,
                                "x":11.375,
                                "y":4.875
                                }],
                        "opacity":1,
                        "type":"objectgroup",
                        "visible":true,
                        "x":0,
                        "y":0
                        },
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Object"
                            }]
                    }, 
                    {
                    "id":46,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Object"
                            }]
                    }, 
                    {
                    "id":47,
                    "properties":[
                            {
                            "name":"TileType",
                            "type":"string",
                            "value":"Object"
                            }]
                    }],
            "tilewidth":32,
            "type":"tileset",
            "version":"1.8"
            }
        """;

        const string blueprintsJson = 
        """
        [
            {
                "label": "small-square-1",
                "points": [
                    [0, 0],
                    [0, 5],
                    [5, 5],
                    [5, 0]
                ],
                "compatible-tilemaps": [
                    "small-square-1"
                ]},
            {
                "label": "large-square-1",
                "shape": [
                    [0, 0],
                    [0, 5],
                    [5, 5],
                    [5, 0]
                ],
                "compatible-tilemaps": [
                    "small-square-1"
                ]
            }
        ]
        """;

        const string definitionsJson = 
        """
        [
            {
                "label": "entrance",
                "type": "Entrance",
                "blueprints": [
                    "small-square-1"
                ]
            },
            {
                "label": "normal",
                "type": "Normal",
                "blueprints": [
                    "small-square-1",
                    "large-square-1"
                ]
            }
        ]
        """;

        _fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"/my-simple.graph.json", new MockFileData(simpleGraphJson) },
            { @"/empty.graph.json", new MockFileData(emptyGraphJson) },
            { @"/malformed.graph.json", new MockFileData(malformedGraphJson) },
            { @"/global/tilemaps/small-square-1.tilemap.json", new MockFileData(s1TemplateJson) },
            { @"/global/tilesets/s-sq-desert-1.tileset.json", new MockFileData(s1TilesetJson) },
            { @"/global/blueprints.json", new MockFileData(blueprintsJson) },
            { @"/global/definitions.json", new MockFileData(definitionsJson) },
        });

        _logger = LoggerFactory.Create((builder) => builder.AddSimpleConsole());
    }

    [TearDown]
    public void TearDown() {
        _logger.Dispose();
    }

    [Test]
    public async Task LoadingValidGraph() {
        IContentProvider provider = new LocalDiskContentProvider(_logger, _fs, "/");

        var config = await provider.LoadAsync("my-simple");
        config.Should().BeSuccess();
    }

    [Test]
    public async Task LoadingMissingGraph() {
        IContentProvider provider = new LocalDiskContentProvider(_logger, _fs, "/");

        var config = await provider.LoadAsync("no-graph-here");
        config.Should().BeFailure();
    }

    [Test]
    public async Task LoadingEmptyGraph() {
        IContentProvider provider = new LocalDiskContentProvider(_logger, _fs, "/");

        var config = await provider.LoadAsync("empty");
        config.Should().BeFailure();
    }

    [Test]
    public async Task LoadingMalformedGraph() {
        IContentProvider provider = new LocalDiskContentProvider(_logger, _fs, "/");

        var config = await provider.LoadAsync("malformed");
        config.Should().BeFailure();
    }

    [Test]
    public async Task RandomGraphNodeDependencyCheck() {
        IContentProvider provider = new LocalDiskContentProvider(_logger, _fs, "/");

        var config = await provider.LoadAsync("my-simple");
        config.Should().BeSuccess();

        var roomDefinitionLabel = config.Value.Graph.Rooms[0].Definition;
        config.Value.GraphDependencies.RoomDefinitions.Should().ContainKey(roomDefinitionLabel);

        var roomDefinition = config.Value.GraphDependencies.RoomDefinitions[roomDefinitionLabel];
        config.Value.GraphDependencies.RoomBlueprints.Should().ContainKey(roomDefinition.Blueprints[0]);

        var roomBlueprint = config.Value.GraphDependencies.RoomBlueprints[roomDefinition.Blueprints[0]];
        config.Value.GraphDependencies.Templates.Should().ContainKey(roomBlueprint.CompatibleTilemaps[0]);

        var roomConnection = config.Value.Graph.Connections[0];
        roomConnection.To.Should().Be(2);
        roomConnection.OneWay.Should().Be(true);

        config.Value.GraphDependencies.TileSets["s-sq-desert-1.tileset.json"].TileCount.Should().Be(48);
    }

    [Test]
    public void TemplateSerializeEnumTest() {
    
        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new JsonStringEnumConverter());
        opts.PropertyNameCaseInsensitive = true;
        opts.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;

        var value = JsonSerializer.Deserialize<TiledMap>(simplemaptest, opts);

        value.Layers[0].Type.Should().Be(TiledMapLayerType.tilelayer);
    }

    [Test]
    public void JsonStringEnumConverterTest() {
        const string json = 
        """
        { 
            "foo": 2,
            "type": "secondvalue"
        }
        """;

        var opts = new JsonSerializerOptions();
        opts.Converters.Add(new JsonStringEnumConverter());
        opts.PropertyNameCaseInsensitive = true;
        opts.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;

        var value = JsonSerializer.Deserialize<SomeType>(json, opts);

        value.Foo.Should().Be(2);
        value.Type.Should().Be(SomeTypeEnum.SecondValue);
    }

    public class SomeType {
        [JsonPropertyName("foo")]
        public int Foo { get; set; }

        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SomeTypeEnum Type { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SomeTypeEnum {
        FirstValue,
        SecondValue
    }
}