namespace FrpHelper.Web.Models;

public sealed class FrpReportDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string FileName { get; init; } = string.Empty;

    public string SourceFormat { get; init; } = "xml";

    public byte[] SourceBytes { get; init; } = Array.Empty<byte>();

    public string OriginalXml { get; init; } = string.Empty;

    public DateTimeOffset ImportedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public ReportMetadata Metadata { get; init; } = new();

    public List<ReportSqlSection> SqlSections { get; init; } = new();

    public List<ReportScriptSection> ScriptSections { get; init; } = new();
}
