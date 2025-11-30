using SQLite;

namespace TrainingDay.Maui.Models.Database;

public class TrainingUnionEntity : TrainingDay.Common.Models.TrainingUnion
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}