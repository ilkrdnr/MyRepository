using FrpHelper.Web.Models;

namespace FrpHelper.Web.Services.Parsing;

public interface IReportParserService
{
    Task<FrpReportDocument> ParseAsync(string fileName, Stream fileStream, CancellationToken cancellationToken = default);
}
