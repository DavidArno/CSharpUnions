using NUnit.Framework;
using UnionExperiments.Unions;
using DiscriminatedUnions;

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
            { Case: Type<Point> } => 0.0,
            { Case: Type<Rectangle>, Rectangle: { Width: var w, Length: var l } } => w * l,
            { Case: Type<Circle>, Circle.Radius: var r } => Math.PI * r * r
        };

        Assert.AreEqual(200, area);
    }

    [Test]
    public void ForCirleCase_CanPatternMatchRadius()
    {
        var shape = Shape.AsCircle(10);

        var area = shape switch
        {
            { Case: Type<Point> } => 0.0,
            { Case: Type<Rectangle>, Rectangle: { Width: var w, Length: var l } } => w * l,
            { Case: Type<Circle>, Circle.Radius: var r } => Math.PI * r * r
        };

        Assert.AreEqual(314, (int)area);
    }

    [Test]
    public void ForPointCase_CanPatternMatchCase()
    {
        var shape = Shape.AsPoint();

        var area = shape switch
        {
            { Case: Type<Point> } => 0.0,
            { Case: Type<Rectangle>, Rectangle: { Width: var w, Length: var l } } => w * l,
            { Case: Type<Circle>, Circle.Radius: var r } => Math.PI * r * r
        };

        Assert.AreEqual(0, area);
    }
}