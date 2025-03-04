using System.Windows.Input;

using MemoryPack;

namespace Daedalus.Core.Commands;

public enum CommandType: byte {
    LoadMap,
    NotifyPlayer,
    KickPlayer,
    EndGame
}

[MemoryPackable]
/*
*  Ensure we define all concrete commands here
*/
[MemoryPackUnion(48, typeof(LoadMapCmd))]
public partial interface ICommand {
    public CommandType Type { get; }
}