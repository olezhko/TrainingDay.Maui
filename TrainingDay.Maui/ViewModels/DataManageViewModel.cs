using Newtonsoft.Json;
using System.Text;
using System.Windows.Input;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Serialize;
using TrainingDay.Maui.Resources.Strings;

namespace TrainingDay.Maui.ViewModels;

public class RepositoryData
{
    public IEnumerable<Training> Trainings { get; set; }
    public IEnumerable<TrainingExerciseComm> TrainingExercises { get; set; }
    public IEnumerable<TrainingUnion> Groups { get; set; }
    public IEnumerable<Exercise> Exercises { get; set; }
    public IEnumerable<WeightNote> WeightNotes { get; set; }
    public IEnumerable<SuperSet> SuperSets { get; set; }
    public IEnumerable<LastTraining> LastTrainings { get; set; }
    public IEnumerable<LastTrainingExercise> LastTrainingExercises { get; set; }
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

        RepositoryData data = LoadRepositoryData(content);
        SetRepositoryData(data);

        IsBusy = false;
    }

    private void SetRepositoryData(RepositoryData data)
    {
        foreach (var item in data.WeightNotes)
        {
            App.Database.SaveItem(item);
        }

        Dictionary<int, int> exercisePairs = new Dictionary<int, int>(); // old, new
        foreach (var exercise in data.Exercises)
        {
            int oldId = exercise.Id;
            exercise.Id = 0;
            int newId = App.Database.SaveExerciseItem(exercise);
            exercisePairs.Add(oldId, newId);
        }
    }

    private RepositoryData LoadRepositoryData(string content)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<RepositoryData>(content);
            return data;
        }
        catch (Exception)
        {
            return null;
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
            Exercises = App.Database.GetExerciseItems().Where(item => item.CodeNum == 0),
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