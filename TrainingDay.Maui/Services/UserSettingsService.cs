using RestSharp;
using System.Text.Json;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Extensions;

namespace TrainingDay.Maui.Services;

public interface IUserSettingsService
{
    Task<UserSettingsDto> GetAsync();

    Task<UserSettingsDto> UpdateAsync(string nickname, bool shareCompletedWorkouts);
}

public class HttpUserSettingsService : IDisposable, IUserSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IAuthService authService;
    private readonly RestClient client;

    public HttpUserSettingsService(IAuthService authService)
    {
        this.authService = authService;
        client = new RestClient(new RestClientOptions($"{ConstantKeys.ApiBaseUrl}/api/UserSettings")
        {
            Timeout = TimeSpan.FromSeconds(10)
        });
    }

    public void Dispose()
    {
        client?.Dispose();
    }

    public async Task<UserSettingsDto> GetAsync()
    {
        if (!await authService.EnsureValidTokenAsync())
        {
            return null;
        }

        var request = CreateAuthorizedRequest(string.Empty, Method.Get);
        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            return null;
        }

        var settings = JsonSerializer.Deserialize<UserSettingsDto>(response.Content, JsonOptions);
        CacheLocally(settings);
        return settings;
    }

    public async Task<UserSettingsDto> UpdateAsync(string nickname, bool shareCompletedWorkouts)
    {
        if (!await authService.EnsureValidTokenAsync())
        {
            return null;
        }

        var request = CreateAuthorizedRequest(string.Empty, Method.Put);
        request.AddJsonBody(new UpdateUserSettingsRequest { Nickname = nickname, ShareCompletedWorkouts = shareCompletedWorkouts });
        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            return null;
        }

        var settings = JsonSerializer.Deserialize<UserSettingsDto>(response.Content, JsonOptions);
        CacheLocally(settings);
        return settings;
    }

    private static void CacheLocally(UserSettingsDto settings)
    {
        if (settings == null)
        {
            return;
        }

        Settings.Nickname = settings.Nickname;
        Settings.ShareCompletedWorkouts = settings.ShareCompletedWorkouts;
    }

    private RestRequest CreateAuthorizedRequest(string resource, Method method)
    {
        var request = new RestRequest(resource, method);
        request.AddHeader("Authorization", $"Bearer {Settings.AuthToken}");
        return request;
    }
}
