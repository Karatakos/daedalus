namespace Daedalus.Server;

using Daedalus.Tiled;
using Daedalus.Tiled.ContentProviders;

using System.Text.Json.Serialization;
using System.Text.Json;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;

using FluentResults;

class Program
{
    static async Task Main(string[] args)
    {
        var loggerFactory = LoggerFactory.Create((builder) => 
            builder.AddSimpleConsole(options => options.SingleLine = true));

        var logger = loggerFactory.CreateLogger<Program>();

        logger.LogInformation("Generating a tile map...");

        string workingDir = Path.Combine(Directory.GetCurrentDirectory(), "../../../maps");
        string outputDir = Path.Combine(workingDir, "tmp/maps");

        TiledMapBuilder builder = new TiledMapBuilder(
            new LocalDiskContentProvider(
                loggerFactory, new FileSystem(), workingDir), loggerFactory);

        try {
            var res = await builder.BuildAsync("simple-loop", new TiledMapBuilderProps());
            if (res.IsFailed) {
                logger.LogError(res.Errors[0].Message);
                return;
            }

            logger.LogInformation("Writing map to disk ...");

            var mapJson = JsonSerializer.Serialize<TiledMap>(res.Value, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(Path.Combine(outputDir, "generatedmap.json"), mapJson);
        }
        catch (Exception ex) {
            logger.LogError(ex, ex.Message);
        }
    }
}
