namespace FrpHelper.Web.Models;

public sealed class AuthSession
{
    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public string UserId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public DateTimeOffset? ExpiresAt { get; init; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken) && !string.IsNullOrWhiteSpace(UserId);
}
