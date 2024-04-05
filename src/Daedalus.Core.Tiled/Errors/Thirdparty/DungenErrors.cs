namespace Daedalus.Tiled.Errors;

using FluentResults;

public class DungenGraphValidationError(string message = "Graph validation failed"): Error(message);
public class DungenSolutioNotFoundError(string message = "Failed to find a solution to the input graph"): Error(message);