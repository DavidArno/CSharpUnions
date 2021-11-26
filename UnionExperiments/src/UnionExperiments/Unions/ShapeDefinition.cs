
using DiscriminatedUnions;

namespace UnionExperiments.Unions;

[DiscriminatedUnion]
record ShapeDefinition
{
    record Point();

    record Rectangle(double Width, double Length);

    record Circle(double Radius);
}

