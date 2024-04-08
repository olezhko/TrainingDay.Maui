using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.Controls;

public class ImageCache : Image
{
    IImageQueueCacheDownloader imageQueueCacheDownloader;
    public ImageCache()
    {
        imageQueueCacheDownloader = Application.Current.MainPage.Handler.MauiContext.Services.GetService<IImageQueueCacheDownloader>();
        imageQueueCacheDownloader.Downloaded += ImageDownloaderOnDownloaded;
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
                if (imageSource == null)
                {
                    imageQueueCacheDownloader.AddUrl(CodeNum.ToString());
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
        imageQueueCacheDownloader.Downloaded -= ImageDownloaderOnDownloaded;
        var item = App.Database.GetImage(CodeNum.ToString());
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