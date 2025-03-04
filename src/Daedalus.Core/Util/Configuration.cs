namespace Daedalus.Core;

public interface IConfiguration {
    public int Port { get; }
    public MatchmakerToken MatchmakerToken { get; set; }
}

public class StaticConfiguration : IConfiguration {
    public int Port { get => 9050; }
    public MatchmakerToken MatchmakerToken { get; set; }
}