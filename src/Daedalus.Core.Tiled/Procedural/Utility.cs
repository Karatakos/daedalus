namespace Daedalus.Core.Tiled.Procedural;

public static class Utility {
    public static uint ConvertToUInt(float value) {
        // Unsure how we want to handle this yet.
        // e.g. 10.8 returns 10, 11, or context dependent. Round then cast always gives us 11.
        //
        return (uint)(value + 0.5f);
    }
}
