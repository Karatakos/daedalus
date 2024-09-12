namespace Daedalus.Server;

using System.IO.Abstractions;

using Microsoft.Extensions.Logging;
using Daedalus.Core;
using Daedalus.Core.Tiled.Maps;
using Daedalus.Core.Tiled.Procedural;
using Daedalus.Core.Tiled.Procedural.ContentProviders;

public class SmartTiledMapGenerator {
    public bool Success { get; private set; } = false;
    public bool Fail { get; private set; } = false;
    public string Error { get; private set; }
    public TiledMapDungen Map { get; private set; }

    private int _retries;
    private string _contentDir;
    private FileSystem _fs;

    public SmartTiledMapGenerator (FileSystem fs, string contentDir, int retries = 0) {
        _retries = retries;
        _contentDir = contentDir;
        _fs = fs;
    }

    public async void GenerateAsync(string graph) {
        TiledMapDungenBuilder builder = new (new LocalDiskContentProvider(DS.LogFactory, _fs, _contentDir), DS.LogFactory);

        var res = await builder.BuildAsync(graph, 
            new TiledMapDungenBuilderProps() { EmptyTileGid = 30, DoorWidth = 2 });

        if (res.IsFailed) {
            DS.Log.LogError(res.Errors[0].Message);

            if (_retries > 0) {
                _retries--;

                GenerateAsync(graph);
            }
            else {
                Error = res.Errors[0].Message;
                Fail = true;

                return;
            }
        }

        Success = true;
        Map = res.Value;
    }
}

