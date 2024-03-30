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

    #region Drag & Drop
    private void DragGestureRecognizer_DragStarting_Collection(System.Object sender, DragStartingEventArgs e)
    {

    }

    private void DropGestureRecognizer_Drop_Collection(System.Object sender, DropEventArgs e)
    {
        // We handle reordering login in our view model
        e.Handled = true;
    }

    #endregion
}