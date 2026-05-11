using Core.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Complaint
{
    public class ComplaintFeedback
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplaintId { get; set; }
        public Complaint Complaint { get; set; } = null!;
        public Guid CitizenId { get; set; }
        public Citizen Citizen { get; set; } = null!;
        public int Rating { get; set; } // 1-5
        public string? Comments { get; set; }
        public Guid? CollectedById { get; set; }
        public InternalUser? CollectedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
