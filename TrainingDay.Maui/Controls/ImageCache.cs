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
                else
                {
					Source = "main.png";
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
            Source = "main.png";
        }
    }

    private Stream Stream(ImageDto data) => new MemoryStream(data.Data);
}