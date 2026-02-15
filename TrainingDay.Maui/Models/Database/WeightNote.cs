using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Table("WeightNote")]
public class WeightNoteEntity : TrainingDay.Common.Models.WeightNote
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}