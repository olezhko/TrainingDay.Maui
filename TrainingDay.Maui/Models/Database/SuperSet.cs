using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Serializable]
public class SuperSetDto : TrainingDay.Common.Models.SuperSet
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}