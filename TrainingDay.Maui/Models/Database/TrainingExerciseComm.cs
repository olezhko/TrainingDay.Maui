using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Table("TrainingExerciseComm")]
public class TrainingExerciseDto : TrainingDay.Common.Models.TrainingExerciseComm
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}