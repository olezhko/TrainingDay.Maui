using Newtonsoft.Json;
using SQLite;
using TrainingDay.Common.Extensions;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Services;

public interface ISQLite
{
    string GetDatabasePath(string filename);
}

public class Repository
{
    SQLiteConnection database;

    public Repository(string filename)
    {
        try
        {
#if ANDROID
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(documentsPath, filename);
            database = new SQLiteConnection(path);
#elif IOS
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string libraryPath = Path.Combine(documentsPath, "..", "Library"); // папка библиотеки
            var path = Path.Combine(libraryPath, filename);
            database = new SQLiteConnection(path);
#endif

        }
        catch (Exception e)
        {
            Console.WriteLine($"Database Error: {e}");
            return;
        }

        InitBasic();
    }

    private async void InitBasic()
    {
        try
        {
            database.CreateTable<Exercise>();
            database.CreateTable<TrainingExerciseComm>();
            database.CreateTable<Training>();
            database.CreateTable<LastTraining>();
            database.CreateTable<WeightNote>();
            database.CreateTable<LastTrainingExercise>();
            database.CreateTable<SuperSet>();
            database.CreateTable<TrainingUnion>();
            database.CreateTable<ImageData>();
            database.CreateTable<Blog>();

            var initExercises = await ResourceExtension.LoadResource<Common.Models.BaseExercise>("exercises", Settings.GetLanguage().TwoLetterISOLanguageName);
            var dbExercises = GetExerciseItems();

            foreach (var exercise in initExercises)
            {
                AddorUpdateExercise(exercise, dbExercises);
            }

            DeleteUnused(dbExercises);
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }
    }

    #region Update Exercises
    private void DeleteUnused(IEnumerable<Exercise> dbExercises)
    {
        var dbGroups = dbExercises.GroupBy(item => item.CodeNum).ToList();
        var trEx = GetTrainingExerciseItems();

        foreach (var group in dbGroups)
        {
            if (group.Key == 0)
            {
                continue;
            }

            int count = group.Count();
            if (count > 1)
            {
                var etalon = group.First();
                for (int i = 1; i < count; i++)
                {
                    var idForDelete = group.ElementAt(i).Id;

                    var itemsToFix = trEx.Where(comm => comm.ExerciseId == idForDelete);
                    foreach (var item in itemsToFix)
                    {
                        item.ExerciseId = etalon.Id;
                        SaveTrainingExerciseItem(item);
                    }

                    DeleteExerciseItem(idForDelete);
                }
            }
        }
    }
    public void AddorUpdateExercise(Common.Models.BaseExercise newExercise, IEnumerable<Exercise> dbExercises)
    {
        var dbExercise = dbExercises.FirstOrDefault(item => item.CodeNum == newExercise.CodeNum);
        if (dbExercise == null)
        {
            SaveBaseExerciseItem(newExercise);
        }
        else
        {
            CorrectExercise(newExercise, dbExercise);
        }
    }

    private void SaveBaseExerciseItem(Common.Models.BaseExercise exercise)
    {
        try
        {
            Exercise newExercise = new Exercise();
            newExercise.CodeNum = exercise.CodeNum;
            newExercise.Description = JsonConvert.SerializeObject(exercise.Description);
            newExercise.MusclesString = exercise.MusclesString;
            newExercise.TagsValue = ExerciseExtensions.ConvertTagStringToInt(exercise.Tags);
            newExercise.ExerciseItemName = exercise.ExerciseItemName;
            SaveExerciseItem(newExercise);
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
            Console.WriteLine(e);
        }
    }

    private void CorrectExercise(Common.Models.BaseExercise srcExercise, Exercise dbExercise)
    {
        try
        {
            dbExercise.Description = JsonConvert.SerializeObject(srcExercise.Description);
            dbExercise.MusclesString = srcExercise.MusclesString;
            dbExercise.ExerciseItemName = srcExercise.ExerciseItemName;
            dbExercise.TagsValue = ExerciseExtensions.ConvertTagStringToInt(srcExercise.Tags);
            SaveExerciseItem(dbExercise);
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
        }
    }
    #endregion

    public int GetLastInsertId() => (int)SQLite3.LastInsertRowid(database.Handle);

    #region Weight Save And Load

    public int SaveWeightNotesItem(WeightNote item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }

