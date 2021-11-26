using NUnit.Framework;

namespace UnionExperimentsTests;

[TestFixture]
public class TypeTests
{
    [Test]
    public void TypeTest()
    {

    }

    private static bool TypeIsCool<T>()
        => typeof(T) switch {
            _ when typeof(T).Equals(typeof(string)) => true,
            _ when typeof(T).Equals(typeof(bool)) => false,
            _ => throw new NotSupportedException($"{typeof(T).Name} is not supported")
        };
}