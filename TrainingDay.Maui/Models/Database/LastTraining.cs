using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Table("LastTrainings")]
public class LastTraining : TrainingDay.Common.LastTraining
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}