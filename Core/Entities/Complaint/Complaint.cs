using Core.Entities.Identity;
using Core.Entities.Location;
using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Complaint
{
    public class Complaint
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RefNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ComplaintStatus Status { get; set; } = ComplaintStatus.Draft;
        public int Priority { get; set; } = 3; // 1=Critical 2=High 3=Medium 4=Low
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public bool IsSlaBreached { get; set; } = false;

        public Guid CitizenId { get; set; }
        public Citizen Citizen { get; set; } = null!;

        public int BlockId { get; set; }
        public Block Block { get; set; } = null!;

        public int CategoryId { get; set; }
        public ComplaintCategory Category { get; set; } = null!;

        public int? AssignedDepartmentId { get; set; }
        public Department? AssignedDepartment { get; set; }

        public Guid? AssignedAgentId { get; set; }
        public InternalUser? AssignedAgent { get; set; }

        public ICollection<ComplaintMedia> Media { get; set; } = new List<ComplaintMedia>();
        public ICollection<ComplaintMessage> Messages { get; set; } = new List<ComplaintMessage>();
        public ComplaintFeedback? Feedback { get; set; }
        public ComplaintITopMapping? ITopMapping { get; set; }
    }
}
