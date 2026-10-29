using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class ExerciseListPage : ContentPage
{
    private readonly ExerciseListPageViewModel _viewModel;

    public ExerciseListPage()
    {
        InitializeComponent();
        _viewModel = new ExerciseListPageViewModel();
        BindingContext = _viewModel;
        NavigationPage.SetBackButtonTitle(this, AppResources.ExercisesString);
    }

    private void SearchBar_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.Filter.NameFilter = e.NewTextValue;
        _viewModel.UpdateItems();
    }

    private void SearchButtonPressed(object sender, EventArgs e)
    {
        SearchBar.HideSoftInputAsync(CancellationToken.None);
    }
}
