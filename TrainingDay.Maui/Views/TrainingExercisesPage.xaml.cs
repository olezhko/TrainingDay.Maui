using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class TrainingExercisesPage : ContentPage
{
    public int TrainingId { get; set; }

    public TrainingExercisesPage()
    {
        InitializeComponent();
        NavigationPage.SetBackButtonTitle(this, AppResources.TrainingString);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Load(TrainingId);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        var vm = BindingContext as TrainingExercisesPageViewModel;
        if (vm.Training.Title.IsNotNullOrEmpty())
        {
            App.Database.SaveTrainingItem(new Training() { Id = TrainingId, Title = vm.Training.Title });
        }
    }

    private void Load(int id)
    {
        TrainingViewModel trVm = new TrainingViewModel(App.Database.GetTrainingItem(id));
        PrepareTrainingViewModel(trVm);
        TrainingExercisesPageViewModel vm = BindingContext as TrainingExercisesPageViewModel;
        if (vm == null)
        {
            vm = new TrainingExercisesPageViewModel();
        }

        vm.Navigation = Navigation;
        vm.Load(trVm);
        BindingContext = vm;

        ScrollItems(vm.TappedExerciseIndex);
    }

    private void ScrollItems(int index)
    {
        if (index != -1)
        {
            ItemsListView.ScrollTo(index);
        }
    }

    public static void PrepareTrainingViewModel(TrainingViewModel vm)
    {
        var trainingExerciseItems = App.Database.GetTrainingExerciseItems();
        var exerciseItems = App.Database.GetExerciseItems();
        var trainingExercises = trainingExerciseItems.Where(ex => ex.TrainingId == vm.Id);
        var unOrderedItems = trainingExercises.Where(a => a.OrderNumber < 0);

        trainingExercises = trainingExercises.OrderBy(a => a.OrderNumber).Where(a => a.OrderNumber >= 0).ToList();
        int index = 0;
        foreach (var trainingExercise in trainingExercises)
        {
            var exercise = exerciseItems.First(ex => ex.Id == trainingExercise.ExerciseId);
            var trEx = new TrainingExerciseViewModel(exercise, trainingExercise)
            {
                TrainingExerciseId = trainingExercise.Id,
            };
            index++;

            vm.AddExercise(trEx);
        }

        foreach (var trainingExercise in unOrderedItems)
        {
            if (trainingExercise.OrderNumber == -1)
            {
                trainingExercise.OrderNumber = index;
                App.Database.SaveTrainingExerciseItem(trainingExercise);
            }
            var exercise = exerciseItems.First(ex => ex.Id == trainingExercise.ExerciseId);
            var trEx = new TrainingExerciseViewModel(exercise, trainingExercise)
            {
                TrainingExerciseId = trainingExercise.Id,
            };
            index++;
            vm.AddExercise(trEx);
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