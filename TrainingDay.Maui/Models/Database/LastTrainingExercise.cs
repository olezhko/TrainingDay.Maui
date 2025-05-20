using SQLite;

namespace TrainingDay.Maui.Models.Database;

public class LastTrainingExercise : TrainingDay.Common.Models.LastTrainingExercise
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}