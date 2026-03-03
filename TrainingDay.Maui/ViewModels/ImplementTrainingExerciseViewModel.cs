using System.Windows.Input;

namespace TrainingDay.Maui.ViewModels;

public class ImplementTrainingExerciseViewModel : TrainingExerciseViewModel
{
    private bool notFinished = true;
    private bool skipped;
    private bool isTimeCalculating;

    public bool IsNotFinished
    {
        get => notFinished;
        set
        {
            notFinished = value;
            OnPropertyChanged();
        }
    }

    public bool IsSkipped
    {
        get => skipped;
        set
        {
            skipped = value;
            OnPropertyChanged();
        }
    }

    public DateTime StartCalculateDateTime { get; set; }

    public bool IsTimeCalculating
    {
        get => isTimeCalculating;
        set
        {
            isTimeCalculating = value;
            OnPropertyChanged();
        }
    }

    public ICommand AddSetCommand => new Command(() =>
    {
        var lastItem = WeightAndRepsItems.LastOrDefault() ?? new WeightAndRepsViewModel(15,0);
        WeightAndRepsItems.Add(lastItem);
        IsNotFinished = true;
    });

    public ICommand RemoveSetCommand => new Command(() =>
    {
        if (WeightAndRepsItems.Count == 0)
            return;

        WeightAndRepsItems.RemoveAt(WeightAndRepsItems.Count - 1);
    });

    public ICommand StartCalculatingTimeCommand => new Command(StartCalculateTime);
    private void StartCalculateTime()
    {
        if (IsTimeCalculating)
        {
            IsTimeCalculating = false;
            return;
        }

        StartCalculateDateTime = DateTime.Now;
        IsTimeCalculating = true;
    }
}