namespace TrainingDay.Maui.Services;

public interface IExerciseImageService
{
    /// <summary>Storage key of an exercise image: built-in exercises use their code, user ones "new_{id}".</summary>
    string GetImageKey(int codeNum, int exerciseId);

    /// <summary>Returns true if the image bytes are already in memory (data is null when the exercise has no image).</summary>
    bool TryGetCachedImageData(string key, out byte[]? data);

    /// <summary>Loads image bytes off the UI thread; null when the exercise has no image.</summary>
    Task<byte[]?> GetImageDataAsync(string key);
}

public class ExerciseImageService : IExerciseImageService
{
    private readonly Repository repository;

    public ExerciseImageService(Repository repository)
    {
        this.repository = repository;
    }

    public string GetImageKey(int codeNum, int exerciseId) => codeNum != 0 ? codeNum.ToString() : $"new_{exerciseId}";

    public bool TryGetCachedImageData(string key, out byte[]? data) => repository.TryGetCachedImageData(key, out data);

    public Task<byte[]?> GetImageDataAsync(string key) => Task.Run(() => repository.GetImageData(key));
}
