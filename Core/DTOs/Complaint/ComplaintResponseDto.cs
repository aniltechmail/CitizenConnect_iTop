using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Complaint
{
    public class ComplaintResponseDto
    {
        public Guid Id { get; set; }
        public string RefNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Citizen
        public Guid CitizenId { get; set; }
        public string CitizenName { get; set; } = string.Empty;
        public string CitizenPhone { get; set; } = string.Empty;

        // Location
        public int BlockId { get; set; }
        public string BlockName { get; set; } = string.Empty;

        // Category
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        // Department
        public int? AssignedDepartmentId { get; set; }
        public string? AssignedDepartmentName { get; set; }

        // Agent
        public Guid? AssignedAgentId { get; set; }
        public string? AssignedAgentName { get; set; }

        // Media
        public List<ComplaintMediaResponseDto> Media { get; set; } = new();
    }
}
