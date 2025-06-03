using SQLite;

namespace TrainingDay.Maui.Models.Database;

public class TrainingUnionDto : TrainingDay.Common.Models.TrainingUnion
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}