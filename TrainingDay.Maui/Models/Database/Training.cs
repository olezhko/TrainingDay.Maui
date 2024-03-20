using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Table("Trainings")]
public class Training : TrainingDay.Common.Training, IServerItem
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }

    public int ServerId { get; set; }
}