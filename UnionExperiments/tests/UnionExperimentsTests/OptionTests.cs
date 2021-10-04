using NUnit.Framework;
using UnionExperiments.Glue;
using UnionExperiments.Unions;
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
                AreEqual(42, option.Value);
                AreEqual(42, option switch
                {
                    (Type<Some<int>>, int x) => x,
                    _ => 0
                });

                AreEqual(42, option switch
                {
                    { Case: Type<Some<int>>, Some.Value: var x }  => x,
                    _ => 0
                });
            });
        }

        [Test]
        public void ForOptionOfString_CanCreateSomeInt_GetItsValue_AndPatternMatchIt()
        {
            var option = Option<string>.AsSome("yellow");

            Multiple(() => {
                IsTrue(option is (Type<Some<string>>, _));
                AreEqual("yellow", option.Value);
                AreEqual("yellow", option switch
                {
                    (Type<Some<string>>, string x) => x,
                    _ => 0
                });
            });
        }
    }
}