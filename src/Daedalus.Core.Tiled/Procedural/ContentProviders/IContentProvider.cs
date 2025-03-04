namespace Daedalus.Core.Tiled.Procedural.ContentProviders;

using GraphToGrid;
using FluentResults;

public interface IContentProvider {
    Task<Result<TiledMapContent>> LoadAsync(string graph);
}