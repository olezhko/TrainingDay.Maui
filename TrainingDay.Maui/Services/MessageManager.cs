using TrainingDay.Maui.Models;

namespace TrainingDay.Maui.Services;

public static class MessageManager
{
    public static async Task DisplayAlert(string title, string message, string okString)
    {
        await Shell.Current.DisplayAlertAsync(title, message, okString);
    }

    public static async Task<bool> DisplayAlert(string title, string message, string okString, string cancelString)
    {
        return await Shell.Current.DisplayAlertAsync(title, message, okString, cancelString);
    }

    /// <summary>Shows an action sheet and returns the chosen action code, or null when cancelled/dismissed.
    /// Callers branch on the code, never on the localized label.</summary>
    public static async Task<TAction?> DisplayActionSheet<TAction>(string title, string cancelString, params ActionSheetOption<TAction>[] options)
        where TAction : struct, Enum
    {
        var chosenText = await Shell.Current.DisplayActionSheetAsync(title, cancelString, null, options.Select(option => option.Text).ToArray());
        return options.FirstOrDefault(option => option.Text == chosenText)?.Action;
    }

    public static async Task<string> DisplayPromptAsync(string title, string message, string okString, string cancelString, string placeholder)
    {
        return await Shell.Current.DisplayPromptAsync(title, message, okString, cancelString, placeholder);
    }
}