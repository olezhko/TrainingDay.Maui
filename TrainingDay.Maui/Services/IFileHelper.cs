using TrainingDay.Maui.Models.AWS;

namespace TrainingDay.Maui.Services;

public interface IFileHelper
{
    Task<IEnumerable<S3ObjectDto>> GetFilesList();
    Task<string> GetFile(string key);
}