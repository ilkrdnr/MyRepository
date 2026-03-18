using System.Text.Json.Serialization;

namespace FrpHelper.Web.Models;

public sealed class SupabaseReportRow
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("report_name")]
    public string ReportName { get; set; } = string.Empty;

    [JsonPropertyName("report_code")]
    public string ReportCode { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
