using FrpHelper.Web.Models;

namespace FrpHelper.Web.Services.Export;

public interface IReportExportService
{
    Task DownloadReportXmlAsync(FrpReportDocument report, CancellationToken cancellationToken = default);
}