        return database.Insert(item);
    }

    public IEnumerable<WeightNote> GetWeightNotesItems()
    {
        return (from i in database.Table<WeightNote>() select i).ToList();
    }

    public void DeleteWeightNote(int senderId)
    {
        database.Delete<WeightNote>(senderId);
    }
    #endregion

    #region LastTrainings

    public IEnumerable<LastTraining> GetLastTrainingItems()
    {
        return (from i in database.Table<LastTraining>() select i).ToList();
    }

    public int SaveLastTrainingItem(LastTraining item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }
        return database.Insert(item);
    }

    public int DeleteLastTraining(int lastId)
    {
        return database.Delete<LastTraining>(lastId);
    }

    public int SaveLastTrainingExerciseItem(LastTrainingExercise item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }
        return database.Insert(item);
    }
    public IEnumerable<LastTrainingExercise> GetLastTrainingExerciseItems()
    {
        return (from i in database.Table<LastTrainingExercise>() select i).ToList();
    }

    public int DeleteLastTrainingExercise(int lastId)
    {
        return database.Delete<LastTrainingExercise>(lastId);
    }
    #endregion

    #region Exercise Methods
    public IEnumerable<Exercise> GetExerciseItems()
    {
        return (from i in database.Table<Exercise>() select i).ToList();
    }

    public Exercise GetExerciseItem(int id)
    {
        return database.Get<Exercise>(id);
    }

    public int DeleteExerciseItem(int id)
    {
        return database.Delete<Exercise>(id);
    }

    public int SaveExerciseItem(Exercise item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }

        database.Insert(item);
        return GetLastInsertId();
    }
    #endregion

    #region TrainingExerciseComm Methods
    public IEnumerable<TrainingExerciseComm> GetTrainingExerciseItems()
    {
        return (from i in database.Table<TrainingExerciseComm>() select i).ToList();
    }

    public TrainingExerciseComm GetTrainingExerciseItem(int id)
    {
        return database.Get<TrainingExerciseComm>(id);
    }

    public int DeleteTrainingExerciseItem(int id)
    {
        return database.Delete<TrainingExerciseComm>(id);
    }

    public int SaveTrainingExerciseItem(TrainingExerciseComm item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }
        database.Insert(item);
        return GetLastInsertId();
    }

    public List<TrainingExerciseViewModel> GetTrainingExerciseItemByTrainingId(int trainingId)
    {
        List<TrainingExerciseViewModel> items = new List<TrainingExerciseViewModel>();
        var allItems = GetTrainingExerciseItems();

        foreach (var trainingExerciseComm in allItems)
        {
            if (trainingExerciseComm.TrainingId == trainingId)
            {
                items.Add(new TrainingExerciseViewModel(GetExerciseItem(trainingExerciseComm.ExerciseId), trainingExerciseComm));
            }
        }


        items = new List<TrainingExerciseViewModel>(items.OrderBy(a => a.OrderNumber));
        return items;
    }


    public void DeleteTrainingExerciseItemByTrainingId(int trainingId)
    {
        var allItems = GetTrainingExerciseItems();
        foreach (var trainingExerciseComm in allItems)
        {
            if (trainingExerciseComm.TrainingId == trainingId)
            {
                DeleteTrainingExerciseItem(trainingExerciseComm.Id);
            }
        }
    }
    #endregion

    #region Training Methods
    public IEnumerable<Training> GetTrainingItems()
    {
        return (from i in database.Table<Training>() select i).ToList();
    }

    public Training GetTrainingItem(int id)
    {
        try
        {
            return database.Get<Training>(id);
        }
        catch
        {
            return new Training();
        }
    }

    public int DeleteTrainingItem(int id)
    {
        return database.Delete<Training>(id);
    }

    public int SaveTrainingItem(Training item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }
        database.Insert(item);
        return GetLastInsertId();
    }
    #endregion

    #region SuperSet

    //public IEnumerable<SuperSetExercise> GetSuperSetItems()
    //{
    //    return (from i in database.Table<SuperSetExercise>() select i).ToList();
    //}

    //public SuperSetExercise GetSuperSetItem(int id)
    //{
    //    return database.Get<SuperSetExercise>(id);
    //}

    //public int SaveSuperSetExerciseItem(SuperSetExercise item)
    //{
    //    if (item.Id != 0)
    //    {
    //        database.Update(item);
    //        return item.Id;
    //    }
    //    database.Insert(item);
    //    return GetLastInsertId();
    //}


    public IEnumerable<SuperSet> GetSuperSetItems()
    {
        return (from i in database.Table<SuperSet>() select i).ToList();
    }

    public int SaveSuperSetItem(SuperSet item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }
        database.Insert(item);
        return GetLastInsertId();
    }

    public int DeleteSuperSetItem(int id)
    {
        return database.Delete<SuperSet>(id);
    }

    public void DeleteSuperSetsByTrainingId(int trainingId)
    {
        var allItems = GetSuperSetItems();
        foreach (var item in allItems)
        {
            if (item.TrainingId == trainingId)
            {
                DeleteTrainingExerciseItem(item.Id);
            }
        }
    }
    #endregion

    #region Image
    public ImageData GetImage(string imageUrl)
    {
        return database.Find<ImageData>(a => a.Url == imageUrl);
    }

    public int SaveImage(ImageData item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }
        database.Insert(item);
        return GetLastInsertId();
    }
    #endregion

    public IEnumerable<TrainingUnion> GetTrainingsGroups()
    {
        return (from i in database.Table<TrainingUnion>() select i).ToList();
    }

    public int SaveTrainingGroup(TrainingUnion item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }

        database.Insert(item);
        return GetLastInsertId();
    }

    public TrainingUnion GetTrainingGroup(int id)
    {
        return database.Get<TrainingUnion>(id);
    }

    public int DeleteTrainingGroup(int id)
    {
        return database.Delete<TrainingUnion>(id);
    }

    public int SaveItem(object item, int id = 0)
    {
        if (id != 0)
        {
            database.Update(item);
            return id;
        }

        database.Insert(item);
        return GetLastInsertId();
    }

    public void DeleteAll<T>()
    {
        database.DeleteAll<T>();
    }

    public void DeleteTrainingExerciseItemByExerciseId(int itemExerciseId)
    {
        var allItems = GetTrainingExerciseItems();
        foreach (var trainingExerciseComm in allItems)
        {
            if (trainingExerciseComm.ExerciseId == itemExerciseId)
            {
                DeleteTrainingExerciseItem(trainingExerciseComm.Id);
            }
        }
    }

    public IEnumerable<Blog> GetBlogItems(int page, int size)
    {
        return (from i in database.Table<Blog>() select i).Skip((page - 1) * size).Take(size).ToList();
    }
}
