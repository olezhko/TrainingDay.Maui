using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Table("LastTrainings")]
public class LastTraining : TrainingDay.Common.LastTraining, IServerItem
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }

    public int ServerId { get; set; }
}