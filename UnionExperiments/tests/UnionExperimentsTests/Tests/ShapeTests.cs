using NUnit.Framework;
using UnionExperiments;

namespace UnionExperimentsTests;

#pragma warning disable CS8509

[TestFixture]
public class ShapeTests
{
    [Test]
    public void ForRectangeCase_CanPatternMatchWidthAndLength()
    {
        var shape = Shape.AsRectangle(10, 20);

        var area = shape switch
        {
            { Case: Shape.PointCase } => 0.0,
            { Case: Shape.RectangleCase, Rectangle: { Width: var w, Length: var l } } => w * l,
            { Case: Shape.CircleCase, Circle.Radius: var r } => Math.PI * r * r
        };

        Assert.AreEqual(200, area);
    }

    [Test]
    public void ForCirleCase_CanPatternMatchRadius()
    {
        var shape = Shape.AsCircle(10);

        var area = shape switch
        {
            { Case: Shape.PointCase } => 0.0,
            { Case: Shape.RectangleCase, Rectangle: { Width: var w, Length: var l } } => w * l,
            { Case: Shape.CircleCase, Circle.Radius: var r } => Math.PI * r * r
        };

        Assert.AreEqual(314, (int)area);
    }

    [Test]
    public void ForPointCase_CanPatternMatchCase()
    {
        var shape = Shape.AsPoint();

        var area = shape switch
        {
            { Case: Shape.PointCase } => 0.0,
            { Case: Shape.RectangleCase, Rectangle: { Width: var w, Length: var l } } => w * l,
            { Case: Shape.CircleCase, Circle.Radius: var r } => Math.PI * r * r
        };

        Assert.AreEqual(0, area);
    }
}