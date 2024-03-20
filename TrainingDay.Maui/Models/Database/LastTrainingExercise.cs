using SQLite;

namespace TrainingDay.Maui.Models.Database;

public class LastTrainingExercise : TrainingDay.Common.LastTrainingExercise, IServerItem
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }

    public int ServerId { get; set; }
}