namespace Daedalus.Core.Tiled.Procedural.ContentProviders;

using Daedalus.Core.Tiled.Maps;
using Daedalus.Core.Tiled.Procedural.Errors;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;

using FluentResults;


/* Pulls map config from the local disk. Expected files and schema:
*
*    - Files: *.graph.json; schema: TiledMapGraphContent
*    - Files: *.tilemap.json; schema: TiledMap
*    - Files: *.tileset.json; schema: TiledSet
*    - File: blueprints.json; schema: TiledMapGraphRoomBlueprintContent
*    - File: definitions.json; schema: TiledMapGraphRoomDefinitionContent
*/
public class LocalDiskContentProvider : IContentProvider {
    private readonly ILogger _logger;
    private readonly IFileSystem _fs;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly string _directory;

    public LocalDiskContentProvider(ILoggerFactory loggerFactory, IFileSystem fs, string directory) 
        : this(loggerFactory, fs, directory, new JsonSerializerOptions() {
            PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower}) 
    {}

    public LocalDiskContentProvider(
        ILoggerFactory loggerFactory, 
        IFileSystem fs, 
        string directory,
        JsonSerializerOptions options){
        
        _logger = loggerFactory.CreateLogger<LocalDiskContentProvider>();
        _fs = fs;
        _directory = directory;
        _jsonSerializerOptions = options;

        // Use the [JsonStringEnumConverter] attribute string-enum casting
        //
        _jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        _jsonSerializerOptions.PropertyNameCaseInsensitive = true;
    }

    /* Loads the given graph as well as it's dependent blueprints and room definitions.
    *  Provider will scan recursively for associated JSON files in the given directory
    */
    public async Task<Result<TiledMapContent>> LoadAsync(string graphLabel) {
        var files = FileRepository.FromDirectory(_directory, _fs);
        if (files.IsFailed)
            return Result.Fail(files.Errors);

        var bpList = await DeserializeFileToJSON<List<TiledMapGraphRoomBlueprintContent>>(
            files.Value.BlueprintFilePath);
        if (bpList.IsFailed)
            return Result.Fail(bpList.Errors);

        var blueprints = bpList.Value.ToDictionary(blueprint => blueprint.Label);

        var definitionList = await DeserializeFileToJSON<List<TiledMapGraphRoomDefinitionContent>>(
            files.Value.DefintionFilePath);
        if (definitionList.IsFailed)
            return Result.Fail(definitionList.Errors);

        var definitions = definitionList.Value.ToDictionary(definition => definition.Label);

        // Load graph by matching file path to graph label
        //
        string? matchingGraphFilePath = files.Value.GraphFilePaths
            .Find(file => DoesFileMatchLabel(file, graphLabel, FileRegexPatterns.GRAPH_REGEX));

        if (string.IsNullOrEmpty(matchingGraphFilePath))
            return Result.Fail(
                new TiledMapGraphValidationError(
                    $"Graph {graphLabel} could not be found on disk under supplied directory {_directory}"));

        var graph = await DeserializeFileToJSON<TiledMapGraphContent>(matchingGraphFilePath);
        if (graph.IsFailed)
            return Result.Fail(graph.Errors);

        // Load tilemaps by enumerating maps from blueprints referenced by graph
        //
        var dependencies = await ValidateGraphAndLoadDependencies(graph.Value, blueprints, definitions, files.Value);
        if (dependencies.IsFailed)
            return Result.Fail(dependencies.Errors);

        return new TiledMapContent(graph.Value, dependencies.Value);
    }

