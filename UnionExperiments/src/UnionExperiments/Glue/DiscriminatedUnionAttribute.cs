using System;

namespace UnionExperiments.Glue;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class DiscriminatedUnionAttribute : Attribute { }