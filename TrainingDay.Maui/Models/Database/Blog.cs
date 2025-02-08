using SQLite;
using TrainingDay.Common;

namespace TrainingDay.Maui.Models.Database;

public class Blog : MobileBlog
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}