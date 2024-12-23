using CommunityToolkit.Maui.Alerts;
using Microcharts;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.ViewModels.Pages;

public enum WeightType
{
    Weight,
    Waist,
    Hip,
}

public enum ChartWeightPeriod
{
    Week,
    TwoWeeks,
    Month,
    TwoMonth,
    ThreeMonth,
    HalfYear,
    Year,
}

class WeightViewAndSetPageViewModel : BaseViewModel
{
    public ObservableCollection<BodyControlItem> BodyControlItems { get; set; } = new ObservableCollection<BodyControlItem>();
    public ICommand WeightPeriodChangedCommand { get; set; }
    public ICommand SaveValueCommand => new Command<BodyControlItem>(SaveCurrentValue);

    public WeightViewAndSetPageViewModel()
    {
        WeightPeriodChangedCommand = new Command<object>(obj => PrepareBodyControlItems((ChartWeightPeriod)obj));
        BodyControlItems.Add(new BodyControlItem(new List<WeightNote>())
        {
            Name = AppResources.WeightString,
            GoalValue = Settings.WeightGoal,
            Type = WeightType.Weight,
        });

        BodyControlItems.Add(new BodyControlItem(new List<WeightNote>())
        {
            Name = AppResources.WaistString,
            GoalValue = Settings.WaistGoal,
            Type = WeightType.Waist,
        });

        BodyControlItems.Add(new BodyControlItem(new List<WeightNote>())
        {
            Name = AppResources.HipsString,
            GoalValue = Settings.HipGoal,
            Type = WeightType.Hip,
        });
    }

    public void OnAppearing()
    {
        PrepareBodyControlItems(ChartWeightPeriod.Week);
    }

    private async void SaveCurrentValue(BodyControlItem sender)
    {
        var type = sender.Type;
        switch (type)
        {
            case WeightType.Weight:
                Settings.WeightGoal = BodyControlItems.First(item => item.Type == type).GoalValue;
                break;
            case WeightType.Waist:
                Settings.WaistGoal = BodyControlItems.First(item => item.Type == type).GoalValue;
                break;
            case WeightType.Hip:
                Settings.HipGoal = BodyControlItems.First(item => item.Type == type).GoalValue;
                break;
        }

        WeightNote note = new WeightNote
        {
            Date = DateTime.Now,
            Weight = sender.CurrentValue,
            Type = (int)sender.Type,
        };
        App.Database.SaveWeightNotesItem(note);

        sender.ChartItems.Add(note);
        sender.Chart = PrepareChart(sender.GoalValue, sender.ChartItems);
        await Toast.Make(Resources.Strings.AppResources.SavedString).Show();
        //await SiteService.SendBodyControl(Settings.Token);

        OnPropertyChanged(nameof(BodyControlItems));
    }

    private int GetDaysByPeriod(ChartWeightPeriod period)
    {
        int countDaysPeriod = 0;

        switch (period)
        {
            case ChartWeightPeriod.Week:
                countDaysPeriod = 7;
                break;
            case ChartWeightPeriod.TwoWeeks:
                countDaysPeriod = 14;
                break;
            case ChartWeightPeriod.Month:
                countDaysPeriod = 31;
                break;
            case ChartWeightPeriod.TwoMonth:
                countDaysPeriod = 62;
                break;
            case ChartWeightPeriod.ThreeMonth:
                countDaysPeriod = 3 * 31;
                break;
            case ChartWeightPeriod.HalfYear:
                countDaysPeriod = 6 * 31;
                break;
            case ChartWeightPeriod.Year:
                countDaysPeriod = 365;
                break;
        }

        return countDaysPeriod;
    }

    private List<WeightNote> PrepareChartData(IEnumerable<WeightNote> bodyControlItems, DateTime startDate, WeightType type)
    {
        var periodWeightItems = bodyControlItems.Where(a => a.Date > startDate).Where(a => a.Type == (int)type);
        var chartItems = new List<WeightNote>();
        var lastItem = bodyControlItems.LastOrDefault(item => item.Date < startDate && item.Type == (int)type);
        if (lastItem != null)
            chartItems.Add(lastItem);

        chartItems.AddRange(periodWeightItems);

        return chartItems;
    }

