namespace TrainingDay.Maui.Models;

public enum InstructionStepKind
{
    /// <summary>The whole text shown as a paragraph, without a marker.</summary>
    Plain,
    Numbered,
    Bullet,
    /// <summary>A closing "Repeat" instruction, which is not a step of its own.</summary>
    Repeat,
}

/// <summary>One row of an exercise's instructions.</summary>
public sealed record InstructionStep(InstructionStepKind Kind, string Text, string Marker, string AccessibilityLabel)
{
    public bool IsNumbered => Kind == InstructionStepKind.Numbered;

    public bool IsSymbol => Kind is InstructionStepKind.Bullet or InstructionStepKind.Repeat;
}
