using SQLite;
using TrainingDay.Common.Communication;

namespace TrainingDay.Maui.Models.Database;

public class BlogDto : BlogResponse
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public int Id { get; set; }
}