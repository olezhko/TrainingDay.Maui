using System.Windows.Input;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.ViewModels.Pages;

public class StatisticsViewModel : BaseViewModel
{
    private ICommand _shareResultsCommand;
    private int _totalTrainings;
    private int _totalExercises;
    private string _totalTime;
    private double _distinctExercisesPercent;
    private int _oneTrainingPerWeekCount;
    private string _mostOftenDay;
    private string _top1ExerciseName;
    private string _top2ExerciseName;
    private string _top3ExerciseName;
    private bool _isShowTopExercisesBlock;
    private bool _isLastTrainingsAvailable;

    public void LoadData()
    {
        IsBusy = true;

        var lastTrainings = App.Database.GetLastTrainingItems();
        if (!lastTrainings.Any())
        {
            IsBusy = false;
            return;
        }

        IsLastTrainingsAvailable = true;
        var lastExercises = App.Database.GetLastTrainingExerciseItems();
        var exercises = App.Database.GetExerciseItems();

        TotalTrainings = lastTrainings.Count();
        TotalExercises = lastExercises.Count();
        var duration = TimeSpan.FromSeconds(lastTrainings.Select(item => item.ElapsedTime)
            .Sum(span => span.TotalSeconds));

        TotalTime = $"{Resources.Strings.AppResources.TotalTimeSpent} {duration:%d} {Resources.Strings.AppResources.Days} {duration:%h} {Resources.Strings.AppResources.Hours} {duration:%m} {Resources.Strings.AppResources.Minutes} {duration:%s} {Resources.Strings.AppResources.Seconds}";

        DistinctExercisesPercent = (double)lastExercises.Select(item => item.ExerciseName).Distinct().Count() / exercises.Count() * 100;
        OneTrainingPerWeekCount = CalculateMaxOneTrainingPerWeekCount(lastTrainings);

        var culture = new System.Globalization.CultureInfo(Settings.CultureName);
        MostOftenDay = culture.DateTimeFormat.GetDayName(lastTrainings
            .Select(item => item.Time.DayOfWeek)
            .GroupBy(s => s).Select(group => new
            {
                DayOfWeek = group.Key,
                Count = group.Count()
            })
            .OrderBy(x => x.DayOfWeek)
            .First().DayOfWeek).FirstCharToUpper();

        if (lastExercises.Count() >= 3)
        {
            var exercisesByCount = lastExercises
                .Select(item => item.ExerciseName)
                .GroupBy(s => s).Select(group => new
                {
                    Name = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(3).ToArray();

            Top1ExerciseName = exercisesByCount[0].Name;
            Top2ExerciseName = exercisesByCount[1].Name;
            Top3ExerciseName = exercisesByCount[2].Name;
            IsShowTopExercisesBlock = true;
        }

        IsBusy = false;
    }

    private int CalculateMaxOneTrainingPerWeekCount(IEnumerable<LastTraining> trainings)
    {
        int result = 0;
        if (!trainings.Any())
        {
            return result;
        }

        var sortedTrainings = trainings.OrderBy(training => training.Time).ToArray();
        DateTime currentWeekStart = sortedTrainings[0].Time.Date;
        DateTime currentWeekEnd = currentWeekStart.AddDays(7);
        int tempResult = 0;
        foreach (var training in trainings)
        {
            if (training.Time < currentWeekStart)
            {
                continue;
            }

            if (training.Time <= currentWeekEnd && training.Time >= currentWeekStart)
            {
                currentWeekStart = currentWeekEnd;
                tempResult++;
            }
            else
            {
                currentWeekStart = training.Time;
                result = Math.Max(result, tempResult);
                tempResult = 1;
            }

            currentWeekEnd = currentWeekStart.AddDays(7);
        }

        result = Math.Max(result, tempResult);

        return result;
    }

    private async void ShareResults()
    {
        await MessageManager.DisplayAlert("Share", "ShareResults", "OK");
    }

    public int TotalTrainings { get => _totalTrainings; set => SetProperty(ref _totalTrainings, value); }

    public int TotalExercises { get => _totalExercises; set => SetProperty(ref _totalExercises, value); }

    public string TotalTime { get => _totalTime; set => SetProperty(ref _totalTime, value); }

    public double DistinctExercisesPercent { get => _distinctExercisesPercent; set => SetProperty(ref _distinctExercisesPercent, value); }

    public int OneTrainingPerWeekCount { get => _oneTrainingPerWeekCount; set => SetProperty(ref _oneTrainingPerWeekCount, value); }

    public string MostOftenDay
    {
        get => _mostOftenDay;
        set => SetProperty(ref _mostOftenDay, value);
    }

    public string Top1ExerciseName
    {
        get => _top1ExerciseName;
        set => SetProperty(ref _top1ExerciseName, value);
    }

    public string Top2ExerciseName
    {
        get => _top2ExerciseName;
        set => SetProperty(ref _top2ExerciseName, value);
    }

    public string Top3ExerciseName
    {
        get => _top3ExerciseName;
        set => SetProperty(ref _top3ExerciseName, value);
    }

    public bool IsShowTopExercisesBlock
    {
        get => _isShowTopExercisesBlock;
        set => SetProperty(ref _isShowTopExercisesBlock, value);
    }

    public bool IsLastTrainingsAvailable
    {
        get => _isLastTrainingsAvailable;
        set => SetProperty(ref _isLastTrainingsAvailable, value);
    }

    public ICommand ShareResultsCommand => _shareResultsCommand ?? (_shareResultsCommand = new Command(ShareResults));
}