namespace DiscriminatedUnions;

public readonly struct Type<T>
{
    private static readonly Type<T> Instance = new();
    public static Type<T> Value { get; } = Instance;
}