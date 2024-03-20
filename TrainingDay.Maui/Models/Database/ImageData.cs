using SQLite;

namespace TrainingDay.Maui.Models.Database;

public class ImageData
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public int Id { get; set; }

    public string Url { get; set; }

    public byte[] Data { get; set; }
}