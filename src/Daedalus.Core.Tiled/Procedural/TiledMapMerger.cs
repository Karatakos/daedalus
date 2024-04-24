namespace Daedalus.Core.Tiled.Procedural;

using Daedalus.Core.Tiled.Maps;
using Daedalus.Core.Tiled.Procedural.Errors;

using FluentResults;

using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;

internal class TiledMapMerger
{
    public HashSet<uint> DirtyTileIndices;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory; 
    private int ObjectCount = 0;

    internal TiledMapMerger(
        ILoggerFactory loggerFactory) {

        _logger = loggerFactory.CreateLogger<TiledMapMerger>();
        _loggerFactory = loggerFactory;
        
        DirtyTileIndices = new HashSet<uint>();
    }

    /* Merges source map into destination map. Mutates destination map.
    */
    internal Result<TiledMap> Merge(
        TiledMap destination,
        TiledMap source,
        Vector2 position,
        uint emptyGid) {

        // For tile layers a caller may need to know which tiles of the destination map were touched
        //
        DirtyTileIndices.Clear();

        int layer = 0;
        MergeLayersWithMap(destination, source, source.Layers, position, emptyGid, ref layer);
        
        var res = MergeTilesetsWithMap(destination, source);
        if (res.IsFailed)
            return Result.Fail(res.Errors);
        
        var map = res.Value;
        
        map.NextObjectId = ObjectCount + 1;
        map.NextlayerId = map.Layers.Count + 1;
        
        return map;
    }   

    private Result<TiledMap> MergeTilesetsWithMap(TiledMap destination, TiledMap source) {
        var tileSets = source.TileSets;
        for(int i = 0; i < tileSets.Count; i++) {
            var tileset = tileSets[i];
            var templateSouceWithoutPath = Path.GetFileName(tileset.Source);

            var matchingTileSetIndex = destination.TileSets.FindIndex(
                t => Path.GetFileName(t.Source) == templateSouceWithoutPath);

            if (matchingTileSetIndex == i)
                continue;

            // If we did get a match (that's not at the same index as our template reference); OR
            // We didn't get a match but our template reference would not be the last template in the new map
            // Then it would screw up the First GID references and our map would not work so exit with an error.
            //
            if (matchingTileSetIndex != -1 || (matchingTileSetIndex == -1 && i < destination.TileSets.Count-1))
                return Result.Fail(new TiledMapBuilerTemplateTileSetsOrderError());

            // Path is irrelevent. It's up to the caller to fetch the right tileset by name, e.g. from compiled resources 
            //
            destination.TileSets.Add(new TiledMapSet(tileset.FirstGid, templateSouceWithoutPath));
        }

        return destination;
    }

    private void MergeLayersWithMap(TiledMap destination, TiledMap source, List<TiledMapLayer> layers, Vector2 position, uint emptyGid, ref int layerIndex) {
        // Algorithm:
        //
        //  Given a valid room template:
        //      Enumerate tiles for each tile layer of the template
        //      Get local position for tile index as well as the tile value
        //      Translate the position according to the room's world position
        //      Get tile index in map for new position and store the tile value at this index in composite map

        for (int i = 0; i < layers.Count; i++) {
            var tempLayer = layers[i];

            if (tempLayer.Type == TiledMapLayerType.group) {
                MergeLayersWithMap(destination, source, tempLayer.Layers, position, emptyGid, ref layerIndex);

                continue;
            }

            if (!TryGetMatchingMapLayer(destination, tempLayer, layerIndex, out TiledMapLayer mapLayer)) {
                mapLayer = new TiledMapLayer(
                    destination.Layers.Count + 1,
                    tempLayer.Type,
                    $"Composite Map Layer Index #{destination.Layers.Count + 1}",
                    destination.Width,
                    destination.Height);

                destination.Layers.Add(mapLayer);

                if (tempLayer.Type == TiledMapLayerType.tilelayer) {
                    var tileLayerDataSize = destination.Width * destination.Height;

                    mapLayer.Data = new uint[tileLayerDataSize];
                    
                    Array.Fill<uint>(mapLayer.Data, emptyGid);
                }  
                else {
                    mapLayer.Objects = new List<TiledObject>();
                    mapLayer.Draworder = tempLayer.Draworder;
                }
            }
            
            if (mapLayer.Type == TiledMapLayerType.tilelayer)
                TransformAndMergeTileLayerWithMapLayer(mapLayer, tempLayer, destination, source, position);
            else
                TransformMergeObjectLayerWithMapLayer(mapLayer, tempLayer, position);
                
            layerIndex++;
        }
    }

    private void TransformMergeObjectLayerWithMapLayer(TiledMapLayer destinationLayer, TiledMapLayer sourceLayer, Vector2 position) {
        foreach (TiledObject templateObject in sourceLayer.Objects) {
            var copy = new TiledObject(++ObjectCount, templateObject.Name, templateObject.Type) {
                X = templateObject.X + position.X,
                Y = templateObject.Y + position.Y,
                Width = templateObject.Width,
                Height = templateObject.Height,
                Rotation = templateObject.Rotation,
                IsPoint = templateObject.IsPoint,
                IsEllipse = templateObject.IsEllipse,
                Gid = templateObject.Gid };
            
            if (templateObject.Polygon != null) {
                copy.Polygon = new List<TiledPolygon2d>();

                foreach (TiledPolygon2d poly in templateObject.Polygon)
                    copy.Polygon.Add(new TiledPolygon2d(poly.X, poly.Y));
            }

            if (templateObject.Properties != null) {
            copy.Properties = new List<TiledProperty>();

            foreach (TiledProperty prop in templateObject.Properties)
                copy.Properties.Add(new TiledProperty(prop.Name, prop.Type, prop.Value));
            }

            destinationLayer.Objects.Add(copy);
        }
    }

    private void TransformAndMergeTileLayerWithMapLayer(
        TiledMapLayer destinationLayer, 
        TiledMapLayer sourceLayer, 
        TiledMap destination, 
        TiledMap source, 
        Vector2 position) {

        for (uint j = 0; j < sourceLayer.Data.Length; j++) {
            var localpos = source.GetWorldSpacePositionForTileIndex(j);
            var worldTileIndex = destination.GetTileIndexContainingWorldSpacePosition(position + localpos);

            destinationLayer.Data[worldTileIndex] = sourceLayer.Data[j];

            if (!DirtyTileIndices.Contains(worldTileIndex))
                DirtyTileIndices.Add(worldTileIndex);
        }   
    }

    private bool TryGetMatchingMapLayer(TiledMap destination, TiledMapLayer sourceLayer, int tempLayerIndex, out TiledMapLayer matchingLayer) {
        // Our intent is to compress layers in the final map to avoid uneccessarily large map sizes. 
        // To this end, our intent here is to find a matching world map layer to merge into
        //
        // Algorithm:
        //  For each world map layers starting at the current template layer index
        //  If the layer's type at this index matches our index type (tile or object) then return a match as we can merge into this layer
        //  Otherwise continue until we find a match or run out of world map layers
        //  
        //  Note: Caller should ensure that groups are flattened and so index represents sequential count of object and tile layers
        //
        for (int j = tempLayerIndex; j < destination.Layers.Count; j++) {
            if (destination.Layers[j].Type == sourceLayer.Type) {
                matchingLayer = destination.Layers[j];
                return true;
            }
        }

        matchingLayer = null;

        return false;
    }
}





