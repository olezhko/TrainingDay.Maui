using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Table("Exercises")]
public class Exercise : TrainingDay.Common.Exercise, IServerItem
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }

    public int ServerId { get; set; }
}