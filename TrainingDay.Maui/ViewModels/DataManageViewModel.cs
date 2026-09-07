using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Models.Serialize;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.ViewModels;

public class RepositoryData
{
    public IEnumerable<TrainingEntity> Trainings { get; set; }
    public IEnumerable<TrainingExerciseEntity> TrainingExercises { get; set; }
    public IEnumerable<TrainingUnionEntity> Groups { get; set; }
    public IEnumerable<ExerciseEntity> Exercises { get; set; }
    public IEnumerable<WeightNoteEntity> WeightNotes { get; set; }
    public IEnumerable<SuperSetEntity> SuperSets { get; set; }
    public IEnumerable<LastTrainingEntity> LastTrainings { get; set; }
    public IEnumerable<LastTrainingExerciseEntity> LastTrainingExercises { get; set; }
}

public class DataManageViewModel : BaseViewModel
{
    private readonly IRepository _repository;

    public DataManageViewModel() : this(App.Database)
    {
    }

    public DataManageViewModel(IRepository repository)
    {
        _repository = repository;
    }

    public ICommand ExportDataCommand => new AsyncRelayCommand(ExportData);
    public ICommand ImportDataCommand => new AsyncRelayCommand(ImportData);

    private async Task ImportData()
    {
        IsBusy = true;

        var file = await FilePicker.PickAsync();
        if (file == null)
        {
            IsBusy = false;
            return;
        }

        var content = File.ReadAllText(file.FullPath, Encoding.UTF8);

        ApplyImportJson(content);

        WeakReferenceMessenger.Default.Send<IncomingTrainingAddedMessage>();

        await Toast.Make(AppResources.SavedString).Show();
        IsBusy = false;
    }

    /// <summary>
    /// Deserializes previously exported repository data and merges it into the current database,
    /// remapping foreign-key ids as it goes. Pure w.r.t. MAUI platform APIs so it can be unit tested.
    /// </summary>
    public void ApplyImportJson(string content)
    {
        try
        {
            var data = JsonSerializer.Deserialize<RepositoryData>(content);
            SetRepositoryData(data);
        }
        catch (Exception ex)
        {
            var dict = new Dictionary<string, object>
            {
                { "context", content }
            };
            LoggingService.TrackError(ex, (IDictionary<string, string>?)dict);
        }
    }

