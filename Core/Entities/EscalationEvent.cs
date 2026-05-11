using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class EscalationEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplaintId { get; set; }
        public Complaint.Complaint Complaint { get; set; } = null!;
        public int Level { get; set; } // 1, 2, 3
        public string Reason { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}
