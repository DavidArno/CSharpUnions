using DiscriminatedUnions;

namespace UnionExperiments;

[DiscriminatedUnion]
struct OptionDefinition
{
    record None();

    record Some<T>(T Value);
}

