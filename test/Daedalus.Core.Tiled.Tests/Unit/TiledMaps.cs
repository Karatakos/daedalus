namespace Daedalus.Tiled.Tests;

using Microsoft.Xna.Framework;

using FluentAssertions;

public class TileMaps
{
    private TiledMap _map;
    private readonly int _width = 5;
    private readonly int _height = 5;
    private readonly int _tileWidth = 32;
    private readonly int _tileHeight = 32;
    private readonly int _worldWidth = 160;
    private readonly int _worldHeight = 160;

    [SetUp]
    public void Setup() {
        _map = new TiledMap(
            "",
            "rightdown",
            _width,
            _height,
            _tileWidth,
            _tileHeight,
            new List<TiledMapLayer>(),
            new List<TiledSet>());

        var data = new int[] {
            1, 2, 2, 2, 3,
            9, 10, 10, 10, 11,
            9, 10, 10, 10, 11,
            9, 10, 10, 10, 11,
            17, 18, 18, 18, 19
        };

        _map.Layers.Add(new TiledMapLayer(
            0,
            "tile map layer 1",
            data,
            _map.Width,
            _map.Height));
    }

    [TearDown]
    public void TearDown() {
    }

    [Test, Sequential]
    public void TileIndexContainingWorldPosition(
        [Values(new int[]{10, 50}, new int[]{0, 0}, new int[]{120, 0})] int[] x, 
        [Values(5, 0, 3)] int y) {

        int tile = _map.GetTileIndexForWorldSpacePosition(new Vector2(x[0], x[1]));

        tile.Should().Be(y);
    }

    [Test, Sequential]
    public void TileIndexToWorldPosition(
        [Values(5, 0)] int x, 
        [Values(new int[]{0, 32}, new int[]{0, 0})] int[] y) {

        var pos = _map.GetWorldSpacePositionForTileIndex(x);

        pos.Should().Be(new Vector2(y[0], y[1]));
    }
}