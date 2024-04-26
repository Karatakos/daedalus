namespace Daedalus.Core.Tiled.Procedural.Extensions;

using Dungen;

public static class RoomExtensions {
    public static void SnapToGrid(this Room room) {
        for (int i=0; i<room.Doors.Count; i++) {
            var start = new Vector2F(
                (int)Math.Round(room.Doors[i].Position.Item1.x, MidpointRounding.AwayFromZero), 
                (int)Math.Round(room.Doors[i].Position.Item1.y, MidpointRounding.AwayFromZero));

            var end = new Vector2F(
                (int)Math.Round(room.Doors[i].Position.Item2.x, MidpointRounding.AwayFromZero), 
                (int)Math.Round(room.Doors[i].Position.Item2.y, MidpointRounding.AwayFromZero));

            room.Doors[i] = new Door((start, end), room.Doors[i].ConnectingRoom, room.Doors[i].DefaultAccess);
        }

        for (int i=0; i<room.Points.Count; i++) {
            var newVec = new Vector2F(
                (int)Math.Round(room.Points[i].x, MidpointRounding.AwayFromZero), 
                (int)Math.Round(room.Points[i].y, MidpointRounding.AwayFromZero));

            room.Points[i] = newVec;
        }

        room.Position = room.GetCenter();
    }
}