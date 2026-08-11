namespace TrainingDay.Maui.Models;

public class AuthRegisterRequest
{
    public string Email { get; set; }

    public string Password { get; set; }

    public string Nick { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; }

    public string Password { get; set; }

    public bool RememberMe { get; set; }
}

public class ForgotPasswordRequest
{
    public string Email { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; }
}

public class LoginResponse
{
    public string Email { get; set; }

    public string Nick { get; set; }

    public string AccessToken { get; set; }

    public DateTime AccessTokenExpires { get; set; }

    public string RefreshToken { get; set; }

    public DateTime RefreshTokenExpires { get; set; }

    public string Role { get; set; }
}

public class RefreshResponse
{
    public string AccessToken { get; set; }

    public DateTime AccessTokenExpires { get; set; }

    public string RefreshToken { get; set; }

    public DateTime RefreshTokenExpires { get; set; }
}

public class AuthResult
{
    public bool Success { get; set; }

    public string ErrorMessage { get; set; }

    public bool RequiresEmailConfirmation { get; set; }

    public static AuthResult Ok() => new() { Success = true };

    public static AuthResult Fail(string message, bool requiresEmailConfirmation = false) => new()
    {
        Success = false,
        ErrorMessage = message,
        RequiresEmailConfirmation = requiresEmailConfirmation,
    };
}
