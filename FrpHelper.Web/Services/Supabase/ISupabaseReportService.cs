using FrpHelper.Web.Models;

namespace FrpHelper.Web.Services.Supabase;

public interface ISupabaseReportService
{
    bool IsConfigured { get; }

    Task<SupabaseOperationResult> UploadReportAsync(FrpReportDocument report, string description, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupabaseReportRow>> GetRecentReportsAsync(int limit = 50, CancellationToken cancellationToken = default);

    Task<SupabaseOperationResult> UpdateReportMetadataAsync(SupabaseReportRow row, CancellationToken cancellationToken = default);

    Task<SupabaseOperationResult> DeleteReportAsync(SupabaseReportRow row, CancellationToken cancellationToken = default);
}
