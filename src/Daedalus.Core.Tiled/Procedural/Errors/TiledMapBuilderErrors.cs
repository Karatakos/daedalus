namespace Daedalus.Core.Tiled.Procedural.Errors;

using FluentResults;
public class TiledMapGraphValidationError(string message = "Missing graph or dependency for graph"): Error(message);
public class TiledMapMalformedJSONError(string message = "Invalid JSON for Graph asset"): Error(message);
public class TiledMapAssetsNotFoundError(string message = "Missing graph or dependency assets for graph"): Error(message);
public class TiledMapBuilerTemplateTileSetsOrderError(string message = "Two or more tile map templates have tile sets with differing order (first GID will be different)"): Error(message);

