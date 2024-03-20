using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class ExerciseListPage : ContentPage
{
    private bool newExerciseAdded;
    private ExerciseListPageViewModel _viewModel;

    public ExerciseListPage()
    {
        InitializeComponent();
        Init(new ExerciseListPageViewModel() { Navigation = this.Navigation });
    }

    public ExerciseListPage(ExerciseListPageViewModel viewmodel)
    {
        InitializeComponent();
        Init(viewmodel);
    }

    private void Init(ExerciseListPageViewModel viewmodel)
    {
        _viewModel = viewmodel;
        BindingContext = _viewModel;
        NavigationPage.SetBackButtonTitle(this, AppResources.ExercisesString);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Items.Count == 0 || SearchBar.Text.IsNotNullOrEmpty() || _viewModel.Filter.CurrentMuscles.Count != 0)
        {
            _viewModel.UpdateItems();
        }

        if (newExerciseAdded)
        {
            newExerciseAdded = false;
            ExercisesListView.ScrollTo(_viewModel.Items.Count - 1);
        }

        ExercisesListView.SelectedItem = null;
    }

    private async void AddExercisesButton_Clicked(object sender, EventArgs e)
    {
        var page = new ExerciseItemPage
        {
            BindingContext = new ExerciseViewModel(),
        };
        page.ExerciseChanged += (o, exercise) =>
        {
            if (exercise.Action == ExerciseChangedEventArgs.ExerciseAction.Added)
            {
                newExerciseAdded = true;
                _viewModel.Items.Add(new ExerciseListItemViewModel(exercise.Sender));
                _viewModel.BaseItems.Add(new ExerciseListItemViewModel(exercise.Sender));
            }
        };

        await Navigation.PushAsync(page);
    }

    private void Page_ExerciseChanged(object sender, ExerciseChangedEventArgs e)
    {
        try
        {
            var delete = _viewModel.Items.First(item => item.Id == e.Sender.Id);
            var index = _viewModel.Items.IndexOf(delete);
            _viewModel.Items.Remove(delete);
            if (e.Action == ExerciseChangedEventArgs.ExerciseAction.Changed)
            {
                _viewModel.Items.Insert(index, new ExerciseListItemViewModel(e.Sender));
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    private void SearchBar_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.UpdateItems();
    }

    private async void ListView_OnItemTapped(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.Count != 0)
        {
            var selected = e.CurrentSelection.FirstOrDefault() as ExerciseListItemViewModel;
            ExerciseItemPage page = new ExerciseItemPage();
            ExerciseViewModel vM = new ExerciseViewModel(selected.GetExercise());
            page.BindingContext = vM;
            page.ExerciseChanged += Page_ExerciseChanged;
            await Navigation.PushAsync(page);
        }
    }
}