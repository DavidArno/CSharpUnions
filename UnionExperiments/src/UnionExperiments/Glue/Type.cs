namespace UnionExperiments.Glue;

public sealed class Type<T>
{
    private Type() { }

    public static Type<T> Instance { get; } = new Type<T>();
}