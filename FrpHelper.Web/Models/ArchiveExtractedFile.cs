namespace FrpHelper.Web.Models;

public sealed class ArchiveExtractedFile
{
    public string FileName { get; init; } = string.Empty;

    public byte[] Content { get; init; } = Array.Empty<byte>();
}
