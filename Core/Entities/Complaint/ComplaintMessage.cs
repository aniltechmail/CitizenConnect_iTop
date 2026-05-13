using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Complaint
{
    public class ComplaintMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplaintId { get; set; }
        public Complaint Complaint { get; set; } = null!;
        public SenderType SenderType { get; set; }
        public Guid SenderId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
