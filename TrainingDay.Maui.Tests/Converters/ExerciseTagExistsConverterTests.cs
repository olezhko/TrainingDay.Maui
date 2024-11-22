using TrainingDay.Maui.Converters;
using TrainingDay.Common;
using System.Globalization;

namespace TrainingDay.Maui.Tests.Converters
{
    [TestFixture]
    public class ExerciseTagExistsConverterTests
    {
        private ExerciseTagExistsConverter _converter;

        [SetUp]
        public void SetUp()
        {
            _converter = new ExerciseTagExistsConverter();
        }

        [Test]
        [TestCase(ExerciseTags.CanDoAtHome, true, new ExerciseTags[] { ExerciseTags.CanDoAtHome })]
        [TestCase(ExerciseTags.CanDoAtHome, false, new ExerciseTags[] { })]
        [TestCase(ExerciseTags.BarbellExist, true, new ExerciseTags[] { ExerciseTags.CanDoAtHome, ExerciseTags.BarbellExist })]
        [TestCase(ExerciseTags.BarbellExist, false, new ExerciseTags[] { ExerciseTags.CanDoAtHome })]
        public void ConvertBack_AddsOrRemovesTagBasedOnBoolean(ExerciseTags tag, bool addTag, ExerciseTags[] expectedTags)
        {
            var initialTags = new List<ExerciseTags> { ExerciseTags.CanDoAtHome };

            var result = (List<ExerciseTags>)_converter.ConvertBack(addTag, typeof(List<ExerciseTags>), tag, CultureInfo.InvariantCulture);

            CollectionAssert.AreEqual(expectedTags, result);
        }

        [Test]
        [TestCase(ExerciseTags.CanDoAtHome, true, new ExerciseTags[] { ExerciseTags.CanDoAtHome })]
        [TestCase(ExerciseTags.CanDoAtHome, false, new ExerciseTags[] { })]
        [TestCase(ExerciseTags.BarbellExist, true, new ExerciseTags[] { ExerciseTags.CanDoAtHome, ExerciseTags.BarbellExist })]
        [TestCase(ExerciseTags.BarbellExist, false, new ExerciseTags[] { ExerciseTags.CanDoAtHome })]
        public void ConvertBack_WorksWhenInitialTagsListIsNull(ExerciseTags tag, bool addTag, ExerciseTags[] expectedTags)
        {
            List<ExerciseTags> initialTags = null;

            var result = (List<ExerciseTags>)_converter.ConvertBack(addTag, typeof(List<ExerciseTags>), tag, CultureInfo.InvariantCulture);

            CollectionAssert.AreEqual(expectedTags, result);
        }

        [Test]
        [TestCase(ExerciseTags.CanDoAtHome, new ExerciseTags[] { ExerciseTags.CanDoAtHome }, true)]
        [TestCase(ExerciseTags.CanDoAtHome, new ExerciseTags[] { }, false)]
        [TestCase(ExerciseTags.BarbellExist, new ExerciseTags[] { ExerciseTags.CanDoAtHome }, false)]
        [TestCase(ExerciseTags.BarbellExist, new ExerciseTags[] { ExerciseTags.CanDoAtHome, ExerciseTags.BarbellExist }, true)]
        public void Convert_ReturnsCorrectBoolean(ExerciseTags tag, ExerciseTags[] tags, bool expected)
        {
            var result = (bool)_converter.Convert(new List<ExerciseTags>(tags), typeof(bool), tag, CultureInfo.InvariantCulture);

            Assert.AreEqual(expected, result);
        }
    }
}
