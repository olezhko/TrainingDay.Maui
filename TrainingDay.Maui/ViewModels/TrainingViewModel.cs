using System.Collections.ObjectModel;
using System.Text.Json;
using TrainingDay.Common.Extensions;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Mappers;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Serialize;
using TrainingDay.Maui.Resources.Strings;

namespace TrainingDay.Maui.ViewModels;

public class TrainingViewModel : BaseViewModel
{
    public int Id { get; set; }
    private TrainingUnion? groupName;
    private string _title;

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            OnPropertyChanged();
        }
    }

    public string LastImplementedDateTime { get; set; } = AppResources.NotExecutedYet;

    /// <summary>
    /// Short card subtitle, e.g. "Exercises: 8 · Chest, Triceps".
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    public bool HasSummary => !string.IsNullOrEmpty(Summary);

    private bool isNextUp;
    public bool IsNextUp { get => isNextUp; set => SetProperty(ref isNextUp, value); }

    public string AccessibilityDescription => string.Join(". ",
        new[] { Title, IsNextUp ? AppResources.NextUpString : null, AppResources.LastImplementedFormatString.Fill(LastImplementedDateTime), Summary }
            .Where(part => !string.IsNullOrEmpty(part)));

    public string StartAccessibilityDescription => AppResources.StartTrainingFormat.Fill(Title);

    public TrainingUnion? Group
    {
        get => groupName;
        set
        {
            groupName = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<SuperSetViewModel<TrainingExerciseViewModel>> ExercisesBySuperSet => PrepareSuperSets();

    public ObservableCollection<TrainingExerciseViewModel> Exercises { get; set; }

    public TrainingViewModel()
    {
        Exercises = new ObservableCollection<TrainingExerciseViewModel>();
    }

    public TrainingViewModel(TrainingEntity tr)
        : this()
    {
        Title = tr.Title;
        Id = tr.Id;
    }

    /// <summary>
    /// Loads a training with its exercises in order. Synchronous DB access — call off the UI thread.
    /// </summary>
    public static TrainingViewModel LoadWithExercises(int id)
    {
        var vm = new TrainingViewModel(App.Database.GetTrainingItem(id));
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

        return vm;
    }

    private ObservableCollection<SuperSetViewModel<TrainingExerciseViewModel>> PrepareSuperSets()
    {
        ObservableCollection<SuperSetViewModel<TrainingExerciseViewModel>> res = [];
        foreach (var exercise in Exercises)
        {
            if (exercise.SuperSetId != 0)
            {
                var first = res.FirstOrDefault(a => a.Id == exercise.SuperSetId);
                if (first != null)
                {
                    first.Add(exercise);
                }
                else
                {
                    res.Add(new SuperSetViewModel<TrainingExerciseViewModel>() { TrainingId = exercise.TrainingId, Id = exercise.SuperSetId });
                    res.Last().Add(exercise);
                }
            }
            else
            {
                res.Add(new SuperSetViewModel<TrainingExerciseViewModel>() { TrainingId = exercise.TrainingId });
                res.Last().Add(exercise);
            }
        }

        return res;
    }

    public void SaveToFile(string filename)
    {
        TrainingSerialize serialize = new()
        {
            Title = Title,
            Id = Id
        };
        foreach (var trainingExerciseViewModel in Exercises)
        {
            serialize.Items.Add(TrainingExerciseMapper.ToSerialize(trainingExerciseViewModel, Id));
        }

        DataManageViewModel.SaveToFile(serialize, filename);
    }

    public void AddExercise(TrainingExerciseViewModel exercise)
    {
        exercise.SuperSetNum = GetSuperSetNum(Exercises, exercise);
        Exercises.Add(exercise);
    }

    public void DeleteTrainingsItemsFromBase()
    {
        App.Database.DeleteTrainingItem(Id);
        App.Database.DeleteTrainingExerciseItemByTrainingId(Id);
        App.Database.DeleteSuperSetsByTrainingId(Id);
    }

    public void DeleteExercise(int id)
    {
        Exercises.Remove(Exercises.FirstOrDefault(item => item.TrainingExerciseId == id));
    }

    public void DeleteExercise(TrainingExerciseViewModel exercise)
    {
        Exercises.Remove(exercise);
    }

    public static int GetSuperSetNum(ObservableCollection<TrainingExerciseViewModel> allItems, TrainingExerciseViewModel item)
    {
        if (item.SuperSetId == 0)
            return 0;

        var uniqueSuperSetIds = allItems
            .Where(x => x.SuperSetId != 0)
            .Select(x => x.SuperSetId)
            .Distinct()
            .ToList();

        int index = uniqueSuperSetIds.IndexOf(item.SuperSetId);
        if (index < 0) // new item with new SuperSetId
            return uniqueSuperSetIds.Count + 1;

        return index >= 0 ? index + 1 : 0;
    }

    public int GetNewSuperSetNum()
    {
        return (from exercise in Exercises where exercise.SuperSetId != 0 select exercise.SuperSetNum).Prepend(0).Max() + 1;
    }
}