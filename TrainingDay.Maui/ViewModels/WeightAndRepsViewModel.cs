using Newtonsoft.Json;
using System.Globalization;
using System.Windows.Input;

namespace TrainingDay.Maui.ViewModels;

public class WeightAndRepsViewModel : BaseViewModel
{
    public WeightAndRepsViewModel() { }

    public WeightAndRepsViewModel(double weight, int repetitions)
    {
        Weight = weight;
        Repetitions = repetitions;
    }

    private void ChangeFinished()
    {
        IsFinished = !IsFinished;
        OnPropertyChanged(nameof(IsFinished));
    }

    [JsonIgnore]
    public ICommand ChangeFinishedCommand => new Command(ChangeFinished);

    [JsonIgnore]
    private int repetitions;

    [JsonIgnore]
    private double weight;

    public int Repetitions
    {
        get => repetitions;
        set
        {
            repetitions = value;
            OnPropertyChanged(nameof(Repetitions));
        }
    }

    public double Weight
    {
        get => weight;
        set
        {
            weight = value;
            OnPropertyChanged(nameof(Weight));
        }
    }

    [JsonIgnore]
    public string WeightString
    {
        get => Weight.ToString(App.CurrentCultureForEntryDot);
        set
        {
            var res = double.TryParse(value, NumberStyles.Any, App.CurrentCultureForEntryDot, out var weight);
            if (res)
            {
                Weight = weight;
            }
        }
    }

    [JsonIgnore]
    public bool IsFinished { get; set; }
}