using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Table("WeightNote")]
public class WeightNote : TrainingDay.Common.WeightNote, IServerItem
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }

    public int ServerId { get; set; }
}