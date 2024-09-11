namespace Daedalus.Core.Commands;

using Daedalus.Core.Tiled.Maps;

using MemoryPack;

[MemoryPackable]
public partial record LoadMapCmd (TiledMapDungen Map) : ICommand {
    public CommandType Type { get => CommandType.LoadMap; }
}