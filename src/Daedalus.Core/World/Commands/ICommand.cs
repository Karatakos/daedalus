using System.Windows.Input;

namespace Daedalus.Core.Commands;

public enum CommandType: byte {
    LoadMap,
    NotifyPlayer,
    KickPlayer,
    EndGame
}

public interface ICommand {
    public CommandType Type { get; }
}