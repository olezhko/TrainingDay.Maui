using Newtonsoft.Json;
using RestSharp;
using System.Diagnostics;
using System.Net;
using TrainingDay.Common;
using TrainingDay.Maui.Models;

namespace TrainingDay.Maui.Services;

public static class SiteService
{
    public static string Email { get; set; }
    public static string Token { get; set; }

    private static string _sendTokenUrl = Consts.SiteApi + @"/MobileTokens";
    public static async Task<bool> SendTokenToServer(string tokenString, string language, TimeSpan zone, int freq)
    {
        try
        {
            MobileToken token = new MobileToken();
            token.Frequency = freq;
            token.Language = language;
            token.Token = tokenString;
            token.Zone = zone.ToString();
            var client = new RestClient(_sendTokenUrl);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json", JsonConvert.SerializeObject(token), ParameterType.RequestBody);
            try
            {
                var result = await client.ExecuteAsync(request);
                Debug.WriteLine($"Result Status Code: {result.StatusCode} - {result.Content}");
            }
            catch (Exception e)
            {
            }
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    private static string _sendFinishedWorkoutUrl = Consts.SiteApi + @"/MobileTokens/workout";
    public static async Task<bool> SendFinishedWorkout(string tokenString)
    {
        try
        {
            var client = new RestClient(_sendFinishedWorkoutUrl);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");

            var payload = new
            {
                token = tokenString,
            };
            var stringData = JsonConvert.SerializeObject(payload);
            request.AddParameter("application/json", stringData, ParameterType.RequestBody);
            try
            {
                var result = await client.ExecuteAsync(request);
                Debug.WriteLine($"Result Status Code: {result.StatusCode} - {result.Content}");
            }
            catch (Exception e)
            {
            }
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    private static string _sendBodyControlUrl = Consts.SiteApi + @"/MobileTokens/bodycontrol";
    public static async Task<bool> SendBodyControl(string tokenString)
    {
        try
        {
            var client = new RestClient(_sendBodyControlUrl);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");

            var payload = new
            {
                token = tokenString,
            };
            request.AddParameter("application/json", JsonConvert.SerializeObject(payload), ParameterType.RequestBody);
            try
            {
                var result = await client.ExecuteAsync(request);
                Debug.WriteLine($"Result Status Code: {result.StatusCode} - {result.Content}");
            }
            catch (Exception e)
            {
            }
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    private static string _getBlogsUrl = Consts.SiteApi + @"/MobileBlogs/details?culture={0}&page={1}";
    public static async Task<IEnumerable<MobileBlog>> GetBlogsFromServer(string cu, int page = 1)
    {
        var client = new RestClient(string.Format(_getBlogsUrl, cu, page));
        client.Timeout = -1;
        var request = new RestRequest(Method.GET);
        request.AddHeader("Content-Type", "application/json");
        request.AddParameter("application/json", "", ParameterType.RequestBody);
        try
        {
            var result = await client.ExecuteAsync(request);
            Debug.WriteLine($"Result Status Code: {result.StatusCode} - {result.Content}");
            return JsonConvert.DeserializeObject<IEnumerable<MobileBlog>>(result.Content);
        }
        catch (Exception e)
        {
            return new List<MobileBlog>();
        }
    }

    private static string _getVideosUrl = Consts.SiteApi + @"/YouTubeVideos/{0}";
    public static async Task<List<Models.YoutubeVideoItem>> GetVideosFromServer(string keyword)
    {
        keyword = keyword.Replace(" ", "+");
        var client = new RestClient(string.Format(_getVideosUrl, keyword));
        client.Timeout = -1;
        var request = new RestRequest(Method.GET);
        request.AddHeader("Content-Type", "application/json");
        request.AddParameter("application/json", "", ParameterType.RequestBody);
        try
        {
            var result = await client.ExecuteAsync(request);
            Debug.WriteLine($"Result Status Code: {result.StatusCode} - {result.Content}");

            return JsonConvert.DeserializeObject<List<Models.YoutubeVideoItem>>(result.Content);
        }
        catch (Exception e)
        {
            //Crashes.TrackError(e);
            Debug.WriteLine($"Error: {e.Message}");
            return new List<Models.YoutubeVideoItem>();
        }
    }

    private static string _getExercisesUrl = Consts.SiteApi + @"/exercises/get={0}";
    public static async Task<List<BaseExercise>> GetExercises(string culture)
    {
        var client = new RestClient(string.Format(_getExercisesUrl, culture));
        client.Timeout = -1;
        var request = new RestRequest(Method.GET);
        request.AddHeader("Content-Type", "application/json");
        request.AddParameter("application/json", "", ParameterType.RequestBody);
        try
        {
            var result = await client.ExecuteAsync(request);
            Debug.WriteLine($"Result Status Code: {result.StatusCode} - {result.Content}");

            return JsonConvert.DeserializeObject<List<BaseExercise>>(result.Content);
        }
        catch (Exception e)
        {
            //Crashes.TrackError(e);
            Debug.WriteLine($"Error: {e.Message}");
            return new List<BaseExercise>();
        }
    }

    private static string _requestRepoUrl = Consts.SiteApi + @"/MobileTokens/repo_sync?mail={0}&token={1}";
    public static async Task<RepoMobileSite> RequestRepo()
    {
        try
        {
            var client = new RestClient(string.Format(_requestRepoUrl, Email, Token));
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json", "", ParameterType.RequestBody);
            IRestResponse response = await client.ExecuteAsync(request);
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return null;
            }
            return JsonConvert.DeserializeObject<RepoMobileSite>(response.Content);
        }
        catch (Exception e)
        {
            return null;
        }
    }

    private static string _uploadRepoUrl = Consts.SiteApi + @"/MobileTokens/repo_sync";
    public static async Task<int> UploadRepoItem(object item, SyncItemType itemType)
    {
        var repoSer = new RepoMobileItem
        {
            itemString = JsonConvert.SerializeObject(item),
            type = itemType,
            mail = Email,
            token = Token,
        };

        try
        {
            var client = new RestClient(_uploadRepoUrl);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            String content = JsonConvert.SerializeObject(repoSer);
            Console.WriteLine($"UploadRepoItem Request Content {content}");
            request.AddParameter("application/json", content, ParameterType.RequestBody);
            IRestResponse response = await client.ExecuteAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return Convert.ToInt32(response.Content);
            }

            return 0;
        }
        catch (Exception e)
        {
            return 0;
        }
    }

    private static string _deleteRepoUnitUrl = Consts.SiteApi + "/MobileTokens/repo_delete?type={0}&mail={1}&token={2}";
    public static async Task<bool> DeleteRepoItems(SyncItemType itemType)
    {
        try
        {
            var client = new RestClient(string.Format(_deleteRepoUnitUrl, (int)itemType, Email, Token));
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            IRestResponse response = await client.ExecuteAsync(request);
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    private static string _tokenUserUrl = Consts.SiteApi + @"/MobileTokens/token_user";
    public static async Task<bool> TokenUser(MobileUserToken tokenUser)
    {
        try
        {
            var client = new RestClient(_tokenUserUrl);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            String content = JsonConvert.SerializeObject(tokenUser);
            request.AddParameter("application/json", content, ParameterType.RequestBody);
            IRestResponse response = await client.ExecuteAsync(request);
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    private static string _authLoginUrl = Consts.Site + @"/mobileauth/login";
    public static async Task<bool> AuthLogin(string email, string password)
    {
        var obj = new
        {
            email,
            password
        };

        try
        {
            var client = new RestClient(_authLoginUrl);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            String content = JsonConvert.SerializeObject(obj);
            request.AddParameter("application/json", content, ParameterType.RequestBody);
            IRestResponse response = await client.ExecuteAsync(request);
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    private static string _authRegisterUrl = Consts.Site + @"/mobileauth/register";
    public static async Task<bool> AuthRegister(string email, string password, string nick)
    {
        var obj = new
        {
            email,
            password,
            nick
        };

        try
        {
            var client = new RestClient(_authRegisterUrl);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            String content = JsonConvert.SerializeObject(obj);
            request.AddParameter("application/json", content, ParameterType.RequestBody);
            IRestResponse response = await client.ExecuteAsync(request);
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    private static string _authForgotUrl = Consts.Site + @"/mobileauth/forgot";
    public static async Task<bool> AuthForgotPassword(string email)
    {
        var obj = new
        {
            email,
        };

        try
        {
            var client = new RestClient(_authForgotUrl);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/json");
            String content = JsonConvert.SerializeObject(obj);
            request.AddParameter("application/json", content, ParameterType.RequestBody);
            IRestResponse response = await client.ExecuteAsync(request);
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}