namespace Daedalus.Server;

using Daedalus.Core;

using Microsoft.Extensions.Logging;

class EntryPoint
{
    public static void Main(string[] args)
    {
        new GameServer(
            LoggerFactory.Create((builder) => {
                builder.AddSimpleConsole(options => options.SingleLine = true);
                builder.SetMinimumLevel(LogLevel.Information);}),
            new MatchmakerToken("somekey"),
            Path.Combine(Directory.GetCurrentDirectory(), "../../content/")).Run();
    }
}