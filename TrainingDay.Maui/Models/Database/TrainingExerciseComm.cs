using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Table("TrainingExerciseComm")]
public class TrainingExerciseEntity : TrainingDay.Common.Models.TrainingExercise
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}