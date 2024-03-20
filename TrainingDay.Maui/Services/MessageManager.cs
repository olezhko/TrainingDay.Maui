namespace TrainingDay.Maui.Services;

public static class MessageManager
{
    public static async Task DisplayAlert(string title, string message, string okString)
    {
        await Shell.Current.DisplayAlert(title, message, okString);
    }

    public static async Task<bool> DisplayAlert(string title, string message, string okString, string cancelString)
    {
        return await Shell.Current.DisplayAlert(title, message, okString, cancelString);
    }

    public static async Task<string> DisplayActionSheet(string title, string message, string cancelString, params string[] buttons)
    {
        return await Shell.Current.DisplayActionSheet(title, cancelString, null, buttons);
    }

    public static async Task<string> DisplayPromptAsync(string title, string message, string okString, string cancelString, string placeholder)
    {
        return await Shell.Current.DisplayPromptAsync(title, message, okString, cancelString, placeholder);
    }
}