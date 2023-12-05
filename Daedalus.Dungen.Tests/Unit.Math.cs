namespace Dungen.Generator.Tests;

using NUnit.Framework;

using Dungen;

[TestFixture]
public class Math
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void IntersectingLines()
    {
        Vector2F intersect;

        Assert.That(
            Math2D.FindIntersection(
                new Vector2F(0,0), 
                new Vector2F(2,8),
                new Vector2F(8,0),
                new Vector2F(0,20), 
                out intersect)
            , Is.EqualTo(false));

        Assert.That(
            Math2D.FindIntersection(
                new Vector2F(0,10), 
                new Vector2F(2,0),
                new Vector2F(10,0),
                new Vector2F(0,5), 
                out intersect)
            , Is.EqualTo(true));

        // Parallel (vertical lines)
        //
        Assert.That(
            Math2D.FindIntersection(
                new Vector2F(0,0), 
                new Vector2F(0,10),
                new Vector2F(2,0),
                new Vector2F(2,10), 
                out intersect)
            , Is.EqualTo(false));

        // Parallel (diagonal lines)
        //
        Assert.That(
            Math2D.FindIntersection(
                new Vector2F(0,0), 
                new Vector2F(5,5),
                new Vector2F(2,0),
                new Vector2F(7,5), 
                out intersect)
            , Is.EqualTo(false));

        // Colinear (overlap)
        //
        Assert.That(
            Math2D.FindIntersection(
                new Vector2F(0,0), 
                new Vector2F(5,5),
                new Vector2F(2,2),
                new Vector2F(7,7), 
                out intersect)
            , Is.EqualTo(false));

        // Colinear (no overlap)
        //
        Assert.That(
            Math2D.FindIntersection(
                new Vector2F(0,0), 
                new Vector2F(5,5),
                new Vector2F(7,7),
                new Vector2F(10,10), 
                out intersect)
            , Is.EqualTo(false));
    }
}