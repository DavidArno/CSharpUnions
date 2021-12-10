using DiscriminatedUnions;
using NUnit.Framework;

namespace UnionExperimentsTests;

[TestFixture]
public static class SizeTests
{
    [Test]
    public static void TypeOfT_IsExpectedSize_AsAChangeWillBreakStructBasedDUPacking()
    {
        var size = SizeOf<Type<Type<string>>>();
        Assert.AreEqual(1, size);
    }

    private static unsafe int SizeOf<T>() where T : unmanaged => sizeof(T);
}