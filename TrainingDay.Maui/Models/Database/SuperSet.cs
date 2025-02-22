using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Serializable]
public class SuperSet : TrainingDay.Common.SuperSet
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}