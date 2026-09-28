namespace TrainingDay.Maui.Controls;

public class ImageCache : Image
{
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
            string key = CodeNum != 0 ? CodeNum.ToString() : $"new_{ExerciseId}";
            currentKey = key;
            BackgroundColor = Colors.Transparent;

            if (App.Database.TryGetCachedImageData(key, out var cached))
            {
                ApplyImage(cached);
                return;
            }

            Source = "workouts.png";

            var data = await Task.Run(() => App.Database.GetImageData(key));

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
