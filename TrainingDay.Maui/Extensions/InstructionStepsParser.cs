using System.Globalization;
using System.Text.RegularExpressions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Models;

namespace TrainingDay.Maui.Extensions;

/// <summary>Turns an exercise's instruction text into rows for display.</summary>
public static partial class InstructionStepsParser
{
    private const string BulletMarker = "•";
    private const string RepeatMarker = "↻";

    private static readonly HashSet<string> RepeatSentences = new(StringComparer.OrdinalIgnoreCase)
    {
        "Repeat",
        "Повторите",
        "Повторить",
        "Повторение",
        "Повторите движение",
    };

    /// <summary>
    /// A line break, or sentence-ending punctuation before something that starts a new sentence.
    /// Requiring the capital keeps "1.5" and "т.е. так" in one piece.
    /// </summary>
    [GeneratedRegex(@"(?<=[.!?…])\s+(?=[\p{Lu}\d«""(])|\s*\n\s*")]
    private static partial Regex SentenceBoundary();

    /// <summary>A sentence that only makes sense together with the one before it.</summary>
    [GeneratedRegex(@"^(This|That|These|It|They|However|But|So|Otherwise|Instead,|Therefore|Thus|Это|Эта|Этот|Эти|Но|Так что|Однако|То есть|Иначе|Поэтому|При этом|В этом случае|В противном случае|Благодаря этому|Только так)(?=[\s,])", RegexOptions.None)]
    private static partial Regex ContinuationSentence();

    /// <param name="text">The execution text.</param>
    /// <param name="tags">The exercise's tags; timed exercises get bullets, as their text has no fixed order.</param>
    /// <param name="stepOfFormat">Format for the screen reader label, e.g. "Step {0} of {1}".</param>
    public static IReadOnlyList<InstructionStep> ParseExecution(string? text, IEnumerable<ExerciseTags>? tags, string stepOfFormat)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var sentences = SplitSentences(text);
        string? repeatText = sentences.Count > 1 ? GetRepeatText(sentences[^1]) : null;
        if (repeatText != null)
        {
            sentences.RemoveAt(sentences.Count - 1);
        }

        var rows = JoinContinuations(sentences);
        if (rows.Count <= 1)
        {
            return [CreatePlain(text)];
        }

        bool isTimed = tags?.Contains(ExerciseTags.ExerciseByTime) == true;
        var steps = rows
            .Select((row, index) => isTimed
                ? CreateBullet(row)
                : new InstructionStep(
                    InstructionStepKind.Numbered,
                    row,
                    (index + 1).ToString(CultureInfo.CurrentCulture),
                    string.Format(CultureInfo.CurrentCulture, stepOfFormat, index + 1, rows.Count)))
            .ToList();

        if (repeatText != null)
        {
            steps.Add(new InstructionStep(InstructionStepKind.Repeat, repeatText, RepeatMarker, string.Empty));
        }

        return steps;
    }

    /// <summary>Text whose sentences have no order, such as advice.</summary>
    public static IReadOnlyList<InstructionStep> ParseBullets(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var rows = JoinContinuations(SplitSentences(text));
        return rows.Count <= 1 ? [CreatePlain(text)] : rows.Select(CreateBullet).ToList();
    }

    private static InstructionStep CreatePlain(string text) => new(InstructionStepKind.Plain, text.Trim(), string.Empty, string.Empty);

    private static InstructionStep CreateBullet(string text) => new(InstructionStepKind.Bullet, text, BulletMarker, string.Empty);

    private static List<string> SplitSentences(string text)
    {
        return SentenceBoundary().Split(text.Trim())
            .Select(sentence => sentence.Trim())
            .Where(sentence => sentence.Length > 0)
            .ToList();
    }

    private static List<string> JoinContinuations(List<string> sentences)
    {
        var rows = new List<string>();
        foreach (string sentence in sentences)
        {
            if (rows.Count > 0 && ContinuationSentence().IsMatch(sentence))
            {
                rows[^1] = $"{rows[^1]} {sentence}";
            }
            else
            {
                rows.Add(sentence);
            }
        }

        return rows;
    }

    private static string? GetRepeatText(string sentence)
    {
        string withoutEnding = sentence.TrimEnd('.', '!', '…', ' ');
        return RepeatSentences.Contains(withoutEnding) ? withoutEnding : null;
    }
}
