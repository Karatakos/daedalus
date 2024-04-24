namespace Daedalus.Core.Tiled.Tests;

using Microsoft.Xna.Framework;

using FluentAssertions;

using Daedalus.Core.Tiled.Maps;

public class TiledMapTests
{
    private TiledMap _map;
    private readonly uint _width = 5;
    private readonly uint _height = 5;
    private readonly uint _tileWidth = 32;
    private readonly uint _tileHeight = 32;
    private readonly uint _worldWidth = 160;
    private readonly uint _worldHeight = 160;

    [SetUp]
    public void Setup() {
        _map = new TiledMap(
            _width,
            _height,
            _tileWidth,
            _tileHeight);

        _map.Layers.Add(new TiledMapLayer(
            0,
            TiledMapLayerType.tilelayer,
            "tile map layer 1",
            _map.Width,
            _map.Height));

        _map.Layers[0].Data = [
            1, 2, 2, 2, 3,
            9, 10, 10, 10, 11,
            9, 10, 10, 10, 11,
            9, 10, 10, 10, 11,
            17, 18, 18, 18, 19
        ];   
    }

    [TearDown]
    public void TearDown() {
    }

    [Test, Sequential]
    public void TileIndexContainingWorldPosition(
        [Values(new int[]{10, 50}, new int[]{0, 0}, new int[]{120, 0})] int[] x, 
        [Values(5, 0, 3)] int y) {

        var tile = _map.GetTileIndexContainingWorldSpacePosition(new Vector2(x[0], x[1]));

        tile.Should().Be((uint)y);
    }

    [Test, Sequential]
    public void TileIndexToWorldPosition(
        [Values(5, 0)] int x, 
        [Values(new int[]{0, 32}, new int[]{0, 0})] int[] y) {

        var pos = _map.GetWorldSpacePositionForTileIndex((uint)x);

        pos.Should().Be(new Vector2(y[0], y[1]));
    }
}