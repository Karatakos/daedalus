namespace Dungen.Generator.Tests;

using System.Collections.Generic;
using NUnit.Framework;

using Dungen.Generator;

[TestFixture]
public class ConfigSpaces
{
    private RoomBlueprint _hexL;

    [SetUp]
    public void Setup() {
        float x = 100/2;
        float y = 100/2;

        _hexL = new RoomBlueprint(
            points: new List<Vector2F>(
                new Vector2F[] {
                    new Vector2F(0, y), 
                    new Vector2F(0, 0),
                    new Vector2F(x, 0),
                    new Vector2F(x, -y),
                    new Vector2F(-x, -y),
                    new Vector2F(-x, y)}));
    }

    [Test]
    public void SpaceGeneratedFromShapes()
    {
        Room fixedRoom = new Room(_hexL, RoomType.Normal);
        Room movingRoom = new Room(_hexL, RoomType.Normal);

        ConfigSpacesBuilder csBuilder = new ConfigSpacesBuilder();

        csBuilder.TryFromRoom(fixedRoom, movingRoom, out ConfigSpace cspace);

        Assert.That(cspace.Lines.Count, Is.EqualTo(12));
    }

    [Test]
    public void SpaceGeneratedFromConfigSpaceIntersection()
    {
        ConfigSpacesBuilder csBuilder = new ConfigSpacesBuilder();

        Room fixedRoom = new Room(_hexL, RoomType.Normal);
        Room movingRoom = new Room(_hexL, RoomType.Normal);

        // Test too simplified as both shapes overlap so will intersect on every line
        // 
        // Note: Line's also intersect at the corners so we get 16 not the expected 8!
        //
        Room fixedRoom2 = new Room(_hexL, RoomType.Normal);

        csBuilder.TryFromRoom(fixedRoom, movingRoom, out ConfigSpace cspace1);
        csBuilder.TryFromRoom(fixedRoom2, movingRoom, out ConfigSpace cspace2);

        ConfigSpace cspace = csBuilder.FromConfigSpace(cspace1, cspace2);

        Assert.That(cspace.Lines.Count, Is.EqualTo(8));
    }
}