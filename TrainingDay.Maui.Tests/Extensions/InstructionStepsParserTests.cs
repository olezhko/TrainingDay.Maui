using TrainingDay.Common.Models;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;

namespace TrainingDay.Maui.Tests.Extensions
{
    [TestFixture]
    public class InstructionStepsParserTests
    {
        private const string StepFormat = "Step {0} of {1}";
        private static readonly ExerciseTags[] RepsTags = [ExerciseTags.ExerciseByReps];
        private static readonly ExerciseTags[] TimeTags = [ExerciseTags.ExerciseByTime];

        private static IReadOnlyList<InstructionStep> Parse(string? text, IEnumerable<ExerciseTags>? tags = null)
        {
            return InstructionStepsParser.ParseExecution(text, tags ?? RepsTags, StepFormat);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Parse_NoText_ReturnsNoSteps(string? text)
        {
            Assert.That(Parse(text), Is.Empty);
        }

        [Test]
        public void Parse_SeveralSentences_NumbersEachOne()
        {
            var steps = Parse(" Lower the bar. Press it up! Hold it? ");

            Assert.That(steps.Select(step => step.Kind), Has.All.EqualTo(InstructionStepKind.Numbered));
            Assert.That(steps.Select(step => step.Text), Is.EqualTo(new[] { "Lower the bar.", "Press it up!", "Hold it?" }));
            Assert.That(steps.Select(step => step.Marker), Is.EqualTo(new[] { "1", "2", "3" }));
            Assert.That(steps[1].AccessibilityLabel, Is.EqualTo("Step 2 of 3"));
        }

        [Test]
        public void Parse_SingleSentence_IsPlain()
        {
            var steps = Parse("Hold the position.");

            Assert.That(steps, Has.Count.EqualTo(1));
            Assert.That(steps[0].Kind, Is.EqualTo(InstructionStepKind.Plain));
            Assert.That(steps[0].Text, Is.EqualTo("Hold the position."));
        }

        [TestCase("Repeat.")]
        [TestCase("Press the bar up. Repeat.")]
        public void Parse_OneActionAtMost_KeepsWholeTextPlain(string text)
        {
            var steps = Parse(text);

            Assert.That(steps, Has.Count.EqualTo(1));
            Assert.That(steps[0].Kind, Is.EqualTo(InstructionStepKind.Plain));
            Assert.That(steps[0].Text, Is.EqualTo(text));
        }

        [TestCase("Repeat.", "Repeat")]
        [TestCase("repeat", "repeat")]
        [TestCase("Повторите.", "Повторите")]
        [TestCase("Повторите движение.", "Повторите движение")]
        public void Parse_ClosingRepeat_BecomesRepeatRowAndIsNotCounted(string closing, string expected)
        {
            // Lowercase "repeat" has to come after a line break to be its own sentence.
            var steps = Parse($"Lower the bar. Press it up.\n{closing}");

            Assert.That(steps, Has.Count.EqualTo(3));
            Assert.That(steps[1].AccessibilityLabel, Is.EqualTo("Step 2 of 2"));
            Assert.That(steps[2].Kind, Is.EqualTo(InstructionStepKind.Repeat));
            Assert.That(steps[2].Text, Is.EqualTo(expected));
        }

        [Test]
        public void Parse_RepeatWithDetails_StaysANumberedStep()
        {
            var steps = Parse("Lower the bar. Repeat for the recommended number of repetitions.");

            Assert.That(steps.Select(step => step.Kind), Has.All.EqualTo(InstructionStepKind.Numbered));
        }

        [Test]
        public void Parse_RepeatInTheMiddle_StaysANumberedStep()
        {
            var steps = Parse("Lower the bar. Repeat. Rest.");

            Assert.That(steps.Select(step => step.Kind), Has.All.EqualTo(InstructionStepKind.Numbered));
        }

        [Test]
        public void Parse_TimedExercise_UsesBullets()
        {
            var steps = Parse("Engage your core. Hold the position.", TimeTags);

            Assert.That(steps.Select(step => step.Kind), Has.All.EqualTo(InstructionStepKind.Bullet));
            Assert.That(steps.Select(step => step.AccessibilityLabel), Has.All.Empty);
        }

        [Test]
        public void Parse_NullTags_NumbersSteps()
        {
            var steps = InstructionStepsParser.ParseExecution("One. Two.", null, StepFormat);

            Assert.That(steps.Select(step => step.Kind), Has.All.EqualTo(InstructionStepKind.Numbered));
        }

        [TestCase("Lift 1.5 kg up. Lower it.", "Lift 1.5 kg up.")]
        [TestCase("Держите спину, т.е. не сутультесь. Опустите руки.", "Держите спину, т.е. не сутультесь.")]
        [TestCase("Use a light weight, e.g. 5 kg plates, first. Lift it.", null)]
        public void Parse_DotsInsideSentence_DoNotSplit(string text, string? firstStep)
        {
            var steps = Parse(text);

            if (firstStep != null)
            {
                Assert.That(steps[0].Text, Is.EqualTo(firstStep));
                Assert.That(steps, Has.Count.EqualTo(2));
            }
            else
            {
                Assert.That(steps[0].Text, Does.StartWith("Use a light weight, e.g."));
            }
        }

        [TestCase("Lock the bars. Otherwise, the platform may slide.")]
        [TestCase("Keep your back flat. This protects your spine.")]
        [TestCase("Держите гриф ровно. При этом не сгибайте руки.")]
        public void ParseExecution_ContinuationSentence_StaysWithThePreviousOne(string text)
        {
            var steps = Parse(text);

            Assert.That(steps, Has.Count.EqualTo(1));
            Assert.That(steps[0].Kind, Is.EqualTo(InstructionStepKind.Plain));
            Assert.That(steps[0].Text, Is.EqualTo(text));
        }

        [Test]
        public void ParseExecution_StepNumbersCountJoinedSentencesOnce()
        {
            var steps = Parse("Lower the bar. This loads your chest. Press it up.");

            Assert.That(steps.Select(step => step.Text), Is.EqualTo(new[] { "Lower the bar. This loads your chest.", "Press it up." }));
            Assert.That(steps[1].AccessibilityLabel, Is.EqualTo("Step 2 of 2"));
        }

        [Test]
        public void ParseExecution_WordsThatOnlyStartLikeAContinuation_AreNotJoined()
        {
            var steps = Parse("Lower the bar. Instead of a rope, use two stirrups. Thumbs stay up.");

            Assert.That(steps, Has.Count.EqualTo(3));
        }

        [TestCase(null)]
        [TestCase("  ")]
        public void ParseBullets_NoText_ReturnsNoRows(string? text)
        {
            Assert.That(InstructionStepsParser.ParseBullets(text), Is.Empty);
        }

        [Test]
        public void ParseBullets_SeveralSentences_AreBullets()
        {
            var rows = InstructionStepsParser.ParseBullets("Keep your back straight. Do not swing. It wastes energy.");

            Assert.That(rows.Select(row => row.Kind), Has.All.EqualTo(InstructionStepKind.Bullet));
            Assert.That(rows.Select(row => row.Text), Is.EqualTo(new[] { "Keep your back straight.", "Do not swing. It wastes energy." }));
        }

        [TestCase("Keep your back straight.")]
        [TestCase("Do not swing. It wastes energy.")]
        public void ParseBullets_OneRow_IsPlain(string text)
        {
            var rows = InstructionStepsParser.ParseBullets(text);

            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].Kind, Is.EqualTo(InstructionStepKind.Plain));
            Assert.That(rows[0].Text, Is.EqualTo(text));
        }

        [Test]
        public void ParseBullets_ClosingRepeat_IsAnOrdinaryBullet()
        {
            var rows = InstructionStepsParser.ParseBullets("Rest well. Repeat.");

            Assert.That(rows.Select(row => row.Kind), Has.All.EqualTo(InstructionStepKind.Bullet));
        }

        [Test]
        public void Parse_LineBreaks_SplitEvenWithoutPunctuation()
        {
            var steps = Parse("Lower the bar\r\n\r\nPress it up");

            Assert.That(steps.Select(step => step.Text), Is.EqualTo(new[] { "Lower the bar", "Press it up" }));
        }
    }
}
