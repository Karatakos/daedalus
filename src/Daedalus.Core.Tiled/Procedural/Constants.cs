namespace Daedalus.Core.Tiled.Procedural;

public static class Constants {
    // Bits on the far end of the 32-bit global tile ID are used for tile flag
    //
    public const uint FLIPPED_HORIZONTALLY_FLAG  = 0x80000000;
    public const uint FLIPPED_VERTICALLY_FLAG    = 0x40000000;
    public const uint FLIPPED_DIAGONALLY_FLAG    = 0x20000000;
    public const uint ROTATED_HEXAGONAL_120_FLAG = 0x10000000;
}
