namespace Daedalus.Core.Tiled.Procedural.ContentProviders;

using Dungen;
using FluentResults;

public interface IContentProvider {
    Task<Result<TiledMapContent>> LoadAsync(string graph);
}