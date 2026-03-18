namespace FrpHelper.Web.Configuration;

public sealed class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;

    public string PublishableKey { get; set; } = string.Empty;

    public string StorageBucket { get; set; } = "frp-files";

    public string ReportsTable { get; set; } = "reports";
}
