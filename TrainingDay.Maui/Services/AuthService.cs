using RestSharp;
using System.Text.Json;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;

namespace TrainingDay.Maui.Services;

public interface IAuthService
{
    bool IsLoggedIn { get; }

    Task<AuthResult> RegisterAsync(string email, string password, string? nick);

    Task<AuthResult> LoginAsync(string email, string password, bool rememberMe);

    Task LogoutAsync();

    Task<bool> ForgotPasswordAsync(string email);

    /// <summary>
    /// Ensures the stored access token is valid, refreshing it via the refresh token if it has
    /// expired. Returns false (and clears the local session) if there is no valid session.
    /// </summary>
    Task<bool> EnsureValidTokenAsync();
}

public class AuthService : IDisposable, IAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly RestClient client;
    private readonly SemaphoreSlim refreshLock = new(1, 1);

    public AuthService()
    {
        client = new RestClient(new RestClientOptions($"{ConstantKeys.ApiBaseUrl}/api/v1/Auth")
        {
            Timeout = TimeSpan.FromSeconds(10)
        });
    }

    public void Dispose()
    {
        client?.Dispose();
        refreshLock?.Dispose();
    }

    public bool IsLoggedIn => Settings.IsLoggedIn;

    private static RestRequest CreateRequest(string resource, Method method, object? body = null)
    {
        var request = new RestRequest(resource, method);
        if (body != null)
        {
            request.AddJsonBody(body);
        }

        return request;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string? nick)
    {
        var request = CreateRequest("register", Method.Post, new AuthRegisterRequest { Email = email, Password = password, Nick = nick });
        var response = await client.ExecuteAsync(request);
        return response.IsSuccessful ? AuthResult.Ok() : AuthResult.Fail(ExtractErrorMessage(response));
    }

    public async Task<AuthResult> LoginAsync(string email, string password, bool rememberMe)
    {
        var request = CreateRequest("login", Method.Post, new LoginRequest { Email = email, Password = password, RememberMe = rememberMe });
        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            var message = ExtractErrorMessage(response);
            var requiresConfirmation = message.Contains("confirm", StringComparison.OrdinalIgnoreCase);
            return AuthResult.Fail(message, requiresConfirmation);
        }

        var login = JsonSerializer.Deserialize<LoginResponse>(response.Content, JsonOptions);
        StoreSession(login);
        return AuthResult.Ok();
    }

    public async Task LogoutAsync()
    {
        if (Settings.IsLoggedIn)
        {
            var request = CreateRequest("logout", Method.Post);
            request.AddHeader("Authorization", $"Bearer {Settings.AuthToken}");
            await client.ExecuteAsync(request);
        }

        Settings.ClearSession();
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var request = CreateRequest("forgot", Method.Post, new ForgotPasswordRequest { Email = email });
        var response = await client.ExecuteAsync(request);
        return response.IsSuccessful;
    }

    public async Task<bool> EnsureValidTokenAsync()
    {
        if (!Settings.IsLoggedIn)
        {
            return false;
        }

        if (!IsExpired(Settings.AccessTokenExpiresAtUtc, DateTime.UtcNow))
        {
            return true;
        }

        // The server rotates and revokes the refresh token on every use, so two callers racing to
        // refresh at the same time would otherwise have the second one present an already-revoked
        // token, get a 401, and clear the session the first caller just refreshed successfully.
        await refreshLock.WaitAsync();
        try
        {
            if (!Settings.IsLoggedIn)
            {
                return false;
            }

            // Another caller may have already refreshed while we were waiting for the lock.
            if (!IsExpired(Settings.AccessTokenExpiresAtUtc, DateTime.UtcNow))
            {
                return true;
            }

            if (string.IsNullOrEmpty(Settings.RefreshToken) || IsExpired(Settings.RefreshTokenExpiresAtUtc, DateTime.UtcNow))
            {
                Settings.ClearSession();
                return false;
            }

            var request = CreateRequest("refresh", Method.Post, new RefreshTokenRequest { RefreshToken = Settings.RefreshToken });
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                Settings.ClearSession();
                return false;
            }

            var refreshed = JsonSerializer.Deserialize<RefreshResponse>(response.Content, JsonOptions);
            Settings.AuthToken = refreshed.AccessToken;
            Settings.AccessTokenExpiresAtUtc = refreshed.AccessTokenExpires;
            Settings.RefreshToken = refreshed.RefreshToken;
            Settings.RefreshTokenExpiresAtUtc = refreshed.RefreshTokenExpires;
            return true;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private static void StoreSession(LoginResponse login)
    {
        Settings.AuthToken = login.AccessToken;
        Settings.AccessTokenExpiresAtUtc = login.AccessTokenExpires;
        Settings.RefreshToken = login.RefreshToken;
        Settings.RefreshTokenExpiresAtUtc = login.RefreshTokenExpires;
        Settings.UserEmail = login.Email;
        Settings.UserRole = login.Role;
        if (!string.IsNullOrWhiteSpace(login.Nick))
        {
            Settings.Nickname = login.Nick;
        }
    }

    /// <summary>
    /// A 60s safety buffer avoids racing a token that expires mid-request.
    /// </summary>
    public static bool IsExpired(DateTime expiresAtUtc, DateTime nowUtc)
    {
        return nowUtc >= expiresAtUtc.AddSeconds(-60);
    }

    /// <summary>
    /// Auth failures come back as BadRequest(string), which ASP.NET Core JSON-encodes as a quoted
    /// string body - fall back to the raw content for any other shape (e.g. ModelState errors).
    /// </summary>
    private static string ExtractErrorMessage(RestResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.Content))
        {
            return response.ErrorMessage ?? "Request failed.";
        }

        try
        {
            var asString = JsonSerializer.Deserialize<string>(response.Content);
            if (!string.IsNullOrWhiteSpace(asString))
            {
                return asString;
            }
        }
        catch (JsonException)
        {
        }

        return response.Content;
    }
}
