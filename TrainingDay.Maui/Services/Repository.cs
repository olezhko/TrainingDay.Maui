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
            database.CreateTable<ExerciseDto>();
            database.CreateTable<TrainingExerciseDto>();
            database.CreateTable<TrainingDto>();
            database.CreateTable<LastTrainingDto>();
            database.CreateTable<WeightNoteDto>();
            database.CreateTable<LastTrainingExerciseDto>();
            database.CreateTable<SuperSetDto>();
            database.CreateTable<TrainingUnionDto>();
            database.CreateTable<ImageDto>();
            //database.CreateTable<BlogDto>();

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
    private void DeleteUnused(IEnumerable<ExerciseDto> dbExercises)
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
    private bool AddorUpdateExercise(BaseExercise newExercise, IEnumerable<ExerciseDto> dbExercises)
    {
        try
        {
			var dbExercise = dbExercises.FirstOrDefault(item => item.CodeNum == newExercise.CodeNum);
			if (dbExercise == null)
			{
				dbExercise = new ExerciseDto();
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

    public int SaveWeightNotesItem(WeightNoteDto item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }

        return database.Insert(item);
    }

    public IEnumerable<WeightNoteDto> GetWeightNotesItems()
    {
        return (from i in database.Table<WeightNoteDto>() select i).ToList();
    }

    public void DeleteWeightNote(int senderId)
    {
        database.Delete<WeightNoteDto>(senderId);
    }
    #endregion

    #region LastTrainings

    public IEnumerable<LastTrainingDto> GetLastTrainingItems()
    {
        return (from i in database.Table<LastTrainingDto>() select i).ToList();
    }

    public int SaveLastTrainingItem(LastTrainingDto item)
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
        return database.Delete<LastTrainingDto>(lastId);
    }

    public int SaveLastTrainingExerciseItem(LastTrainingExerciseDto item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }
        return database.Insert(item);
    }
    public IEnumerable<LastTrainingExerciseDto> GetLastTrainingExerciseItems()
    {
        return (from i in database.Table<LastTrainingExerciseDto>() select i).ToList();
    }

    public int DeleteLastTrainingExercise(int lastId)
    {
        return database.Delete<LastTrainingExerciseDto>(lastId);
    }
    #endregion

    #region Exercise Methods
    public IEnumerable<ExerciseDto> GetExerciseItems()
    {
        return (from i in database.Table<ExerciseDto>() select i).ToList();
    }

    public ExerciseDto GetExerciseItem(int id)
    {
        return database.Get<ExerciseDto>(id);
    }

    public int DeleteExerciseItem(int id)
    {
        return database.Delete<ExerciseDto>(id);
    }

    public int SaveExerciseItem(ExerciseDto item)
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
    public IEnumerable<TrainingExerciseDto> GetTrainingExerciseItems()
    {
        return (from i in database.Table<TrainingExerciseDto>() select i).ToList();
    }

    public TrainingExerciseDto GetTrainingExerciseItem(int id)
    {
        return database.Get<TrainingExerciseDto>(id);
    }

    public int DeleteTrainingExerciseItem(int id)
    {
        return database.Delete<TrainingExerciseDto>(id);
    }

    public int SaveTrainingExerciseItem(TrainingExerciseDto item)
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
    public IEnumerable<TrainingDto> GetTrainingItems()
    {
        return (from i in database.Table<TrainingDto>() select i).ToList();
    }

    public TrainingDto GetTrainingItem(int id)
    {
        try
        {
            return database.Get<TrainingDto>(id);
        }
        catch
        {
            return new TrainingDto();
        }
    }

    public int DeleteTrainingItem(int id)
    {
        return database.Delete<TrainingDto>(id);
    }

    public int SaveTrainingItem(TrainingDto item)
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


    public IEnumerable<SuperSetDto> GetSuperSetItems()
    {
        return (from i in database.Table<SuperSetDto>() select i).ToList();
    }

    public int SaveSuperSetItem(SuperSetDto item)
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
        return database.Delete<SuperSetDto>(id);
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
    public ImageDto GetImage(string imageUrl)
    {
        return database.Find<ImageDto>(a => a.Url == imageUrl);
    }

    public int SaveImage(ImageDto item)
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

    public IEnumerable<TrainingUnionDto> GetTrainingsGroups()
    {
        return (from i in database.Table<TrainingUnionDto>() select i).ToList();
    }

    public int SaveTrainingGroup(TrainingUnionDto item)
    {
        if (item.Id != 0)
        {
            database.Update(item);
            return item.Id;
        }

        database.Insert(item);
        return GetLastInsertId();
    }

    public TrainingUnionDto GetTrainingGroup(int id)
    {
        return database.Get<TrainingUnionDto>(id);
    }

    public int DeleteTrainingGroup(int id)
    {
        return database.Delete<TrainingUnionDto>(id);
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

    public IEnumerable<BlogDto> GetBlogItems(int page, int size)
    {
        return (from i in database.Table<BlogDto>() select i).Skip((page - 1) * size).Take(size).ToList();
    }
}
