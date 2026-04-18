using SQLite;

namespace TrainingDay.Maui.Data;

public interface IRepository<T> where T : class, IEntity, new()
{
    IEnumerable<T> GetAll();

    T? GetById(int id);

    int Insert(T entity);

    int Update(T entity);

    int Delete(int id);

    TableQuery<T> Query(); // optional, for advanced filtering
}