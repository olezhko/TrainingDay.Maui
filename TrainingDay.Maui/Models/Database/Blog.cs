using SQLite;

namespace TrainingDay.Maui.Models.Database;

public class BlogEntity
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public int Id { get; set; }

    public int Guid { get; set; }

    /// <summary>
    /// http-content needed to decode
    /// </summary>
    public string? Content { get; set; }
    /// <summary>
    /// DateTime
    /// </summary>
    public DateTime Published { get; set; }
    public string Title { get; set; }
}