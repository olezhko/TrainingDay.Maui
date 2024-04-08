using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.Services;

public interface IImageQueueCacheDownloader
{
    void AddUrl(string url);
    void Start();
    event EventHandler<ImageData> Downloaded;
}

public class ImageQueueCacheDownloader : IImageQueueCacheDownloader
{
    IFileHelper _fileHelper;
    public event EventHandler<ImageData> Downloaded;

    private static readonly Queue<string> Items = new Queue<string>();
    public ImageQueueCacheDownloader(IFileHelper fileHelper)
    {
        _fileHelper = fileHelper;
        Start();
    }

    public void AddUrl(string url)
    {
        if (!Items.Contains(url))
        {
            Items.Enqueue(url);
        }
    }

    public void Start()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    if (Items.Count != 0)
                    {
                        string url = Items.Dequeue();
                        var data = await _fileHelper.GetFile($"{url}.jpg");

                        var bytes = File.ReadAllBytes(data);

                        var item = new ImageData()
                        {
                            Data = bytes.ToArray(),
                            Url = url,
                        };

                        App.Database.SaveImage(item);
                        OnDownloaded(item);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Task.Delay(400).Wait();
            }
        });
    }

    protected virtual void OnDownloaded(ImageData e)
    {
        Downloaded?.Invoke(this, e);
    }
}