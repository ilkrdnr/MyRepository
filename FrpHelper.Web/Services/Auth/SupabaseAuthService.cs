using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FrpHelper.Web.Configuration;
using FrpHelper.Web.Models;
using FrpHelper.Web.Services.ClientStorage;
using Microsoft.Extensions.Options;

namespace FrpHelper.Web.Services.Auth;

public sealed class SupabaseAuthService(
    HttpClient httpClient,
    IOptions<SupabaseOptions> options,
    IClientStorageService clientStorageService) : IAuthService
{
    private const string SessionKey = "frphelper.auth.session";

    private readonly HttpClient _httpClient = httpClient;
    private readonly SupabaseOptions _options = options.Value;
    private readonly IClientStorageService _clientStorage = clientStorageService;

    public AuthSession? CurrentSession { get; private set; }

    public bool IsAuthenticated => CurrentSession?.IsAuthenticated == true;

    public event Action<AuthSession?>? SessionChanged;

    private bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Url) &&
        !string.IsNullOrWhiteSpace(_options.PublishableKey);

    public async Task<AuthOperationResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return AuthOperationResult.Failure("Supabase ayarlari eksik. appsettings.json dosyasini kontrol edin.");
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return AuthOperationResult.Failure("E-posta ve şifre zorunludur.");
        }

        var endpoint = BuildEndpoint("/auth/v1/signup");
        var payload = new
        {
            email,
            password
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("apikey", _options.PublishableKey);
        request.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return AuthOperationResult.Failure(BuildErrorMessage(body, "Kayıt işlemi başarısız oldu."));
        }

        var authPayload = JsonSerializer.Deserialize<SupabaseAuthResponse>(body, JsonOptions());
        var session = CreateSession(authPayload);

        if (session is null)
        {
            return AuthOperationResult.Failure("Kayıt tamamlandı. E-posta doğrulama sonrası giriş yapın.");
        }

        await PersistSessionAsync(session, cancellationToken);
        return AuthOperationResult.Success("Kayıt başarılı. Oturum açıldı.", session);
    }

    public async Task<AuthOperationResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return AuthOperationResult.Failure("Supabase ayarlari eksik. appsettings.json dosyasini kontrol edin.");
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return AuthOperationResult.Failure("E-posta ve şifre zorunludur.");
        }

        var endpoint = BuildEndpoint("/auth/v1/token?grant_type=password");
        var payload = new
        {
            email,
            password
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("apikey", _options.PublishableKey);
        request.Content = JsonContent.Create(payload);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return AuthOperationResult.Failure(BuildErrorMessage(body, "Giriş başarısız."));
        }

        var authPayload = JsonSerializer.Deserialize<SupabaseAuthResponse>(body, JsonOptions());
        var session = CreateSession(authPayload);

        if (session is null)
        {
            return AuthOperationResult.Failure("Oturum verisi alınamadı.");
        }

        await PersistSessionAsync(session, cancellationToken);
        return AuthOperationResult.Success("Giriş başarılı.", session);
    }

    public async Task<AuthSession?> RestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        var sessionJson = await _clientStorage.GetItemAsync(SessionKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(sessionJson))
        {
            CurrentSession = null;
            SessionChanged?.Invoke(null);
            return null;
        }

        try
        {
            var restored = JsonSerializer.Deserialize<AuthSession>(sessionJson, JsonOptions());
            CurrentSession = restored;
            SessionChanged?.Invoke(CurrentSession);
            return CurrentSession;
        }
        catch
        {
            await _clientStorage.RemoveItemAsync(SessionKey, cancellationToken);
            CurrentSession = null;
            SessionChanged?.Invoke(null);
            return null;
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var token = CurrentSession?.AccessToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            var endpoint = BuildEndpoint("/auth/v1/logout");
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("apikey", _options.PublishableKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            _ = await _httpClient.SendAsync(request, cancellationToken);
        }

        CurrentSession = null;
        await _clientStorage.RemoveItemAsync(SessionKey, cancellationToken);
        SessionChanged?.Invoke(null);
    }

    private async Task PersistSessionAsync(AuthSession session, CancellationToken cancellationToken)
    {
        CurrentSession = session;
        var json = JsonSerializer.Serialize(session, JsonOptions());
        await _clientStorage.SetItemAsync(SessionKey, json, cancellationToken);
        SessionChanged?.Invoke(CurrentSession);
    }

    private string BuildEndpoint(string path)
    {
        var baseUrl = _options.Url.TrimEnd('/');
        return $"{baseUrl}{path}";
    }

    private static AuthSession? CreateSession(SupabaseAuthResponse? response)
    {
        if (response is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(response.AccessToken) || response.User is null)
        {
            return null;
        }

        DateTimeOffset? expires = null;
        if (response.ExpiresAt is not null)
        {
            expires = DateTimeOffset.FromUnixTimeSeconds(response.ExpiresAt.Value);
        }
        else if (response.ExpiresIn is not null)
        {
            expires = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn.Value);
        }

        return new AuthSession
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken ?? string.Empty,
            UserId = response.User.Id,
            Email = response.User.Email ?? string.Empty,
            ExpiresAt = expires
        };
    }

    private static string BuildErrorMessage(string body, string fallback)
    {
        try
        {
            var error = JsonSerializer.Deserialize<SupabaseAuthError>(body, JsonOptions());
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }
        }
        catch
        {
            // Ignore parse issues and return fallback message.
        }

        return fallback;
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class SupabaseAuthResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; init; }

        [JsonPropertyName("expires_at")]
        public long? ExpiresAt { get; init; }

        [JsonPropertyName("user")]
        public SupabaseUser? User { get; init; }
    }

    private sealed class SupabaseUser
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; init; }
    }

    private sealed class SupabaseAuthError
    {
        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }
}
