using SQLite;

namespace TrainingDay.Maui.Models.Database;

public class LastTrainingExerciseEntity : TrainingDay.Common.Models.LastTrainingExercise
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}