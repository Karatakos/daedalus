namespace Daedalus.Tiled.ContentProviders;

using Dungen;
using FluentResults;

public interface IContentProvider {
    Task<Result<TiledMapContent>> LoadAsync(string graph);
}