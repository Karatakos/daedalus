namespace Daedalus.Core.Tiled.Procedural.Errors;

using FluentResults;
public class TiledMapDoorInstallerValidationError(string message = "One or more installer options are invalid"): Error(message);