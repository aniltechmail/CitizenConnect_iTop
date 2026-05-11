using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Complaint
{
    public class ComplaintMedia
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ComplaintId { get; set; }
        public Complaint Complaint { get; set; } = null!;
        public MediaType MediaType { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? MimeType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
