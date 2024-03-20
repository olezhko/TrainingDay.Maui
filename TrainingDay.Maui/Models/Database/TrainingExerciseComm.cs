using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Table("TrainingExerciseComm")]
public class TrainingExerciseComm : TrainingDay.Common.TrainingExerciseComm, IServerItem
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }

    public int ServerId { get; set; }
}