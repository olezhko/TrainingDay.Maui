using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.Controls;

public class ImageCache : Image
{
    public ImageCache()
    {
        App.ImageDownloader.Downloaded += ImageDownloaderOnDownloaded;
        BackgroundColor = Colors.White;
        Source = "main.png";
    }

    public static readonly BindableProperty ImageUrlProperty = BindableProperty.Create(nameof(ImageUrl), typeof(string), typeof(ImageCache), null, propertyChanged: (bindable, oldvalue, newvalue) => ((ImageCache)bindable).OnImageUrlChanged(), defaultBindingMode: BindingMode.TwoWay);
    public string ImageUrl
    {
        get => (string)GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public void OnImageUrlChanged()
    {
        LoadImage();
    }

    private void LoadImage()
    {
        try
        {
            if (!string.IsNullOrEmpty(ImageUrl))
            {
                BackgroundColor = Colors.Transparent;
                var imageSource = App.Database.GetImage(ImageUrl);
                if (imageSource == null)
                {
                    var uriSource = new UriImageSource()
                    {
                        Uri = new Uri(ImageUrl),
                        CachingEnabled = false,
                    };
                    Source = uriSource;

                    //App.ImageDownloader.AddUrl(ImageUrl);
                }
                else
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

    private void ImageDownloaderOnDownloaded(object sender, ImageData e)
    {
        App.ImageDownloader.Downloaded -= ImageDownloaderOnDownloaded;
        var item = App.Database.GetImage(ImageUrl);
        if (item != null)
        {
            this.Dispatcher.Dispatch(() =>
            {
                Source = ImageSource.FromStream(() => Stream(item));
            });
        }
    }

    private Stream Stream(ImageData data)
    {
        return new MemoryStream(data.Data);
    }
}