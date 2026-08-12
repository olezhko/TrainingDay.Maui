using RestSharp;
using System.Text.Json;
using TrainingDay.Common.Models;

namespace TrainingDay.Maui.Services;

public interface ISocialWorkoutsService
{
    Task ShareWorkoutAsync(ShareSocialWorkoutRequest workout);

    Task<IReadOnlyList<SocialWorkoutDto>> GetFeedAsync(int page, int pageSize);

    Task<bool> LikeAsync(string serverId);

    Task<bool> UnlikeAsync(string serverId);
}

public class HttpSocialWorkoutsService : IDisposable, ISocialWorkoutsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IAuthService authService;
    private readonly RestClient client;

    public HttpSocialWorkoutsService(IAuthService authService)
    {
        this.authService = authService;
        client = new RestClient(new RestClientOptions("https://api.trainingday.space/api/v1/SocialWorkouts")
        {
            Timeout = TimeSpan.FromSeconds(10)
        });
    }

    public void Dispose()
    {
        client?.Dispose();
    }

    public async Task ShareWorkoutAsync(ShareSocialWorkoutRequest workout)
    {
        if (!await authService.EnsureValidTokenAsync())
        {
            return;
        }

        var request = CreateAuthorizedRequest(string.Empty, Method.Post);
        request.AddJsonBody(workout);
        await client.ExecuteAsync(request);
    }

    public async Task<IReadOnlyList<SocialWorkoutDto>> GetFeedAsync(int page, int pageSize)
    {
        var request = CreateAuthorizedRequest(string.Empty, Method.Get);
        request.AddQueryParameter("page", page);
        request.AddQueryParameter("pageSize", pageSize);

        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            return [];
        }

        var result = JsonSerializer.Deserialize<PagedResult<SocialWorkoutDto>>(response.Content, JsonOptions);
        return result?.Items ?? [];
    }

    public async Task<bool> LikeAsync(string serverId)
    {
        if (!await authService.EnsureValidTokenAsync())
        {
            return false;
        }

        var request = CreateAuthorizedRequest($"{serverId}/like", Method.Post);
        var response = await client.ExecuteAsync(request);
        return response.IsSuccessful;
    }

    public async Task<bool> UnlikeAsync(string serverId)
    {
        if (!await authService.EnsureValidTokenAsync())
        {
            return false;
        }

        var request = CreateAuthorizedRequest($"{serverId}/like", Method.Delete);
        var response = await client.ExecuteAsync(request);
        return response.IsSuccessful;
    }

    private RestRequest CreateAuthorizedRequest(string resource, Method method)
    {
        var request = new RestRequest(resource, method);
        request.AddHeader("Authorization", $"Bearer {Settings.AuthToken}");
        return request;
    }
}
