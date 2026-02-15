using Newtonsoft.Json;
using SQLite;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
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
            string libraryPath = Path.Combine(documentsPath, "..", "Library");
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
            database.CreateTable<ExerciseEntity>();
            database.CreateTable<TrainingExerciseEntity>();
            database.CreateTable<TrainingEntity>();
            database.CreateTable<LastTrainingEntity>();
            database.CreateTable<WeightNoteEntity>();
            database.CreateTable<LastTrainingExerciseEntity>();
            database.CreateTable<SuperSetEntity>();
            database.CreateTable<TrainingUnionEntity>();
            database.CreateTable<ImageEntity>();
            database.CreateTable<BlogEntity>();

            var initExercises = await ResourceExtension.LoadResource<BaseExercise>("exercises", Settings.GetLanguage().TwoLetterISOLanguageName);
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
    private void DeleteUnused(IEnumerable<ExerciseEntity> dbExercises)
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
    private bool AddorUpdateExercise(BaseExercise newExercise, IEnumerable<ExerciseEntity> dbExercises)
    {
        try
        {
			var dbExercise = dbExercises.FirstOrDefault(item => item.CodeNum == newExercise.CodeNum);
			if (dbExercise == null)
			{
				dbExercise = new ExerciseEntity();
				dbExercise.CodeNum = newExercise.CodeNum;
			}

			dbExercise.DifficultType = newExercise.DifficultLevel;
			dbExercise.Description = JsonConvert.SerializeObject(newExercise.Description);
			dbExercise.MusclesString = newExercise.MusclesString;
			dbExercise.Name = newExercise.Name;
			dbExercise.TagsValue = ExerciseExtensions.ConvertTagStringToInt(newExercise.Tags);
			SaveExerciseItem(dbExercise);
            return true;
		}
		catch (Exception e)
        {
			LoggingService.TrackError(e);
            return false;
		}
    }

    #endregion

    public int GetLastInsertId() => (int)SQLite3.LastInsertRowid(database.Handle);

    #region Weight Save And Load

    public int SaveWeightNotesItem(WeightNoteEntity item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }

        return database.Insert(item);
    }

    public IEnumerable<WeightNoteEntity> GetWeightNotesItems()
    {
        return (from i in database.Table<WeightNoteEntity>() select i).ToList();
    }

    public void DeleteWeightNote(int senderId)
    {
        database.Delete<WeightNoteEntity>(senderId);
    }
    #endregion

    #region LastTrainings

    public IEnumerable<LastTrainingEntity> GetLastTrainingItems()
    {
        return (from i in database.Table<LastTrainingEntity>() select i).ToList();
    }

    public int SaveLastTrainingItem(LastTrainingEntity item)
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
        return database.Delete<LastTrainingEntity>(lastId);
    }

    public int SaveLastTrainingExerciseItem(LastTrainingExerciseEntity item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }
        return database.Insert(item);
    }
    public IEnumerable<LastTrainingExerciseEntity> GetLastTrainingExerciseItems()
    {
        return (from i in database.Table<LastTrainingExerciseEntity>() select i).ToList();
    }

    public int DeleteLastTrainingExercise(int lastId)
    {
        return database.Delete<LastTrainingExerciseEntity>(lastId);
    }
    #endregion

    #region Exercise Methods
    public IEnumerable<ExerciseEntity> GetExerciseItems()
    {
        return (from i in database.Table<ExerciseEntity>() select i).ToList();
    }

    public ExerciseEntity GetExerciseItem(int id)
    {
        return database.Get<ExerciseEntity>(id);
    }

    public int DeleteExerciseItem(int id)
    {
        return database.Delete<ExerciseEntity>(id);
    }

    public int SaveExerciseItem(ExerciseEntity item)
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
    public IEnumerable<TrainingExerciseEntity> GetTrainingExerciseItems()
    {
        return (from i in database.Table<TrainingExerciseEntity>() select i).ToList();
    }

    public TrainingExerciseEntity GetTrainingExerciseItem(int id)
    {
        return database.Get<TrainingExerciseEntity>(id);
    }

    public int DeleteTrainingExerciseItem(int id)
    {
        return database.Delete<TrainingExerciseEntity>(id);
    }

    public int SaveTrainingExerciseItem(TrainingExerciseEntity item)
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
    public IEnumerable<TrainingEntity> GetTrainingItems()
    {
        return (from i in database.Table<TrainingEntity>() select i).ToList();
    }

    public TrainingEntity GetTrainingItem(int id)
    {
        try
        {
            return database.Get<TrainingEntity>(id);
        }
        catch
        {
            return new TrainingEntity();
        }
    }

    public int DeleteTrainingItem(int id)
    {
        return database.Delete<TrainingEntity>(id);
    }

    public int SaveTrainingItem(TrainingEntity item)
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


    public IEnumerable<SuperSetEntity> GetSuperSetItems()
    {
        return (from i in database.Table<SuperSetEntity>() select i).ToList();
    }

    public int SaveSuperSetItem(SuperSetEntity item)
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
        return database.Delete<SuperSetEntity>(id);
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
    public ImageEntity GetImage(string imageUrl)
    {
        return database.Find<ImageEntity>(a => a.Url == imageUrl);
    }

    public int SaveImage(ImageEntity item)
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

    public IEnumerable<TrainingUnionEntity> GetTrainingsGroups()
    {
        return (from i in database.Table<TrainingUnionEntity>() select i).ToList();
    }

    public int SaveTrainingGroup(TrainingUnionEntity item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }

        database.Insert(item);
        return GetLastInsertId();
    }

    public TrainingUnionEntity GetTrainingGroup(int id)
    {
        return database.Get<TrainingUnionEntity>(id);
    }

    public int DeleteTrainingGroup(int id)
    {
        return database.Delete<TrainingUnionEntity>(id);
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

    public IEnumerable<BlogEntity> GetBlogItems()
    {
        return [.. (from i in database.Table<BlogEntity>() select i)];
    }

    public IEnumerable<BlogEntity> GetBlogItems(int page, int size)
    {
        return [.. (from i in database.Table<BlogEntity>() select i).Skip((page - 1) * size).Take(size)];
    }
}