    private void SetRepositoryData(RepositoryData data)
    {
        var baseExercises = _repository.GetExerciseItems().ToList();
        foreach (var item in data.WeightNotes)
        {
            _repository.SaveItem(item);
        }

        Dictionary<int, int> exercisePairs = new Dictionary<int, int>(); // old, new
        foreach (var exercise in data.Exercises)
        {
            int oldId = exercise.Id;
            exercise.Id = 0;
            if (exercise.CodeNum == 0)
            {
                int newId = _repository.SaveExerciseItem(exercise);
                exercisePairs.Add(oldId, newId);
            }
            else
            {
                var baseExercise = baseExercises.FirstOrDefault(be => be.CodeNum == exercise.CodeNum);
                if (baseExercise != null)
                {
                    exercisePairs.Add(oldId, baseExercise.Id);
                }
            }
        }

        Dictionary<int, int> trainingPairs = new Dictionary<int, int>(); // old, new
        foreach (var item in data.Trainings)
        {
            int oldId = item.Id;
            item.Id = 0;
            int newId = _repository.SaveTrainingItem(item);
            trainingPairs.Add(oldId, newId);
        }

        foreach (var item in data.Groups)
        {
            int oldId = item.Id;
            item.Id = 0;
            var TrainingIDs = JsonSerializer.Deserialize<List<int>>(item.TrainingIDsString)
                .Where(id => trainingPairs.ContainsKey(id))
                .Select(id => trainingPairs[id]);
            item.TrainingIDsString = JsonSerializer.Serialize(TrainingIDs);
            int newId = _repository.SaveTrainingGroup(item);
        }

        Dictionary<int, int> superSetPairs = new Dictionary<int, int>(); // old, new
        foreach (var item in data.SuperSets)
        {
            if (!trainingPairs.TryGetValue(item.TrainingId, out int newTrainingId))
            {
                continue;
            }

            int oldId = item.Id;
            item.Id = 0;
            item.TrainingId = newTrainingId;
            int newId = _repository.SaveSuperSetItem(item);
            superSetPairs.Add(oldId, newId);
        }

        foreach (var item in data.TrainingExercises)
        {
            try
            {
                int oldId = item.Id;
                item.Id = 0;

                item.ExerciseId = exercisePairs[item.ExerciseId];
                item.TrainingId = trainingPairs[item.TrainingId];
                if (item.SuperSetId != 0)
                {
                    item.SuperSetId = superSetPairs[item.SuperSetId];
                }

                int newId = _repository.SaveTrainingExerciseItem(item);
            }
            catch
            {

            }
        }

        Dictionary<int, int> lastTrainingPairs = new Dictionary<int, int>(); // old, new
        foreach (var item in data.LastTrainings)
        {
            if (!trainingPairs.TryGetValue(item.TrainingId, out int newTrainingId))
            {
                continue;
            }

            int oldId = item.Id;
            item.Id = 0;
            item.TrainingId = newTrainingId;
            int newId = _repository.SaveLastTrainingItem(item);
            lastTrainingPairs.Add(oldId, newId);
        }

        foreach (var item in data.LastTrainingExercises)
        {
            if (!lastTrainingPairs.TryGetValue(item.LastTrainingId, out int newLastTrainingId))
            {
                continue;
            }

            int oldId = item.Id;
            item.Id = 0;
            item.LastTrainingId = newLastTrainingId;
            int newId = _repository.SaveLastTrainingExerciseItem(item);
        }
    }

    private async Task ExportData()
    {
        IsBusy = true;

        var content = BuildExportJson();
        var filename = Path.Combine(FileSystem.CacheDirectory, "RepositoryData.trday");
        File.WriteAllText(filename, content, Encoding.UTF8);

        await Share.Default.RequestAsync(new ShareFileRequest()
        {
            Title = AppResources.ShareTrainingString,
            File = new ShareFile(filename, "application/trday"),
        }).ContinueWith((task, obj) =>
        {
            Toast.Make(AppResources.SavedString).Show();
        }, TaskContinuationOptions.AttachedToParent);

        IsBusy = false;
    }

    /// <summary>
    /// Reads the current database into a <see cref="RepositoryData"/> snapshot and serializes it.
    /// Pure w.r.t. MAUI platform APIs so it can be unit tested.
    /// </summary>
    public string BuildExportJson()
    {
        RepositoryData repositoryData = new()
        {
            Trainings = _repository.GetTrainingItems(),
            TrainingExercises = _repository.GetTrainingExerciseItems(),
            Groups = _repository.GetTrainingsGroups(),
            Exercises = _repository.GetExerciseItems(),
            WeightNotes = _repository.GetWeightNotesItems(),
            SuperSets = _repository.GetSuperSetItems(),
            LastTrainings = _repository.GetLastTrainingItems(),
            LastTrainingExercises = _repository.GetLastTrainingExerciseItems()
        };

        return JsonSerializer.Serialize(repositoryData);
    }


    public static void SaveToFile(TrainingSerialize training, string filename)
    {
        var content = JsonSerializer.Serialize(training);
        File.WriteAllText(filename, content, Encoding.UTF8);
    }

    public static TrainingSerialize LoadFromData(string data)
    {
        try
        {
            var training = JsonSerializer.Deserialize<TrainingSerialize>(data);
            return training;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static TrainingSerialize LoadFromFile(string filename)
    {
        try
        {
            var content = File.ReadAllText(filename);
            return LoadFromData(content);
        }
        catch (Exception)
        {
            return null;
        }
    }
}