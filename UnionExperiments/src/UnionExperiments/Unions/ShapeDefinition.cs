global using static System.Math;

using DiscriminatedUnions;

using ListOfInt = System.Collections.Generic.List<int>;

namespace UnionExperiments.Unions {
    using System;


    record R<T>(List<T> L);

[DiscriminatedUnion]
record ShapeDefinition
{
    record Point();

    record Rectangle(double Width, double Length);

    record Circle(double Radius);

//    record struct Cabbage<T>(Point? Point, R<T> X, ListOfInt Li);
}
}