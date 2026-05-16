using Core.DTOs.Reports;
using Core.Enums;

namespace Core.Interfaces.Services
{
    public interface IReportingService
    {
        Task<DashboardSummaryDto> GetDashboardSummaryAsync();
        Task<PagedComplaintReportDto> GetComplaintReportAsync(
            DateTime? fromDate,
            DateTime? toDate,
            ComplaintStatus? status,
            int? departmentId,
            int? districtId,
            int? constituencyId,
            int? areaId,
            int? blockId,
            int page,
            int pageSize);
        Task<string> ExportComplaintReportCsvAsync(
            DateTime? fromDate,
            DateTime? toDate,
            ComplaintStatus? status,
            int? departmentId,
            int? districtId,
            int? constituencyId,
            int? areaId,
            int? blockId);
        Task<IEnumerable<DepartmentPerformanceDto>> GetDepartmentPerformanceAsync();
        Task<IEnumerable<AgentPerformanceDto>> GetAgentPerformanceAsync();
        Task<IEnumerable<LocationReportDto>> GetLocationReportAsync();
    }
}
