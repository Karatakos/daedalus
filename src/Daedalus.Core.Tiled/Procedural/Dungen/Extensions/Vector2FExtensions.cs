namespace Daedalus.Core.Tiled.Procedural.Extensions;

using Microsoft.Xna.Framework;

using Dungen;

public static class Vector2FExtensions {
    public static Vector2 ToVector2(this Vector2F vec) {
        return new Vector2((int)(vec.x + 0.5f), (int)(vec.y + 0.5f));
    }
}