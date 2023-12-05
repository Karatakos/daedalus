namespace Dungen.Generator.Tests;

using System.Collections.Generic;
using NUnit.Framework;

using Dungen;

[TestFixture]
public class Layouts
{
    private RoomBlueprint _square;

    [SetUp]
    public void Setup()
    {
        float x = 20/2;
        float y = 20/2;

        _square = new RoomBlueprint(
            points: new List<Vector2F>(
                new Vector2F[] {
                    new Vector2F(x, y), 
                    new Vector2F(x, -y),
                    new Vector2F(-x, -y),
                    new Vector2F(-x, y)}));
    }

    [Test]
    public void RoomDistance()
    {
        Room room1 = new Room(_square, RoomType.Normal);
        Room room2 = new Room(_square, RoomType.Normal);
        room2.Translate(new Vector2F(11, 11));

        Assert.That(
            Room.ComputeRoomDistance(room1, room2), 
            Is.EqualTo(0.0F));
    }

    [Test]
    public void RoomCollisionArea()
    {
        Room room1 = new Room(_square, RoomType.Normal);
        Room room2 = new Room(_square, RoomType.Normal);

        room2.Translate(new Vector2F(2, 2));

        Assert.That(
            Room.ComputeRoomCollisionArea(room1, room2),
            Is.EqualTo(400F));

        room2.Translate(-room2.GetCenter());
        room2.Translate(new Vector2F(10,10));

        Assert.That(
            Room.ComputeRoomCollisionArea(room1, room2),
            Is.EqualTo(399.999969F));
    }

    [Test]
    public void RoomWallContactArea()
    {
        Room room1 = new Room(_square, RoomType.Normal);
        Room room2 = new Room(_square, RoomType.Normal);

        room2.Translate(new Vector2F(10, 5));

        Assert.That(
            Room.ComputeRoomContactArea(room1, room2),
            Is.EqualTo(80F));
    }
}