    private async Task<Result<TiledMapDependencyContent>> ValidateGraphAndLoadDependencies(
        TiledMapGraphContent graph,
        Dictionary<string, TiledMapGraphRoomBlueprintContent> blueprints,
        Dictionary<string, TiledMapGraphRoomDefinitionContent> definitions,
        FileRepository repo) {

        var templates = new Dictionary<string, TiledMap>();
        var templateTileSets = new Dictionary<string, TiledSet>();

        foreach (TiledMapGraphRoomNodeContent room in graph.Rooms) {

            // TODO: Sickening amount of repeated code built up here. SIMPLIFY!!!

            if (room.Definition == null)
                return Result.Fail(
                    new TiledMapGraphValidationError(
                        $"No room definition defined in Graph {graph.Label} for room {room.Number}."));

            if (!definitions.ContainsKey(room.Definition))
                return Result.Fail(
                    new TiledMapGraphValidationError(
                        $"Room definition {room.Definition} referenced by Graph {graph.Label} could not be found."));

            if (definitions[room.Definition].Blueprints.Count == 0)
                return Result.Fail(
                    new TiledMapGraphValidationError(
                        $"No Blueprint(s) found for Room Definition {room.Definition} referenced by Graph {graph.Label}."));

            foreach (string bpLabel in definitions[room.Definition].Blueprints) {
                if (!blueprints.ContainsKey(bpLabel))
                    return Result.Fail(
                        new TiledMapGraphValidationError(
                            $"Blueprint {bpLabel} referenced by Room Definition {room.Definition} could not be found."));

                if (blueprints[bpLabel].CompatibleTilemaps == null || blueprints[bpLabel].CompatibleTilemaps.Count == 0)
                    return Result.Fail(
                        new TiledMapGraphValidationError(
                            $"No compatible tilemaps defined for Blueprint {bpLabel} referenced by Graph {graph.Label}."));

                foreach (string tmLabel in blueprints[bpLabel].CompatibleTilemaps) {
                    if (templates.ContainsKey(tmLabel))
                        continue;

                    string? matchingTilemapFilePath = repo.TileMapFilePaths
                        .Find(file => DoesFileMatchLabel(file, tmLabel, FileRegexPatterns.TILEMAP_REGEX));

                    if (string.IsNullOrEmpty(matchingTilemapFilePath))
                        return Result.Fail(
                            new TiledMapGraphValidationError(
                                $"Could not find Tilemap {tmLabel} referenced by Blueprint {bpLabel}."));

                    var tilemap = await DeserializeFileToJSON<TiledMap>(matchingTilemapFilePath);
                    if (tilemap.IsFailed)
                        return Result.Fail(tilemap.Errors);

                    templates.Add(tmLabel, tilemap.Value);

                    foreach (TiledMapSet ts in tilemap.Value.TileSets) {
                        string source = Path.GetFileName(ts.Source);
                        if (templateTileSets.ContainsKey(source))
                            continue;
                        
                        string? path = repo.TileSetFilePaths
                            .Find(file => DoesFileMatchLabel(file, source, FileRegexPatterns.TILESET_REGEX));

                        if (string.IsNullOrEmpty(path))
                            return Result.Fail(
                            new TiledMapGraphValidationError(
                                $"Could not find Tileset {source} referenced by Tilemap {tmLabel}."));

                        var tileset = await DeserializeFileToJSON<TiledSet>(path);
                        if (tileset.IsFailed)
                            return Result.Fail(tileset.Errors);

                        templateTileSets.Add(source, tileset.Value);
                    }
                }
            }
        }   

        return new TiledMapDependencyContent(
            templates,
            templateTileSets,
            definitions,
            blueprints);
    }

    private bool DoesFileMatchLabel(string file, string target, string pattern) {
        var match = Regex.Match(file, pattern);
        if (match.Success && match.Groups.Count > 1) 
            return match.Groups[0].Value == target ||  match.Groups[1].Value == target;

        return false;
    }

    private async Task<Result<T>> DeserializeFileToJSON<T>(string path) {
        using var fs = _fs.File.OpenRead(path);
        try {
            var res = await JsonSerializer.DeserializeAsync<T>(fs, _jsonSerializerOptions);

            return res ?? 
                throw new Exception($"Unknown issue occured trying to deserialize object from {path}");
        }
        catch (JsonException e) {
            _logger.LogError(e, e.Message);

            return Result.Fail(new TiledMapMalformedJSONError(e.Message));
        }
    }
}

