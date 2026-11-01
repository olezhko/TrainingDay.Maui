using System.Globalization;
using TrainingDay.Maui.Extensions;

namespace TrainingDay.Maui.Tests.Extensions
{
    [TestFixture]
    public class DateTimeExtensionsTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 26, 12, 0, 0);
        private CultureInfo originalUICulture;

        [SetUp]
        public void SetUp()
        {
            originalUICulture = CultureInfo.CurrentUICulture;
        }

        [TearDown]
        public void TearDown()
        {
            CultureInfo.CurrentUICulture = originalUICulture;
        }

        [Test]
        [TestCase(2, "2 дня назад")]
        [TestCase(4, "4 дня назад")]
        [TestCase(5, "5 дней назад")]
        [TestCase(11, "11 дней назад")]
        [TestCase(13, "13 дней назад")]
        [TestCase(14, "2 недели назад")]
        [TestCase(28, "4 недели назад")]
        public void ToRelativeDateString_Russian_UsesPluralForms(int daysAgo, string expected)
        {
            var culture = new CultureInfo("ru-RU");
            CultureInfo.CurrentUICulture = culture;

            var result = Now.AddDays(-daysAgo).ToRelativeDateString(Now, culture);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        [TestCase(2, "2 days ago")]
        [TestCase(13, "13 days ago")]
        [TestCase(14, "2 weeks ago")]
        public void ToRelativeDateString_English_UsesPluralForms(int daysAgo, string expected)
        {
            var culture = new CultureInfo("en-US");
            CultureInfo.CurrentUICulture = culture;

            var result = Now.AddDays(-daysAgo).ToRelativeDateString(Now, culture);

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
