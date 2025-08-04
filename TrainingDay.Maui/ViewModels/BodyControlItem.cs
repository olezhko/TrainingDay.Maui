using Microcharts;
using System.Globalization;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.ViewModels;

public class BodyControlItem : BaseViewModel
{
    public int MaxLengthCurrentField { get; set; } = 3;
    public int MaxLengthGoalField { get; set; } = 3;

    public string GoalValueString
    {
        get => GoalValue.ToString(CultureInfo.CurrentCulture);
        set
        {
            var res = double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var weight);
            if (res && weight != GoalValue)
            {
                GoalValue = weight;
                OnPropertyChanged();
            }
        }
    }

    public string CurrentValueString
    {
        get => CurrentValue.ToString(CultureInfo.CurrentCulture);
        set
        {
            var res = double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var weight);
            if (res && weight != CurrentValue)
            {
                CurrentValue = weight;
                OnPropertyChanged();
            }
        }
    }

    private WeightType type;
    public WeightType Type
    {
        get => type;
        set
        {
            type = value;
            OnPropertyChanged();
        }
    }

    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    private double _currentValue;
    public double CurrentValue
    {
        get => _currentValue;
        set
        {
            _currentValue = value;

            MaxLengthCurrentField = value > 100 ? 3 : 4;
            OnPropertyChanged(nameof(MaxLengthCurrentField));
            OnPropertyChanged(nameof(CurrentValueString));
        }
    }

    private double goalValue;
    public double GoalValue
    {
        get => goalValue;
        set
        {
            goalValue = value;

            MaxLengthGoalField = value > 100 ? 3 : 4;
            OnPropertyChanged(nameof(MaxLengthGoalField));
            OnPropertyChanged(nameof(GoalValueString));
        }
    }

    private LineChart chart;
    public LineChart Chart
    {
        get => chart;
        set
        {
            chart = value;
            OnPropertyChanged();
        }
    }

    public List<WeightNoteDto> ChartItems { get; set; }

    public BodyControlItem(List<WeightNoteDto> chartItems)
    {
        ChartItems = new List<WeightNoteDto>(chartItems);
    }

    public BodyControlItem(List<WeightNoteDto> chartItems, string name, LineChart chart)
    {
        ChartItems = new List<WeightNoteDto>(chartItems);
        _name = name;
        this.chart = chart;
    }
}