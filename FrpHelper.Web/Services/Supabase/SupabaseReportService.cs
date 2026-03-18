using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FrpHelper.Web.Configuration;
using FrpHelper.Web.Models;
using FrpHelper.Web.Services.Auth;
using Microsoft.Extensions.Options;

namespace FrpHelper.Web.Services.Supabase;

public sealed class SupabaseReportService(
    HttpClient httpClient,
    IOptions<SupabaseOptions> options,
    IAuthService authService) : ISupabaseReportService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly SupabaseOptions _options = options.Value;
    private readonly IAuthService _authService = authService;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Url) &&
        !string.IsNullOrWhiteSpace(_options.PublishableKey);

    public async Task<SupabaseOperationResult> UploadReportAsync(FrpReportDocument report, string description, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return SupabaseOperationResult.Failure("Supabase ayarları eksik. appsettings.json içindeki Supabase bölümünü kontrol edin.");
        }

        if (!_authService.IsAuthenticated)
        {
            return SupabaseOperationResult.Failure("Yükleme için giriş yapmalısınız.");
        }

        var baseUrl = _options.Url.TrimEnd('/');
        var storagePath = BuildStoragePath(report.FileName);
        var storageBucket = _options.StorageBucket;
        var publicUrl = $"{baseUrl}/storage/v1/object/public/{storageBucket}/{storagePath}";

        var uploadUrl = $"{baseUrl}/storage/v1/object/{storageBucket}/{storagePath}";
        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        AddSupabaseHeaders(uploadRequest, includePrefer: false);
        uploadRequest.Headers.Add("x-upsert", "true");
        uploadRequest.Content = new ByteArrayContent(report.SourceBytes);
        uploadRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var uploadResponse = await _httpClient.SendAsync(uploadRequest, cancellationToken);
        if (!uploadResponse.IsSuccessStatusCode)
        {
            var body = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);
            return SupabaseOperationResult.Failure($"Storage yükleme hatası: {(int)uploadResponse.StatusCode} - {body}");
        }

        var insertUrl = $"{baseUrl}/rest/v1/{_options.ReportsTable}";
        var payload = new[]
        {
            new
            {
                report_name = report.Metadata.ReportName,
                report_code = report.Metadata.ReportCode,
                description,
                sql_content = string.Join("\n\n", report.SqlSections.Select(section => $"-- {section.Title}\n{section.Content}")),
                pascal_content = string.Join("\n\n", report.ScriptSections.Select(section => $"-- {section.Title}\n{section.Content}")),
                file_path = storagePath,
                file_url = publicUrl,
                source_created_at = report.Metadata.CreatedAt,
                source_modified_at = report.Metadata.ModifiedAt,
                owner_id = _authService.CurrentSession?.UserId
            }
        };

        using var insertRequest = new HttpRequestMessage(HttpMethod.Post, insertUrl);
        AddSupabaseHeaders(insertRequest, includePrefer: true);
        insertRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var insertResponse = await _httpClient.SendAsync(insertRequest, cancellationToken);
        if (!insertResponse.IsSuccessStatusCode)
        {
            var body = await insertResponse.Content.ReadAsStringAsync(cancellationToken);
            return SupabaseOperationResult.Failure($"DB kayıt hatası: {(int)insertResponse.StatusCode} - {body}");
        }

        return SupabaseOperationResult.Success("Rapor Supabase Storage ve tabloya başarıyla kaydedildi.");
    }

    public async Task<IReadOnlyList<SupabaseReportRow>> GetRecentReportsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return Array.Empty<SupabaseReportRow>();
        }

        if (!_authService.IsAuthenticated)
        {
            return Array.Empty<SupabaseReportRow>();
        }

        var safeLimit = Math.Clamp(limit, 1, 200);
        var baseUrl = _options.Url.TrimEnd('/');
        var query = $"{baseUrl}/rest/v1/{_options.ReportsTable}?select=id,report_name,report_code,description,file_url,created_at&order=created_at.desc&limit={safeLimit}";

        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        AddSupabaseHeaders(request, includePrefer: false);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<SupabaseReportRow>();
        }

        var rows = await response.Content.ReadFromJsonAsync<List<SupabaseReportRow>>(cancellationToken: cancellationToken);
        return rows ?? new List<SupabaseReportRow>();
    }

    private void AddSupabaseHeaders(HttpRequestMessage request, bool includePrefer)
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
            request.Headers.Add("Prefer", "return=representation");
        }
    }

    private static string BuildStoragePath(string fileName)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var safeName = Regex.Replace(nameWithoutExtension, "[^a-zA-Z0-9_-]", "_");
        var prefix = DateTime.UtcNow.ToString("yyyy/MM/dd");

        return $"{prefix}/{Guid.NewGuid():N}_{safeName}{extension}";
    }
}
