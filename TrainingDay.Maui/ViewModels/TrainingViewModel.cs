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

    public TrainingViewModel(TrainingDto tr)
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
                Muscles = MusclesConverter.ConvertFromListToString(trainingExerciseViewModel.Muscles.ToList()),
                ExerciseId = trainingExerciseViewModel.ExerciseId,
                Description = JsonConvert.SerializeObject(trainingExerciseViewModel.Description.Model),
                ExerciseItemName = trainingExerciseViewModel.ExerciseItemName,
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
        exercise.SuperSetNum = GetSuperSetNum(this, exercise);
        Exercises.Add(exercise);
    }

    public void DeleteTrainingsItemsFromBase()
    {
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

    public static int GetSuperSetNum(TrainingViewModel training, TrainingExerciseViewModel item)
    {
        if (item.SuperSetId == 0)
        {
            return 0;
        }

        int number = 1;
        List<int> superSetIds = new List<int>();
        foreach (var exercise in training.Exercises)
        {
            if (exercise.SuperSetId == 0)
            {
                continue;
            }

            if (exercise.SuperSetId == item.SuperSetId && exercise.Id != item.Id)
            {
                return exercise.SuperSetNum;
            }
            else
            {
                if (!superSetIds.Contains(exercise.SuperSetId))
                {
                    superSetIds.Add(exercise.SuperSetId);
                    number++;
                }

                return number;
            }
        }

        return number;
    }

    public int GetNewSuperSetNum()
    {
        return (from exercise in Exercises where exercise.SuperSetId != 0 select exercise.SuperSetNum).Prepend(0).Max() + 1;
    }
}