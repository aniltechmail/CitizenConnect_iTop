using System.Globalization;
using System.Text;
using Core.DTOs.Reports;
using Core.Entities.Complaint;
using Core.Enums;
using Core.Interfaces.Services;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Reporting
{
    public class ReportingService : IReportingService
    {
        private readonly AppDbContext _db;
        public ReportingService(AppDbContext db) => _db = db;

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            var complaints = await BaseComplaintQuery().ToListAsync();
            var escalations = await _db.EscalationEvents.CountAsync();

            return new DashboardSummaryDto
            {
                ComplaintsByStatus = complaints
                    .GroupBy(c => c.Status.ToString())
                    .Select(g => new NamedCountDto { Name = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Name),
                ComplaintsByDepartment = complaints
                    .GroupBy(c => c.AssignedDepartment?.Name ?? "Unassigned")
                    .Select(g => new NamedCountDto { Name = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Name),
                ComplaintsByLocation = complaints
                    .GroupBy(c => new
                    {
                        District = c.Block.Area.Constituency.District.Name,
                        Constituency = c.Block.Area.Constituency.Name,
                        Area = c.Block.Area.Name,
                        Block = c.Block.Name
                    })
                    .Select(g => new LocationCountDto
                    {
                        District = g.Key.District,
                        Constituency = g.Key.Constituency,
                        Area = g.Key.Area,
                        Block = g.Key.Block,
                        Count = g.Count()
                    }),
                AverageResolutionHours = AverageResolutionHours(complaints),
                SlaBreachCount = complaints.Count(c => c.IsSlaBreached),
                EscalationCount = escalations
            };
        }

        public async Task<PagedComplaintReportDto> GetComplaintReportAsync(
            DateTime? fromDate,
            DateTime? toDate,
            ComplaintStatus? status,
            int? departmentId,
            int? districtId,
            int? constituencyId,
            int? areaId,
            int? blockId,
            int page,
            int pageSize)
        {
            var query = ApplyFilters(BaseComplaintQuery(), fromDate, toDate, status, departmentId, districtId, constituencyId, areaId, blockId);
            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedComplaintReportDto
            {
                Items = items.Select(MapComplaint),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<string> ExportComplaintReportCsvAsync(
            DateTime? fromDate,
            DateTime? toDate,
            ComplaintStatus? status,
            int? departmentId,
            int? districtId,
            int? constituencyId,
            int? areaId,
            int? blockId)
        {
            var items = await ApplyFilters(BaseComplaintQuery(), fromDate, toDate, status, departmentId, districtId, constituencyId, areaId, blockId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("RefNumber,Title,Status,Category,Department,District,Constituency,Area,Block,CreatedAt,SubmittedAt,ResolvedAt,IsSlaBreached");
            foreach (var item in items.Select(MapComplaint))
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(item.RefNumber), Csv(item.Title), Csv(item.Status), Csv(item.Category), Csv(item.Department ?? ""),
                    Csv(item.District), Csv(item.Constituency), Csv(item.Area), Csv(item.Block),
                    Csv(item.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
                    Csv(item.SubmittedAt?.ToString("O", CultureInfo.InvariantCulture) ?? ""),
                    Csv(item.ResolvedAt?.ToString("O", CultureInfo.InvariantCulture) ?? ""),
                    Csv(item.IsSlaBreached.ToString())
                }));
            }
            return sb.ToString();
        }

        public async Task<IEnumerable<DepartmentPerformanceDto>> GetDepartmentPerformanceAsync()
        {
            var complaints = await BaseComplaintQuery().ToListAsync();
            return complaints
                .GroupBy(c => new { c.AssignedDepartmentId, Name = c.AssignedDepartment?.Name ?? "Unassigned" })
                .Select(g =>
                {
                    var total = g.Count();
                    return new DepartmentPerformanceDto
                    {
                        DepartmentId = g.Key.AssignedDepartmentId,
                        Department = g.Key.Name,
                        TotalAssigned = total,
                        ResolvedCount = g.Count(c => c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed),
                        AverageResolutionHours = AverageResolutionHours(g),
                        SlaBreachRate = total == 0 ? 0 : Math.Round(g.Count(c => c.IsSlaBreached) * 100.0 / total, 2)
                    };
                })
                .OrderBy(x => x.Department);
        }

        public async Task<IEnumerable<AgentPerformanceDto>> GetAgentPerformanceAsync()
        {
            var complaints = await BaseComplaintQuery().ToListAsync();
            return complaints
                .Where(c => c.AssignedAgentId.HasValue)
                .GroupBy(c => new { c.AssignedAgentId, Name = c.AssignedAgent?.FullName ?? "Unknown" })
                .Select(g => new AgentPerformanceDto
                {
                    AgentId = g.Key.AssignedAgentId,
                    AgentName = g.Key.Name,
                    ComplaintsHandled = g.Count(),
                    AverageResolutionHours = AverageResolutionHours(g),
                    AverageFeedbackRating = Math.Round(g.Where(c => c.Feedback != null).Select(c => c.Feedback!.Rating).DefaultIfEmpty(0).Average(), 2)
                })
                .OrderBy(x => x.AgentName);
        }

        public async Task<IEnumerable<LocationReportDto>> GetLocationReportAsync()
        {
            var complaints = await BaseComplaintQuery().ToListAsync();
            return complaints
                .GroupBy(c => new
                {
                    District = c.Block.Area.Constituency.District.Name,
                    Constituency = c.Block.Area.Constituency.Name,
                    Area = c.Block.Area.Name,
                    Block = c.Block.Name
                })
                .Select(g => new LocationReportDto
                {
                    District = g.Key.District,
                    Constituency = g.Key.Constituency,
                    Area = g.Key.Area,
                    Block = g.Key.Block,
                    Count = g.Count()
                })
                .OrderBy(x => x.District)
                .ThenBy(x => x.Constituency)
                .ThenBy(x => x.Area)
                .ThenBy(x => x.Block);
        }

        private IQueryable<Complaint> BaseComplaintQuery() =>
            _db.Complaints
                .AsNoTracking()
                .Include(c => c.Category)
                .Include(c => c.AssignedDepartment)
                .Include(c => c.AssignedAgent)
                .Include(c => c.Feedback)
                .Include(c => c.Block)
                    .ThenInclude(b => b.Area)
                    .ThenInclude(a => a.Constituency)
                    .ThenInclude(c => c.District);

        private static IQueryable<Complaint> ApplyFilters(
            IQueryable<Complaint> query,
            DateTime? fromDate,
            DateTime? toDate,
            ComplaintStatus? status,
            int? departmentId,
            int? districtId,
            int? constituencyId,
            int? areaId,
            int? blockId)
        {
            var normalizedFromDate = NormalizeUtc(fromDate);
            var normalizedToDate = NormalizeUtc(toDate);

            if (normalizedFromDate.HasValue) query = query.Where(c => c.CreatedAt >= normalizedFromDate.Value);
            if (normalizedToDate.HasValue) query = query.Where(c => c.CreatedAt <= normalizedToDate.Value);
            if (status.HasValue) query = query.Where(c => c.Status == status.Value);
            if (departmentId.HasValue) query = query.Where(c => c.AssignedDepartmentId == departmentId.Value);
            if (districtId.HasValue) query = query.Where(c => c.Block.Area.Constituency.DistrictId == districtId.Value);
            if (constituencyId.HasValue) query = query.Where(c => c.Block.Area.ConstituencyId == constituencyId.Value);
            if (areaId.HasValue) query = query.Where(c => c.Block.AreaId == areaId.Value);
            if (blockId.HasValue) query = query.Where(c => c.BlockId == blockId.Value);
            return query;
        }

        private static DateTime? NormalizeUtc(DateTime? value)
        {
            if (!value.HasValue)
                return null;

            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            };
        }

        private static ComplaintReportItemDto MapComplaint(Complaint c) => new()
        {
            Id = c.Id,
            RefNumber = c.RefNumber,
            Title = c.Title,
            Status = c.Status.ToString(),
            Category = c.Category?.Name ?? "",
            Department = c.AssignedDepartment?.Name,
            District = c.Block.Area.Constituency.District.Name,
            Constituency = c.Block.Area.Constituency.Name,
            Area = c.Block.Area.Name,
            Block = c.Block.Name,
            CreatedAt = c.CreatedAt,
            SubmittedAt = c.SubmittedAt,
            ResolvedAt = c.ResolvedAt,
            IsSlaBreached = c.IsSlaBreached
        };

        private static double AverageResolutionHours(IEnumerable<Complaint> complaints)
        {
            var durations = complaints
                .Where(c => c.ResolvedAt.HasValue)
                .Select(c => (c.ResolvedAt!.Value - (c.SubmittedAt ?? c.CreatedAt)).TotalHours)
                .ToList();
            return durations.Count == 0 ? 0 : Math.Round(durations.Average(), 2);
        }

        private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
