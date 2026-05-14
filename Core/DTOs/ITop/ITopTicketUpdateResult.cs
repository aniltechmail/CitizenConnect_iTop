using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.ITop
{
    public class ITopTicketUpdateResult
    {
        public bool WasAttempted { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }

        public static ITopTicketUpdateResult Skipped(string reason) => new()
        {
            WasAttempted = false,
            Success = false,
            Error = reason
        };

        public static ITopTicketUpdateResult Failed(string error) => new()
        {
            WasAttempted = true,
            Success = false,
            Error = error
        };
    }
}
