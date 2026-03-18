using System.IO.Compression;
using FrpHelper.Web.Models;
using SharpCompress.Archives.Rar;

namespace FrpHelper.Web.Services.Archive;

public sealed class ArchiveService : IArchiveService
{
    public Task<IReadOnlyList<ArchiveExtractedFile>> ExpandAsync(string fileName, byte[] content, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);

        if (extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<IReadOnlyList<ArchiveExtractedFile>>(ExtractFromZip(content));
        }

        if (extension.Equals(".rar", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<IReadOnlyList<ArchiveExtractedFile>>(ExtractFromRar(content));
        }

        return Task.FromResult<IReadOnlyList<ArchiveExtractedFile>>(
            new List<ArchiveExtractedFile>
            {
                new()
                {
                    FileName = fileName,
                    Content = content
                }
            });
    }

    private static List<ArchiveExtractedFile> ExtractFromZip(byte[] content)
    {
        using var input = new MemoryStream(content);
        using var archive = new ZipArchive(input, ZipArchiveMode.Read);

        var extracted = new List<ArchiveExtractedFile>();

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name) || !IsSupportedReport(entry.Name))
            {
                continue;
            }

            using var entryStream = entry.Open();
            using var output = new MemoryStream();
            entryStream.CopyTo(output);

            extracted.Add(new ArchiveExtractedFile
            {
                FileName = entry.Name,
                Content = output.ToArray()
            });
        }

        if (extracted.Count == 0)
        {
            throw new InvalidOperationException("ZIP içinde FRP/FRX/XML dosyası bulunamadı.");
        }

        return extracted;
    }

    private static List<ArchiveExtractedFile> ExtractFromRar(byte[] content)
    {
        using var input = new MemoryStream(content);
        using var archive = RarArchive.Open(input);
        var extracted = new List<ArchiveExtractedFile>();

        foreach (var entry in archive.Entries.Where(static e =>
                     !e.IsDirectory &&
                     !string.IsNullOrWhiteSpace(e.Key) &&
                     IsSupportedReport(e.Key!)))
        {
            using var entryStream = entry.OpenEntryStream();
            using var output = new MemoryStream();
            entryStream.CopyTo(output);

            extracted.Add(new ArchiveExtractedFile
            {
                FileName = Path.GetFileName(entry.Key!),
                Content = output.ToArray()
            });
        }

        if (extracted.Count == 0)
        {
            throw new InvalidOperationException("RAR içinde FRP/FRX/XML dosyası bulunamadı.");
        }

        return extracted;
    }

    private static bool IsSupportedReport(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".frp", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".frx", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".xml", StringComparison.OrdinalIgnoreCase);
    }
}
