using FrpHelper.Web.Models;

namespace FrpHelper.Web.Services.Archive;

public interface IArchiveService
{
    Task<IReadOnlyList<ArchiveExtractedFile>> ExpandAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);
}
