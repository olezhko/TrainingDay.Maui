using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Controls;

public partial class ExerciseView : ContentView
{
    public ICommand DeleteRequestCommand => new Command<WeightAndRepsViewModel>(DeleteRequestWeightAndReps);

    public ObservableCollection<ExerciseVideo> VideoItems { get; set; } = [];

    public TrainingExerciseViewModel CurrentExercise
    {
        get => (TrainingExerciseViewModel)BindingContext;
    }

    public event EventHandler<ImageSource> ImageTappedEvent;

    public ExerciseView()
    {
        InitializeComponent();
    }

    private void ImageTapped(object sender, EventArgs e)
    {
        ImageTappedEvent?.Invoke(this, ImageControl.Source);
    }

    private void AddWeightAndRepsItem_Clicked(object sender, EventArgs e)
    {
        var item = (TrainingExerciseViewModel)BindingContext;
        if (item.WeightAndRepsItems.Count == 0)
        {
            item.WeightAndRepsItems.Add(new WeightAndRepsViewModel(0, 15));
        }
        else
        {
            var last = item.WeightAndRepsItems.Last();
            item.WeightAndRepsItems.Add(new WeightAndRepsViewModel(last.Weight, last.Repetitions));
        }
    }

    private void DeleteRequestWeightAndReps(WeightAndRepsViewModel sender)
    {
        var item = (TrainingExerciseViewModel)BindingContext;
        item.WeightAndRepsItems.Remove(sender);
    }
}