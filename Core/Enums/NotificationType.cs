using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Enums
{
    public enum NotificationType
    {
        ComplaintSubmitted = 0,
        ComplaintAssigned = 1,
        ComplaintInProgress = 2,
        ComplaintResolved = 3,
        EscalationTriggered = 4,
        FeedbackRequested = 5,
        MessageReceived = 6
    }
}
