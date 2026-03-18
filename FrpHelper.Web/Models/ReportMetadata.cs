namespace FrpHelper.Web.Models;

public sealed class ReportMetadata
{
    public string ReportName { get; set; } = string.Empty;

    public string ReportCode { get; set; } = string.Empty;

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public string Description { get; set; } = string.Empty;
}
