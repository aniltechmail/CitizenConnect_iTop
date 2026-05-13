using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Complaint
{
    public class ComplaintITopMapping
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplaintId { get; set; }
        public Complaint Complaint { get; set; } = null!;
        public string ITopTicketRef { get; set; } = string.Empty;
        public string ITopTicketId { get; set; } = string.Empty;
        public string ITopClass { get; set; } = "UserRequest";
        public DateTime? LastSyncedAt { get; set; }
        public SyncStatus SyncStatus { get; set; } = SyncStatus.PendingSync;
        public string? LastSyncError { get; set; }
    }
}
