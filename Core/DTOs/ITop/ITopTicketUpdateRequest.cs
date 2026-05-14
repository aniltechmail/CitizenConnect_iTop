using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.ITop
{
    public class ITopTicketUpdateRequest
    {
        public string ITopTicketId { get; set; } = string.Empty;
        public string ITopClass { get; set; } = "UserRequest";
        public string NewStatus { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string ComplaintRefNumber { get; set; } = string.Empty;
    }
}
