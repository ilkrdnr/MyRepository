using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FrpHelper.Web.Configuration;
using FrpHelper.Web.Models;
using FrpHelper.Web.Services.Auth;
using Microsoft.Extensions.Options;

namespace FrpHelper.Web.Services.Permissions;

public sealed class UserPermissionService(
    HttpClient httpClient,
    IOptions<SupabaseOptions> options,
    IAuthService authService) : IUserPermissionService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly SupabaseOptions _options = options.Value;
    private readonly IAuthService _authService = authService;

    public UserPermissionRow? CurrentPermissions { get; private set; }

    public bool IsAdmin => CurrentPermissions?.IsAdmin == true;

    public bool CanViewPool => CurrentPermissions?.CanViewPool ?? true;

    public bool CanUploadPool => CurrentPermissions?.CanUploadPool ?? true;

    public bool CanEditReports => CurrentPermissions?.CanEditReports ?? true;

    public event Action<UserPermissionRow?>? PermissionChanged;

    private bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Url) &&
        !string.IsNullOrWhiteSpace(_options.PublishableKey);

    public async Task<SupabaseOperationResult> EnsureCurrentUserRecordAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return SupabaseOperationResult.Failure("Supabase ayarları eksik.");
        }

        if (!_authService.IsAuthenticated || _authService.CurrentSession is null)
        {
            CurrentPermissions = null;
            PermissionChanged?.Invoke(null);
            return SupabaseOperationResult.Failure("Önce giriş yapmalısınız.");
        }

        var current = await GetMyPermissionCoreAsync(cancellationToken);
        if (current is not null)
        {
            CurrentPermissions = current;
            PermissionChanged?.Invoke(CurrentPermissions);
            return SupabaseOperationResult.Success("Kullanıcı yetkisi bulundu.");
        }

        var baseUrl = _options.Url.TrimEnd('/');
        var insertUrl = $"{baseUrl}/rest/v1/{_options.PermissionsTable}";

        var payload = new[]
        {
            new
            {
                user_id = _authService.CurrentSession.UserId,
                email = _authService.CurrentSession.Email,
                is_admin = false,
                can_view_pool = true,
                can_upload_pool = true,
                can_edit_reports = true
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, insertUrl);
        AddSupabaseHeaders(request, includePrefer: true, prefer: "return=representation");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return SupabaseOperationResult.Failure($"Yetki kaydı oluşturulamadı: {(int)response.StatusCode} - {body}");
        }

        var rows = await response.Content.ReadFromJsonAsync<List<UserPermissionRow>>(cancellationToken: cancellationToken);
        CurrentPermissions = rows?.FirstOrDefault();
        PermissionChanged?.Invoke(CurrentPermissions);
        return SupabaseOperationResult.Success("Kullanıcı yetkisi oluşturuldu.");
    }

    public async Task<UserPermissionRow?> RefreshMyPermissionsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || !_authService.IsAuthenticated)
        {
            CurrentPermissions = null;
            PermissionChanged?.Invoke(null);
            return null;
        }

        var row = await GetMyPermissionCoreAsync(cancellationToken);
        if (row is null)
        {
            row = new UserPermissionRow
            {
                UserId = _authService.CurrentSession?.UserId ?? string.Empty,
                Email = _authService.CurrentSession?.Email ?? string.Empty,
                IsAdmin = false,
                CanViewPool = true,
                CanUploadPool = true,
                CanEditReports = true
            };
        }

        CurrentPermissions = row;
        PermissionChanged?.Invoke(CurrentPermissions);
        return CurrentPermissions;
    }

    public async Task<IReadOnlyList<UserPermissionRow>> GetAllUsersAsync(int limit = 500, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || !_authService.IsAuthenticated || !IsAdmin)
        {
            return Array.Empty<UserPermissionRow>();
        }

        var safeLimit = Math.Clamp(limit, 1, 1000);
        var baseUrl = _options.Url.TrimEnd('/');
        var query = $"{baseUrl}/rest/v1/{_options.PermissionsTable}?select=user_id,email,is_admin,can_view_pool,can_upload_pool,can_edit_reports,updated_at,updated_by&order=updated_at.desc.nullslast,email.asc&limit={safeLimit}";

        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        AddSupabaseHeaders(request, includePrefer: false);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<UserPermissionRow>();
        }

        var rows = await response.Content.ReadFromJsonAsync<List<UserPermissionRow>>(cancellationToken: cancellationToken);
        return rows ?? new List<UserPermissionRow>();
    }

    public async Task<SupabaseOperationResult> UpdateUserPermissionsAsync(UserPermissionRow row, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return SupabaseOperationResult.Failure("Supabase ayarları eksik.");
        }

        if (!_authService.IsAuthenticated || !IsAdmin || _authService.CurrentSession is null)
        {
            return SupabaseOperationResult.Failure("Bu işlem için yönetici yetkisi gerekiyor.");
        }

        if (string.IsNullOrWhiteSpace(row.UserId))
        {
            return SupabaseOperationResult.Failure("Geçerli bir kullanıcı seçilmedi.");
        }

        var baseUrl = _options.Url.TrimEnd('/');
        var updateUrl = $"{baseUrl}/rest/v1/{_options.PermissionsTable}?user_id=eq.{Uri.EscapeDataString(row.UserId)}";

        var payload = new
        {
            email = row.Email,
            is_admin = row.IsAdmin,
            can_view_pool = row.CanViewPool,
            can_upload_pool = row.CanUploadPool,
            can_edit_reports = row.CanEditReports,
            updated_by = _authService.CurrentSession.UserId
        };

        using var request = new HttpRequestMessage(HttpMethod.Patch, updateUrl);
        AddSupabaseHeaders(request, includePrefer: true, prefer: "return=representation");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return SupabaseOperationResult.Failure($"Kullanıcı yetkisi güncellenemedi: {(int)response.StatusCode} - {body}");
        }

        if (string.Equals(row.UserId, _authService.CurrentSession.UserId, StringComparison.OrdinalIgnoreCase))
        {
            var updatedRows = await response.Content.ReadFromJsonAsync<List<UserPermissionRow>>(cancellationToken: cancellationToken);
            CurrentPermissions = updatedRows?.FirstOrDefault() ?? row;
            PermissionChanged?.Invoke(CurrentPermissions);
        }

        return SupabaseOperationResult.Success("Kullanıcı yetkisi güncellendi.");
    }

    private async Task<UserPermissionRow?> GetMyPermissionCoreAsync(CancellationToken cancellationToken)
    {
        var userId = _authService.CurrentSession?.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var baseUrl = _options.Url.TrimEnd('/');
        var query = $"{baseUrl}/rest/v1/{_options.PermissionsTable}?select=user_id,email,is_admin,can_view_pool,can_upload_pool,can_edit_reports,updated_at,updated_by&user_id=eq.{Uri.EscapeDataString(userId)}&limit=1";

        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        AddSupabaseHeaders(request, includePrefer: false);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var rows = await response.Content.ReadFromJsonAsync<List<UserPermissionRow>>(cancellationToken: cancellationToken);
        return rows?.FirstOrDefault();
    }

    private void AddSupabaseHeaders(HttpRequestMessage request, bool includePrefer, string? prefer = null)
    {
        request.Headers.Add("apikey", _options.PublishableKey);
        var accessToken = _authService.CurrentSession?.AccessToken;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.PublishableKey);
        }
        else
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (includePrefer)
        {
            request.Headers.Add("Prefer", prefer ?? "return=representation");
        }
    }
}