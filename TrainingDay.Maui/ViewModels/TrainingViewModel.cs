using Newtonsoft.Json;
using System.Collections.ObjectModel;
using TrainingDay.Common.Extensions;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Serialize;
using TrainingDay.Maui.Resources.Strings;

namespace TrainingDay.Maui.ViewModels;

public class TrainingViewModel : BaseViewModel
{
    public int Id { get; set; }
    private TrainingUnionViewModel groupName;
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

    public TrainingUnionViewModel GroupName
    {
        get => groupName;
        set
        {
            groupName = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<SuperSetViewModel> ExercisesBySuperSet => PrepareSuperSets();

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

    private ObservableCollection<SuperSetViewModel> PrepareSuperSets()
    {
        ObservableCollection<SuperSetViewModel> res = new ObservableCollection<SuperSetViewModel>();
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
                    res.Add(new SuperSetViewModel() { TrainingId = exercise.TrainingId, Id = exercise.SuperSetId });
                    res.Last().Add(exercise);
                }
            }
            else
            {
                res.Add(new SuperSetViewModel() { TrainingId = exercise.TrainingId });
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
            serialize.Items.Add(new TrainingExerciseSerialize()
            {
                TrainingExerciseId = trainingExerciseViewModel.TrainingExerciseId,
                SuperSetId = trainingExerciseViewModel.SuperSetId,
                OrderNumber = trainingExerciseViewModel.OrderNumber,
                TrainingId = Id,
                Muscles = MusclesExtensions.ConvertFromListToString(trainingExerciseViewModel.Muscles.ToList()),
                ExerciseId = trainingExerciseViewModel.ExerciseId,
                Description = JsonConvert.SerializeObject(trainingExerciseViewModel.Description.Model),
                Name = trainingExerciseViewModel.Name,
                IsNotFinished = trainingExerciseViewModel.IsNotFinished,
                IsSkipped = trainingExerciseViewModel.IsSkipped,
                SuperSetNum = trainingExerciseViewModel.SuperSetNum,

                TagsValue = ExerciseExtensions.ConvertTagListToInt(trainingExerciseViewModel.Tags),
                WeightAndRepsString = ExerciseManager.ConvertJson(trainingExerciseViewModel.Tags, trainingExerciseViewModel),
                CodeNum = trainingExerciseViewModel.CodeNum,
            });
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