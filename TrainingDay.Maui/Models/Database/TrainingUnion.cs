using SQLite;

namespace TrainingDay.Maui.Models.Database;

public class TrainingUnion : TrainingDay.Common.TrainingUnion
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}