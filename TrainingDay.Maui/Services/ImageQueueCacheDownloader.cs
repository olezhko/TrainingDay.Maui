using System.Net;
using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.Services;

public class ImageQueueCacheDownloader
{
    public event EventHandler<ImageData> Downloaded;

    private static readonly Queue<string> Items = new Queue<string>();
    public ImageQueueCacheDownloader()
    {
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
        Task.Run(() =>
        {
            while (true)
            {
                try
                {
                    if (Items.Count != 0)
                    {
                        using (var client = new WebClient())
                        {
                            string url = Items.Dequeue();
                            var data = client.DownloadData(new Uri(url));

                            var item = new ImageData()
                            {
                                Data = data,
                                Url = url,
                            };
                            App.Database.SaveImage(item);
                            OnDownloaded(item);
                        }
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