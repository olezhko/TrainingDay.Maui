using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Table("Alarm")]
public class Alarm : TrainingDay.Common.Alarm, IServerItem
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
    public int ServerId { get; set; }
}