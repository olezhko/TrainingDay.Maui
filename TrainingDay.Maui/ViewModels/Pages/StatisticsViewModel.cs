using System.Globalization;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.ViewModels.Pages;

public class StatisticsViewModel : BaseViewModel
{
    private int _totalTrainings;
    private int _totalExercises;
    private string _totalTime = string.Empty;
    private double _distinctExercisesPercent;
    private int _oneTrainingPerWeekCount;
    private string _mostOftenDay = string.Empty;
    private TopExercise? _top1Exercise;
    private TopExercise? _top2Exercise;
    private TopExercise? _top3Exercise;
    private bool _isLastTrainingsAvailable;
    private bool _isEmpty;

    private sealed record StatisticsSnapshot(
        int TotalTrainings,
        int TotalExercises,
        string TotalTime,
        double DistinctExercisesPercent,
        int OneTrainingPerWeekCount,
        string MostOftenDay,
        IReadOnlyList<TopExercise> TopExercises);

    public async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            // Reads the whole history; keep it off the UI thread
            var snapshot = await Task.Run(BuildSnapshot);

            IsLastTrainingsAvailable = snapshot != null;
            IsEmpty = snapshot == null;
            if (snapshot == null)
                return;

            TotalTrainings = snapshot.TotalTrainings;
            TotalExercises = snapshot.TotalExercises;
            TotalTime = snapshot.TotalTime;
            DistinctExercisesPercent = snapshot.DistinctExercisesPercent;
            OneTrainingPerWeekCount = snapshot.OneTrainingPerWeekCount;
            MostOftenDay = snapshot.MostOftenDay;
            Top1Exercise = snapshot.TopExercises.ElementAtOrDefault(0);
            Top2Exercise = snapshot.TopExercises.ElementAtOrDefault(1);
            Top3Exercise = snapshot.TopExercises.ElementAtOrDefault(2);
            OnPropertyChanged(nameof(IsShowTopExercisesBlock));
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static StatisticsSnapshot? BuildSnapshot()
    {
        var lastTrainings = App.Database.GetLastTrainingItems().ToList();
        if (lastTrainings.Count == 0)
            return null;

        var lastExercises = App.Database.GetLastTrainingExerciseItems().ToList();
        var exercisesCount = App.Database.GetExerciseItems().Count();

        var duration = TimeSpan.FromSeconds(lastTrainings.Sum(item => item.ElapsedTime.TotalSeconds));

        // History keeps names of deleted / renamed exercises too, so the ratio can pass 100%
        var distinctDone = lastExercises.Select(item => item.ExerciseName).Distinct().Count();
        var distinctPercent = exercisesCount == 0 ? 0 : Math.Min(100, (double)distinctDone / exercisesCount * 100);

        var culture = new CultureInfo(Settings.CultureName);
        var mostOftenDay = lastTrainings
            .GroupBy(item => item.Time.DayOfWeek)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .First().Key;

        var topExercises = lastExercises
            .GroupBy(item => item.ExerciseName)
            .Select(group => new TopExercise(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .Take(3)
            .ToList();

        return new StatisticsSnapshot(
            lastTrainings.Count,
            lastExercises.Count,
            duration.ToShortDurationString(),
            distinctPercent,
            CalculateMaxOneTrainingPerWeekCount(lastTrainings),
            culture.DateTimeFormat.GetDayName(mostOftenDay).FirstCharToUpper(),
            topExercises);
    }

    private static int CalculateMaxOneTrainingPerWeekCount(IEnumerable<LastTrainingEntity> trainings)
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
        foreach (var training in sortedTrainings)
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

    public TopExercise? Top1Exercise
    {
        get => _top1Exercise;
        set => SetProperty(ref _top1Exercise, value);
    }

    public TopExercise? Top2Exercise
    {
        get => _top2Exercise;
        set => SetProperty(ref _top2Exercise, value);
    }

    public TopExercise? Top3Exercise
    {
        get => _top3Exercise;
        set => SetProperty(ref _top3Exercise, value);
    }

    public bool IsShowTopExercisesBlock => Top1Exercise != null;

    public bool IsLastTrainingsAvailable
    {
        get => _isLastTrainingsAvailable;
        set => SetProperty(ref _isLastTrainingsAvailable, value);
    }

    /// <summary>
    /// True only after loading found no history, so the empty state doesn't flash while loading.
    /// </summary>
    public bool IsEmpty
    {
        get => _isEmpty;
        set => SetProperty(ref _isEmpty, value);
    }
}
