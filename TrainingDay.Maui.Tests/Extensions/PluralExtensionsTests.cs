using System.Globalization;
using TrainingDay.Maui.Extensions;

namespace TrainingDay.Maui.Tests.Extensions
{
    [TestFixture]
    public class PluralExtensionsTests
    {
        private static readonly CultureInfo Russian = new CultureInfo("ru-RU");

        [Test]
        [TestCase(1, "1 упражнение")]
        [TestCase(21, "21 упражнение")]
        [TestCase(101, "101 упражнение")]
        [TestCase(2, "2 упражнения")]
        [TestCase(4, "4 упражнения")]
        [TestCase(22, "22 упражнения")]
        [TestCase(0, "0 упражнений")]
        [TestCase(5, "5 упражнений")]
        [TestCase(11, "11 упражнений")]
        [TestCase(12, "12 упражнений")]
        [TestCase(14, "14 упражнений")]
        [TestCase(111, "111 упражнений")]
        [TestCase(112, "112 упражнений")]
        public void ToPluralString_Russian_UsesOneFewMany(int count, string expected)
        {
            var result = count.ToPluralString(Russian, "{0} упражнение", "{0} упражнения", "{0} упражнений");

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase("en-US", 1, "1 exercise")]
        [TestCase("en-US", 0, "0 exercises")]
        [TestCase("en-US", 2, "2 exercises")]
        [TestCase("en-US", 21, "21 exercises")]
        [TestCase("de-DE", 1, "1 exercise")]
        [TestCase("de-DE", 3, "3 exercises")]
        public void ToPluralString_OtherLanguages_UseOneOrOther(string cultureName, int count, string expected)
        {
            var result = count.ToPluralString(new CultureInfo(cultureName), "{0} exercise", "{0} exercises (few)", "{0} exercises");

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
