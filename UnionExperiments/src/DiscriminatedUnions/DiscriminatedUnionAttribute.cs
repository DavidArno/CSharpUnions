namespace DiscriminatedUnions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class DiscriminatedUnionAttribute : Attribute
{
    public static string UnionDefinitionAttributeName { get; } =
        nameof(DiscriminatedUnionAttribute).Replace("Attribute", "");
}