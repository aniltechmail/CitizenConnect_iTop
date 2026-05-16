namespace Core.DTOs.Reports
{
    public class DashboardSummaryDto
    {
        public IEnumerable<NamedCountDto> ComplaintsByStatus { get; set; } = [];
        public IEnumerable<NamedCountDto> ComplaintsByDepartment { get; set; } = [];
        public IEnumerable<LocationCountDto> ComplaintsByLocation { get; set; } = [];
        public double AverageResolutionHours { get; set; }
        public int SlaBreachCount { get; set; }
        public int EscalationCount { get; set; }
    }

    public class NamedCountDto
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class LocationCountDto
    {
        public string District { get; set; } = string.Empty;
        public string Constituency { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Block { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ComplaintReportItemDto
    {
        public Guid Id { get; set; }
        public string RefNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string District { get; set; } = string.Empty;
        public string Constituency { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Block { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public bool IsSlaBreached { get; set; }
    }

    public class PagedComplaintReportDto
    {
        public IEnumerable<ComplaintReportItemDto> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class DepartmentPerformanceDto
    {
        public int? DepartmentId { get; set; }
        public string Department { get; set; } = string.Empty;
        public int TotalAssigned { get; set; }
        public int ResolvedCount { get; set; }
        public double AverageResolutionHours { get; set; }
        public double SlaBreachRate { get; set; }
    }

    public class AgentPerformanceDto
    {
        public Guid? AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public int ComplaintsHandled { get; set; }
        public double AverageResolutionHours { get; set; }
        public double AverageFeedbackRating { get; set; }
    }

    public class LocationReportDto : LocationCountDto
    {
    }
}
