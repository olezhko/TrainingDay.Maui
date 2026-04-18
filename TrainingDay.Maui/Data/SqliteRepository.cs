using SQLite;

namespace TrainingDay.Maui.Data;

public class SqliteRepository<T> : IRepository<T> where T : class, IEntity, new()
{
    private readonly SQLiteConnection _database;

    public SqliteRepository(SQLiteConnection database)
    {
        _database = database;
        _database.CreateTable<T>(); // ensure table exists
    }

    public IEnumerable<T> GetAll()
        => _database.Table<T>().ToList();

    public T? GetById(int id)
        => _database.Find<T>(id);

    public int Insert(T entity)
        => _database.Insert(entity);

    public int Update(T entity)
        => _database.Update(entity);

    public int Delete(int id)
    {
        var entity = GetById(id);
        if (entity == null) return 0;

        return _database.Delete(entity);
    }

    public TableQuery<T> Query()
        => _database.Table<T>();
}