from datetime import datetime
from pydantic import BaseModel
import uuid


class EscalationEventResponseSchema(BaseModel):
    id: uuid.UUID
    complaint_id: uuid.UUID
    complaint_ref_number: str
    level: int
    reason: str
    triggered_at: datetime
    resolved_at: datetime | None = None

    model_config = {"from_attributes": True}
