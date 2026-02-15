using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Serializable]
public class SuperSetEntity : TrainingDay.Common.Models.SuperSet
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}