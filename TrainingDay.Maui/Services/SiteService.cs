using Newtonsoft.Json;
using RestSharp;
using TrainingDay.Maui.Extensions;

namespace TrainingDay.Maui.Services;

public static class SiteService
{
    const string blogId = "5827172986409389203";
    const string getBlogsUrl = $"https://www.googleapis.com/blogger/v3/blogs/{blogId}/posts?key={ConstantKeys.BloggerKey}";
    const string getBlogUrlFormat = "https://www.googleapis.com/blogger/v3/blogs/{0}/posts/{2}?key={1}";

    private static RestClient InitApiClient(string url)
    {
        var options = new RestClientOptions(url)
        {
        };

        var client = new RestClient(options);
        return client;
    }

    private static void Dispose(RestClient client)
    {
        client.Dispose();
        client = null;
    }

    private static RestRequest InitRequest(Method method = Method.Get, object body = null)
    {
        var request = new RestRequest();
        request.Method = method;
        if (body != null)
        {
            request.AddParameter("application/json", body, ParameterType.RequestBody);
        }
        return request;
    }

    public static async Task<BlogsResponse> GetBlogsAsync(string nextPageToken = null)
    {
        try
        {
            string url = getBlogsUrl;
            if (nextPageToken is not null)
            {
                url = $"{url}&pageToken={nextPageToken}";
            }

            var response = await InitApiClient(url)
                .ExecuteAsync(InitRequest());

            if (response.IsSuccessful)
            {
                var result = JsonConvert.DeserializeObject<BlogsResponse>(response.Content);
                return result;
            }

            return null;
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public static async Task<BlogResponse> GetBlogAsync(string id)
    {
        try
        {
            string url = string.Format(getBlogUrlFormat, blogId, ConstantKeys.BloggerKey, id);

            var response = await InitApiClient(url)
                .ExecuteAsync(InitRequest());

            if (response.IsSuccessful)
            {
                var result = JsonConvert.DeserializeObject<BlogResponse>(response.Content);
                return result;
            }

            return null;
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }
}

public class BlogsResponse
{
    public string NextPageToken { get; set; }
    public IEnumerable<BlogResponse> Items { get; set; }
}

public class BlogResponse
{
    /// <summary>
    /// DateTime
    /// </summary>
    public string Published { get; set; }
    public string Title { get; set; }

    public string Id { get; set; }
    /// <summary>
    /// http-content needed to decode
    /// </summary>
    public string Content { get; set; }

    public IReadOnlyCollection<string> Labels { get; set; }
}