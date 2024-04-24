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

public class LocalDiskContentProviderTests
{
    private IFileSystem _fs;
    private ILoggerFactory _logger;

    [SetUp]
    public void Setup() {
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

        _fs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"/my-simple.graph.json", new MockFileData(Content.SimpleGraph) },
            { @"/empty.graph.json", new MockFileData(emptyGraphJson) },
            { @"/malformed.graph.json", new MockFileData(malformedGraphJson) },
            { @"/global/tilemaps/s-sq-desert-1.tilemap.json", new MockFileData(Content.TileMapTemplateWithObjects) },
            { @"/global/tilesets/s-sq-desert-1.tileset.json", new MockFileData(Content.TileSetTemplate) },
            { @"/global/blueprints.json", new MockFileData(Content.RoomBlueprints) },
            { @"/global/definitions.json", new MockFileData(Content.RoomDefinitions) },
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

        var value = JsonSerializer.Deserialize<TiledMap>(Content.TileMapTemplateWithObjects, opts);

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