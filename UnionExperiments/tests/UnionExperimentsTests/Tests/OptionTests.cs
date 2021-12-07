using NUnit.Framework;
using UnionExperiments;
using DiscriminatedUnions;
using static NUnit.Framework.Assert;

namespace UnionExperimentsTests
{
    [TestFixture]
    public class OptionTests
    {
        [Test]
        public void ForOptionOfInt_CanCreateSomeInt_GetItsValue_AndPatternMatchIt()
        {
            var option = Option<int>.AsSome(42);

            Multiple(() => {
                IsTrue(option is { Case: Type<Some<int>> });
                AreEqual(42, option.Some.Value);
                AreEqual(42, option switch {
                    { Case: Type<Some<int>>, Some.Value: var x } => x,
                    _ => 0
                });
            });
        }

        [Test]
        public void ForOptionOfString_CanCreateSomeInt_GetItsValue_AndPatternMatchIt()
        {
            var option = Option<string>.AsSome("yellow");

            Multiple(() => {
                AreEqual("yellow", option.Some.Value);
                AreEqual("yellow", option switch {
                    { Case: Type<Some<string>>, Some.Value: var x } => x,
                    _ => ""
                });
            });
        }
    }
}