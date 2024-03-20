using SQLite;

namespace TrainingDay.Maui.Models.Database;

public class TrainingUnion : TrainingDay.Common.TrainingUnion, IServerItem
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }

    public int ServerId { get; set; }
}