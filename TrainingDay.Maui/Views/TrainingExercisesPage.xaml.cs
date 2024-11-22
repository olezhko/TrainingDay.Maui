using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class TrainingExercisesPage : ContentPage
{
    private TrainingExercisesPageViewModel viewModel;
    public TrainingExercisesPage()
    {
        InitializeComponent();
        AdMob.AdUnitId = DeviceInfo.Platform == DevicePlatform.Android ? ConstantKeys.WorkoutAndroidAds : ConstantKeys.WorkoutiOSAds;
        AdMob.IsVisible = false;
        NavigationPage.SetBackButtonTitle(this, AppResources.TrainingString);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel = BindingContext as TrainingExercisesPageViewModel;
        ScrollItems(viewModel.TappedExerciseIndex);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (viewModel.Training.Title.IsNotNullOrEmpty())
        {
            App.Database.SaveTrainingItem(new Training() { Id = viewModel.ItemId, Title = viewModel.Training.Title });
        }
    }

    private void ScrollItems(int index)
    {
        if (index != -1)
        {
            ItemsListView.ScrollTo(index);
        }
    }
}