from datetime import datetime
from pydantic import BaseModel
import uuid


class NotificationType:
    ComplaintSubmitted = 0
    ComplaintAssigned = 1
    ComplaintInProgress = 2
    ComplaintResolved = 3
    EscalationTriggered = 4
    FeedbackRequested = 5
    MessageReceived = 6

    @staticmethod
    def to_string(value: int) -> str:
        mapping = {
            0: "ComplaintSubmitted",
            1: "ComplaintAssigned",
            2: "ComplaintInProgress",
            3: "ComplaintResolved",
            4: "EscalationTriggered",
            5: "FeedbackRequested",
            6: "MessageReceived",
        }
        return mapping.get(value, "Unknown")


class NotificationResponseSchema(BaseModel):
    id: uuid.UUID
    user_type: str
    user_id: uuid.UUID
    complaint_id: uuid.UUID | None
    type: str
    message: str
    is_read: bool
    created_at: datetime

    model_config = {"from_attributes": True}
