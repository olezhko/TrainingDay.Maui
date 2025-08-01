using CommunityToolkit.Maui.Alerts;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Views;

[QueryProperty(nameof(Item),"Item")]
public partial class TrainingExerciseItemPage : ContentPage
{
    private bool isSaved;
    private TrainingExerciseViewModel item;

    public TrainingExerciseViewModel Item
    {
        get => item;
        set
        {
            item = value;
            ExerciseView.BindingContext = value;
        }
    }

    public TrainingExerciseItemPage()
    {
        InitializeComponent();
        TitleLabel.Text = AppResources.Editing;
    }

    // if we make changes, but after press back, changes is saved
    protected override void OnDisappearing()
    {
        if (isSaved)
        {
            return;
        }

        base.OnDisappearing();
    }

    private async void Save_clicked(object sender, EventArgs e)
    {
        if (!ExerciseView.CurrentExercise.Name.IsNotNullOrEmpty())
            return;

        isSaved = true;
        App.Database.SaveTrainingExerciseItem(ExerciseView.CurrentExercise.GetTrainingExerciseComm());

		await Toast.Make(AppResources.SavedString).Show();
		await Shell.Current.GoToAsync("..");
    }

    private void ExerciseView_OnImageTappedEvent(object sender, ImageSource e)
    {
        FullscreenImage.Source = e;
        ImageFrame.IsVisible = true;
    }

    private void FullscreenImageTapped(object sender, EventArgs e)
    {
        ImageFrame.IsVisible = false;
    }
}