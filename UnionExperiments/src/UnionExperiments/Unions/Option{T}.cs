using UnionExperiments.Glue;

namespace UnionExperiments.Unions;

[DiscriminatedUnion]
[NoTryGet]
public readonly partial struct Option<T>
{
    public static partial Option<T> AsNone();

    public static partial Option<T> AsSome(T value);

    public T Value => _validCase == Cases.SomeCase
        ? _caseSome.Value
        : throw new InvalidOperationException("Option contains no value.");

}

