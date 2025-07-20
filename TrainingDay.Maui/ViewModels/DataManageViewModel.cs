using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using System.Text;
using System.Windows.Input;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Models.Serialize;
using TrainingDay.Maui.Resources.Strings;

namespace TrainingDay.Maui.ViewModels;

public class RepositoryData
{
    public IEnumerable<TrainingDto> Trainings { get; set; }
    public IEnumerable<TrainingExerciseDto> TrainingExercises { get; set; }
    public IEnumerable<TrainingUnionDto> Groups { get; set; }
    public IEnumerable<ExerciseDto> Exercises { get; set; }
    public IEnumerable<WeightNoteDto> WeightNotes { get; set; }
    public IEnumerable<SuperSetDto> SuperSets { get; set; }
    public IEnumerable<LastTrainingDto> LastTrainings { get; set; }
    public IEnumerable<LastTrainingExerciseDto> LastTrainingExercises { get; set; }
}

public class DataManageViewModel : BaseViewModel
{
    public ICommand ExportDataCommand => new Command(ExportData);
    public ICommand ImportDataCommand => new Command(ImportData);

    private async void ImportData()
    {
        IsBusy = true;

        var file = await FilePicker.PickAsync();
        if (file == null)
        {
            IsBusy = false;
            return;
        }

        var content = File.ReadAllText(file.FullPath);

        RepositoryData data = JsonConvert.DeserializeObject<RepositoryData>(content);
		SetRepositoryData(data);

        WeakReferenceMessenger.Default.Send<IncomingTrainingAddedMessage>();

        await Toast.Make(AppResources.SavedString).Show();
        IsBusy = false;
    }

    private void SetRepositoryData(RepositoryData data)
    {
        var baseExercises = App.Database.GetExerciseItems().ToList();
        foreach (var item in data.WeightNotes)
        {
            App.Database.SaveItem(item);
        }

        Dictionary<int, int> exercisePairs = new Dictionary<int, int>(); // old, new
        foreach (var exercise in data.Exercises)
        {
            int newId = 0;
            int oldId = exercise.Id;
            exercise.Id = 0;
            if (exercise.CodeNum == 0)
            {
                newId = App.Database.SaveExerciseItem(exercise);
            }
            else
            {
                newId = baseExercises.FirstOrDefault(be => be.CodeNum == exercise.CodeNum).Id;
            }

            exercisePairs.Add(oldId, newId);
        }

        Dictionary<int, int> trainingPairs = new Dictionary<int, int>(); // old, new
        foreach (var item in data.Trainings)
        {
            int oldId = item.Id;
            item.Id = 0;
            int newId = App.Database.SaveTrainingItem(item);
            trainingPairs.Add(oldId, newId);
        }

        foreach (var item in data.Groups)
        {
            int oldId = item.Id;
            item.Id = 0;
            var TrainingIDs = JsonConvert.DeserializeObject<List<int>>(item.TrainingIDsString).Select(id => trainingPairs[id]);
            item.TrainingIDsString = JsonConvert.SerializeObject(TrainingIDs);
            int newId = App.Database.SaveTrainingGroup(item);
        }

        Dictionary<int, int> superSetPairs = new Dictionary<int, int>(); // old, new
        foreach (var item in data.SuperSets)
        {
            int oldId = item.Id;
            item.Id = 0;
            item.TrainingId = trainingPairs[item.TrainingId];
            int newId = App.Database.SaveSuperSetItem(item);
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

                int newId = App.Database.SaveTrainingExerciseItem(item);
            }
            catch (Exception ex)
            {

            }
        }

        Dictionary<int, int> lastTrainingPairs = new Dictionary<int, int>(); // old, new
        foreach (var item in data.LastTrainings)
        {
            int oldId = item.Id;
            item.Id = 0;
            item.TrainingId = trainingPairs[item.TrainingId];
            int newId = App.Database.SaveLastTrainingItem(item);
            lastTrainingPairs.Add(oldId, newId);
        }

        foreach (var item in data.LastTrainingExercises)
        {
            int oldId = item.Id;
            item.Id = 0;
            item.LastTrainingId = lastTrainingPairs[item.LastTrainingId];
            int newId = App.Database.SaveLastTrainingExerciseItem(item);
        }
    }

    private async void ExportData()
    {
        IsBusy = true;
        RepositoryData repositoryData = new()
        {
            Trainings = App.Database.GetTrainingItems(),
            TrainingExercises = App.Database.GetTrainingExerciseItems(),
            Groups = App.Database.GetTrainingsGroups(),
            Exercises = App.Database.GetExerciseItems(),
            WeightNotes = App.Database.GetWeightNotesItems(),
            SuperSets = App.Database.GetSuperSetItems(),
            LastTrainings = App.Database.GetLastTrainingItems(),
            LastTrainingExercises = App.Database.GetLastTrainingExerciseItems()
        };

        SaveToFile(repositoryData, out string filename);

        await Share.Default.RequestAsync(new ShareFileRequest()
        {
            Title = AppResources.ShareTrainingString,
            File = new ShareFile(filename, "application/trday"),
        });

        await Toast.Make(AppResources.SavedString).Show();

        IsBusy = false;
    }

    private void SaveToFile(RepositoryData repositoryData, out string filename)
    {
        filename = Path.Combine(FileSystem.CacheDirectory, $"RepositoryData.trday");

        var content = JsonConvert.SerializeObject(repositoryData);
        File.WriteAllText(filename, content, Encoding.UTF8);
    }

    public static TrainingSerialize LoadFromData(string data)
    {
        try
        {
            var training = JsonConvert.DeserializeObject<TrainingSerialize>(data);
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

    public static void SaveToFile(TrainingSerialize training, string filename)
    {
        var content = JsonConvert.SerializeObject(training);
        File.WriteAllText(filename, content, Encoding.UTF8);
    }
}