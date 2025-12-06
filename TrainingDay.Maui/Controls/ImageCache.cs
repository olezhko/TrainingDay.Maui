using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.Controls;

public class ImageCache : Image
{
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

    private void LoadImage()
    {
        Source = "workouts.png";

        try
        {
            string key = CodeNum != 0 ? CodeNum.ToString() : $"new_{ExerciseId}";

            BackgroundColor = Colors.Transparent;

            var imageSource = App.Database.GetImage(key);

            if (imageSource != null)
            {
                Behaviors.Clear();
                Source = ImageSource.FromStream(() => Stream(imageSource));
            }
        }
        catch
        {
        }
    }

    private Stream Stream(ImageDto data) => new MemoryStream(data.Data);
}