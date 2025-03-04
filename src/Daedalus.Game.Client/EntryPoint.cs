namespace Daedalus.Game.Client;

using Microsoft.Extensions.Logging;

class EntryPoint
{
    public static void Main(string[] args)
    {
        var logFactory = LoggerFactory.Create((builder) => {
            builder.AddSimpleConsole(options => options.SingleLine = true);
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var game = new GameClient(logFactory);

        game.Run();
    }
}