public static class ListExtensions {
    public static Dictionary<string, T> ToDictionary<T>(this List<T> list, Func<T, string> kSelector) {
        var dictionary = new Dictionary<string, T>();
        foreach(var obj in list)
            dictionary.Add(kSelector(obj), obj);
    
        return dictionary;
    }
}

public static class FileRegexPatterns {
    public static readonly string DEFINITIONS_REGEX = @"(?i)definitions[.]json$";
    public static readonly string BLUEPRINTS_REGEX = @"(?i)blueprints[.]json$";
    public static readonly string GRAPH_REGEX = @"([a-zA-Z0-9\-_.]+).(?i)graph[.]json$";
    public static readonly string TILEMAP_REGEX = @"([a-zA-Z0-9\-_.]+).(?i)tilemap[.]json$";
    public static readonly string TILESET_REGEX = @"([a-zA-Z0-9\-_.]+).(?i)tileset[.]json$";
}

internal class FileRepository {
    public readonly string DefintionFilePath;
    public readonly string BlueprintFilePath;
    public readonly List<string> GraphFilePaths;
    public readonly List<string> TileMapFilePaths;
    public readonly List<string> TileSetFilePaths;

    public FileRepository(
        string defintionFilePath,
        string blueprintFilePath,
        List<string> graphFilePaths,
        List<string> tileMapFilePaths,
        List<string> tileSetFilePaths) {
        
        DefintionFilePath = defintionFilePath;
        BlueprintFilePath = blueprintFilePath;
        GraphFilePaths = graphFilePaths;
        TileMapFilePaths = tileMapFilePaths;
        TileSetFilePaths = tileSetFilePaths;
    }

    public static Result<FileRepository> FromDirectory(string directory, IFileSystem fs) {

        // TODO: This is quite nasty. Implement a custom recursive directory scan that checks for
        //       each file type as it scans so as we only scan once. 

        var definitions = GetFilesForRegex(directory, 
            new Regex(FileRegexPatterns.DEFINITIONS_REGEX), fs);
        if (definitions.Count == 0) 
            return Result.Fail(new TiledMapAssetsNotFoundError("Not room definitions found")); 

        var blueprints = GetFilesForRegex(directory, 
            new Regex(FileRegexPatterns.BLUEPRINTS_REGEX), fs);
        if (blueprints.Count == 0) 
            return Result.Fail(new TiledMapAssetsNotFoundError("Not room blueprints found")); 

        var graphs = GetFilesForRegex(directory, 
            new Regex(FileRegexPatterns.GRAPH_REGEX), fs);
        if (graphs.Count == 0) 
            return Result.Fail(new TiledMapAssetsNotFoundError("Not graphs found")); 

        var tilemaps = GetFilesForRegex(directory, 
            new Regex(FileRegexPatterns.TILEMAP_REGEX), fs);
        if (tilemaps.Count == 0) 
            return Result.Fail(new TiledMapAssetsNotFoundError("Not tile maps found")); 

        var tilesets = GetFilesForRegex(directory, 
            new Regex(FileRegexPatterns.TILESET_REGEX), fs);
        if (tilemaps.Count == 0) 
            return Result.Fail(new TiledMapAssetsNotFoundError("Not tile sets found")); 
        
        return new FileRepository(
            definitions[0], 
            blueprints[0], 
            graphs, 
            tilemaps,
            tilesets);
    }

    private static List<string> GetFilesForRegex(string directory, Regex regex, IFileSystem fs) {
        return fs.Directory
            .GetFiles(directory, "*.json", SearchOption.AllDirectories)
            .Where(path => regex.IsMatch(path))
            .ToList<string>();
    }
}

