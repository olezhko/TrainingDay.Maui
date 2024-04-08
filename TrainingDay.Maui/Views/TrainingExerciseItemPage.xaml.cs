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
            LoadExercise(value);
        }
    }

    public TrainingExerciseItemPage()
    {
        InitializeComponent();
    }

    private void LoadExercise(TrainingExerciseViewModel item)
    {
        TitleLabel.Text = item.ExerciseItemName;
        ExerciseView.BindingContext = item;
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
        if (!ExerciseView.CurrentExercise.ExerciseItemName.IsNotNullOrEmpty())
            return;

        isSaved = true;
        await Toast.Make(AppResources.SavedString).Show();
        App.Database.SaveExerciseItem(ExerciseView.CurrentExercise.GetExercise());
        App.Database.SaveTrainingExerciseItem(ExerciseView.CurrentExercise.GetTrainingExerciseComm());

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