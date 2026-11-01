using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.Controls;

public class ImageCache : Image
{
    // controls are created by XAML, so the service is resolved from the app container instead of injected
    private static IExerciseImageService ImageService => IPlatformApplication.Current!.Services.GetRequiredService<IExerciseImageService>();

    private string? currentKey;

    public ImageCache()
    {
        BackgroundColor = Colors.White;
        Loaded += ImageCache_Loaded;
    }

    private void ImageCache_Loaded(object? sender, EventArgs e)
    {
        LoadImage();
    }

    public static readonly BindableProperty CodeNumProperty = BindableProperty.Create(nameof(CodeNum), typeof(int), typeof(ImageCache), null, propertyChanged: (bindable, oldvalue, newvalue) => ((ImageCache)bindable).OnImageUrlChanged(), defaultBindingMode: BindingMode.TwoWay);
    public int CodeNum
    {
        get => (int)GetValue(CodeNumProperty);
        set => SetValue(CodeNumProperty, value);
    }

    public static readonly BindableProperty ExerciseIdProperty = BindableProperty.Create(nameof(ExerciseId), typeof(int), typeof(ImageCache), null, propertyChanged: (bindable, oldvalue, newvalue) => ((ImageCache)bindable).OnImageUrlChanged(), defaultBindingMode: BindingMode.TwoWay);
    public int ExerciseId
    {
        get => (int)GetValue(ExerciseIdProperty);
        set => SetValue(ExerciseIdProperty, value);
    }

    public void OnImageUrlChanged() => LoadImage();

    private async void LoadImage()
    {
        try
        {
            var imageService = ImageService;
            string key = imageService.GetImageKey(CodeNum, ExerciseId);
            currentKey = key;
            BackgroundColor = Colors.Transparent;

            if (imageService.TryGetCachedImageData(key, out var cached))
            {
                ApplyImage(cached);
                return;
            }

            Source = "workouts.png";

            var data = await imageService.GetImageDataAsync(key);

            // cell was recycled for another exercise while loading
            if (key != currentKey)
                return;

            ApplyImage(data);
        }
        catch
        {
        }
    }

    private void ApplyImage(byte[]? data)
    {
        if (data == null)
        {
            Source = "workouts.png";
            return;
        }

        Behaviors.Clear();
        Source = ImageSource.FromStream(() => new MemoryStream(data));
    }
}
