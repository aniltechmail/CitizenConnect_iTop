using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Enums
{
    public enum ComplaintStatus
    {
        Draft = 0,
        Submitted = 1,
        Assigned = 2,
        InProgress = 3,
        Resolved = 4,
        Closed = 5,
        Rejected = 6
    }
}
