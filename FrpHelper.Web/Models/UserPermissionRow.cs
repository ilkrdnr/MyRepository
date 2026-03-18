using System.Text.Json.Serialization;

namespace FrpHelper.Web.Models;

public sealed class UserPermissionRow
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("is_admin")]
    public bool IsAdmin { get; set; }

    [JsonPropertyName("can_view_pool")]
    public bool CanViewPool { get; set; } = true;

    [JsonPropertyName("can_upload_pool")]
    public bool CanUploadPool { get; set; } = true;

    [JsonPropertyName("can_edit_reports")]
    public bool CanEditReports { get; set; } = true;

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }

    [JsonPropertyName("updated_by")]
    public string? UpdatedBy { get; set; }
}