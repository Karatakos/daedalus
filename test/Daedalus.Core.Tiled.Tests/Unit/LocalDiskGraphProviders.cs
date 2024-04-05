namespace Daedalus.Tiled.Tests;

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Logging;

using FluentResults;
using FluentResults.Extensions.FluentAssertions;
using FluentAssertions;
using Daedalus.Tiled.ContentProviders;

public class LocalDiskGraphProviders
{
    private IFileSystem _fs;
    private ILoggerFactory _logger;

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
                    "source":"..\/desert.tileset.tsx"
                    }],
            "tilewidth":32,
            "type":"map",
            "version":"1.8",
            "width":5
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
        config.Value.RoomDefinitions.Should().ContainKey(roomDefinitionLabel);

        var roomDefinition = config.Value.RoomDefinitions[roomDefinitionLabel];
        config.Value.RoomBlueprints.Should().ContainKey(roomDefinition.Blueprints[0]);

        var roomBlueprint = config.Value.RoomBlueprints[roomDefinition.Blueprints[0]];
        config.Value.Templates.Should().ContainKey(roomBlueprint.CompatibleTilemaps[0]);

        var roomConnection = config.Value.Graph.Connections[0];
        roomConnection.To.Should().Be(2);
        roomConnection.OneWay.Should().Be(true);
    }
}