    private void PrepareBodyControlItems(ChartWeightPeriod selectedPeriod)
    {
        IsBusy = true;
        double currentWeightValue = 0, currentWaistValue = 0, currentHipsValue = 0;
        var bodyControlItems = App.Database.GetWeightNotesItems();

        int countDaysPeriod = GetDaysByPeriod(selectedPeriod);
        var startDate = DateTime.Now.AddDays(-countDaysPeriod);

        var weightItems = PrepareChartData(bodyControlItems, startDate, WeightType.Weight);
        if (weightItems.Any())
        {
            currentWeightValue = weightItems.Last().Weight;
        }

        var waistItems = PrepareChartData(bodyControlItems, startDate, WeightType.Waist);
        if (waistItems.Any())
        {
            currentWaistValue = waistItems.Last().Weight;
        }

        var hipsItems = PrepareChartData(bodyControlItems, startDate, WeightType.Hip);
        if (hipsItems.Any())
        {
            currentHipsValue = hipsItems.Last().Weight;
        }

        BodyControlItems[0].ChartItems = weightItems;
        BodyControlItems[0].CurrentValue = currentWeightValue;
        BodyControlItems[0].Chart = PrepareChart(Settings.WeightGoal, weightItems);

        BodyControlItems[1].ChartItems = waistItems;
        BodyControlItems[1].CurrentValue = currentWaistValue;
        BodyControlItems[1].Chart = PrepareChart(Settings.WaistGoal, waistItems);

        BodyControlItems[2].ChartItems = hipsItems;
        BodyControlItems[2].CurrentValue = currentHipsValue;
        BodyControlItems[2].Chart = PrepareChart(Settings.HipGoal, hipsItems);

        IsBusy = false;
    }

    private LineChart PrepareChart(double goal, IEnumerable<WeightNote> items)
    {
        if (!items.Any())
        {
            return null;
        }

        var dictDate = items.GroupBy(k => k.Date.Date)
            .OrderBy(k => k.Key)
            .ToDictionary(k => k.Key, v => v.OrderBy(x => x.Date).Last());

        var entries = dictDate.Select(item => new ChartEntry((float)item.Value.Weight)
        {
            ValueLabel = item.Value.Weight.ToString(),
            Label = item.Key.ToString(Settings.GetLanguage().DateTimeFormat.ShortDatePattern.Replace("yyyy", "").Trim('.').Trim('/')),
            ValueLabelColor = App.Current.RequestedTheme == AppTheme.Light ? SKColors.Black : SKColors.White,
            TextColor = App.Current.RequestedTheme == AppTheme.Light ? SKColors.Black : SKColors.White,
        }).ToList();

        var goalEntries = entries.Select(item => new ChartEntry((float)goal)
        {
            ValueLabel = goal.ToString(),
            Label = item.Label,
            ValueLabelColor = App.Current.RequestedTheme == AppTheme.Light ? SKColors.Black : SKColors.White,
            TextColor = App.Current.RequestedTheme == AppTheme.Light ? SKColors.Black : SKColors.White,
        }).ToList();

        var minValueEntries = entries.Select(item => item.Value).Min() - 1;
        var minValueGoals = goalEntries.Select(item => item.Value).Min() - 1;
        var minValue = Math.Min(minValueEntries.Value, minValueGoals.Value);
        return new LineChart
        {
            IsAnimated = false,
            Margin = 30,
            LabelOrientation = Orientation.Horizontal,
            ValueLabelOrientation = Orientation.Horizontal,
            ValueLabelOption = ValueLabelOption.TopOfElement,
            LabelTextSize = 42,
            ValueLabelTextSize = 42,
            SerieLabelTextSize = 42,
            LineSize = 5,
            PointSize = 20,
            PointMode = PointMode.Circle,
            LineMode = LineMode.Spline,
            ShowYAxisLines = true,
            ShowYAxisText = true,
            MinValue = (float)minValue,
            BackgroundColor = SKColors.Transparent,
            YAxisPosition = Position.Left,
            YAxisTextPaint = new SKPaint
            {
                Color = App.Current.RequestedTheme == AppTheme.Light ? SKColors.Black : SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.StrokeAndFill,
                TextSize = 42,
            },
            YAxisLinesPaint = new SKPaint
            {
                Color = App.Current.RequestedTheme == AppTheme.Light ? SKColors.Black : SKColors.White,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
            },
            LabelColor = App.Current.RequestedTheme == AppTheme.Light ? SKColors.Black : SKColors.White,
            Series = new List<ChartSerie>()
                {
                    new ChartSerie()
                    {
                        Color = SKColors.Green,
                        Entries = entries,
                    },
                    new ChartSerie()
                    {
                        Color = SKColors.Gold,
                        Entries = goalEntries,
                        //IsFullLine = true,
                    },
                },
        };
    }
}