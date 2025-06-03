using SQLite;
using TrainingDay.Common.Communication;

namespace TrainingDay.Maui.Models.Database;

public class BlogDto : MobileBlog
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}