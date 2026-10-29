namespace TrainingDay.Maui.Models;

/// <summary>One button of an action sheet: a stable action code and its localized label.</summary>
public sealed record ActionSheetOption<TAction>(TAction Action, string Text) where TAction : struct, Enum;
