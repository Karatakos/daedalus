namespace Daedalus.Server;

using Daedalus.Core.Tiled.Maps;
using Daedalus.Core.Tiled.Procedural;
using Daedalus.Core.Tiled.Procedural.ContentProviders;

using System.Text.Json.Serialization;
using System.Text.Json;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;

using FluentResults;

class Program
{
    static async Task Main(string[] args)
    {
        var loggerFactory = LoggerFactory.Create((builder) => {
            builder.AddSimpleConsole(options => options.SingleLine = true);
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        logger.LogInformation("Generating a tile map...");

        string workingDir = Path.Combine(Directory.GetCurrentDirectory(), "../../content/maps");
        string outputDir = Path.Combine(workingDir, "../tmp/");

        TiledMapDungenBuilder builder = new TiledMapDungenBuilder(
            new LocalDiskContentProvider(
                loggerFactory, new FileSystem(), workingDir), loggerFactory);

        try {
            var res = await builder.BuildAsync("simple-loop", 
                new TiledMapDungenBuilderProps() { EmptyTileGid = 30, DoorWidth = 2 });

            if (res.IsFailed) {
                logger.LogError(res.Errors[0].Message);
                return;
            }

            logger.LogInformation("Writing map to disk ...");

            var mapJson = JsonSerializer.Serialize<TiledMapDungen>(res.Value);
            File.WriteAllText(Path.Combine(outputDir, "generatedmap.json"), mapJson);
        }
        catch (Exception ex) {
            logger.LogError(ex, ex.Message);
        }
    }
}
