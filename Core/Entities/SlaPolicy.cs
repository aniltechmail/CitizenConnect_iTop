using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class SlaPolicy
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public ComplaintCategory Category { get; set; } = null!;
        public int ResponseHours { get; set; }
        public int ResolutionHours { get; set; }
        public int EscalationLevel1Hours { get; set; }
        public int EscalationLevel2Hours { get; set; }
        public int EscalationLevel3Hours { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
