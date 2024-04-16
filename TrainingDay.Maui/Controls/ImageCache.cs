using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.Controls;

public class ImageCache : Image
{
    public ImageCache()
    {
        BackgroundColor = Colors.White;
        Source = "main.png";
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

    public void OnImageUrlChanged()
    {
        LoadImage();
    }

    private void LoadImage()
    {
        try
        {
            if (CodeNum != 0)
            {
                BackgroundColor = Colors.Transparent;
                var imageSource = App.Database.GetImage(CodeNum.ToString());
                if (imageSource != null)
                {
                    Source = ImageSource.FromStream(() => Stream(imageSource));
                }
            }
            else
            {
                Source = "main.png";
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private Stream Stream(ImageData data)
    {
        return new MemoryStream(data.Data);
    }
}