using Microcharts;
using System.Globalization;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.ViewModels;

public class BodyControlItem : BaseViewModel
{
    private const double GoalReachedTolerance = 0.05;

    public int MaxLengthCurrentField { get; set; } = 3;
    public int MaxLengthGoalField { get; set; } = 3;

    public string GoalValueString
    {
        get => GoalValue > 0 ? GoalValue.ToString(CultureInfo.CurrentCulture) : string.Empty;
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
        get => CurrentValue > 0 ? CurrentValue.ToString(CultureInfo.CurrentCulture) : string.Empty;
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

    private string unit = string.Empty;
    public string Unit
    {
        get => unit;
        set
        {
            unit = value?.Trim() ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnit));
            UpdateSummary();
        }
    }

    public bool HasUnit => !string.IsNullOrEmpty(Unit);

    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            isSelected = value;
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
            UpdateSummary();
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
            UpdateSummary();
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
            OnPropertyChanged(nameof(HasChart));
        }
    }

    public bool HasChart => Chart != null;

    private List<WeightNoteEntity> chartItems;
    public List<WeightNoteEntity> ChartItems
    {
        get => chartItems;
        set
        {
            chartItems = value;
            UpdateSummary();
        }
    }

    public string ChangeText { get; private set; } = string.Empty;
    public bool HasChange => !string.IsNullOrEmpty(ChangeText);

    public string GoalProgressText { get; private set; } = string.Empty;
    public bool HasGoalProgress => !string.IsNullOrEmpty(GoalProgressText);

    public BodyControlItem(List<WeightNoteEntity> chartItems)
    {
        ChartItems = new List<WeightNoteEntity>(chartItems);
    }

    public BodyControlItem(List<WeightNoteEntity> chartItems, string name, LineChart chart)
    {
        ChartItems = new List<WeightNoteEntity>(chartItems);
        _name = name;
        this.chart = chart;
    }

    public void UpdateSummary()
    {
        ChangeText = string.Empty;
        if (ChartItems is { Count: > 1 })
        {
            var delta = ChartItems[^1].Weight - ChartItems[0].Weight;
            var sign = delta > 0 ? "+" : delta < 0 ? "−" : string.Empty;
            ChangeText = string.Format(AppResources.PeriodChangeFormat, sign + FormatValue(Math.Abs(delta)));
        }

        GoalProgressText = string.Empty;
        if (GoalValue > 0 && CurrentValue > 0)
        {
            var difference = Math.Abs(GoalValue - CurrentValue);
            GoalProgressText = difference < GoalReachedTolerance
                ? AppResources.GoalReachedString
                : string.Format(AppResources.ToGoalFormat, FormatValue(difference));
        }

        OnPropertyChanged(nameof(ChangeText));
        OnPropertyChanged(nameof(HasChange));
        OnPropertyChanged(nameof(GoalProgressText));
        OnPropertyChanged(nameof(HasGoalProgress));
    }

    private string FormatValue(double value)
    {
        var text = value.ToString("0.#", CultureInfo.CurrentCulture);
        return HasUnit ? $"{text} {Unit}" : text;
    }
}
