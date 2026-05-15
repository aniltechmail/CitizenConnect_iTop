from pydantic import AliasChoices, BaseModel, Field, field_validator
from datetime import datetime
from typing import Optional
import uuid


# ── Enums ──────────────────────────────────────────────────────────────────

class ComplaintStatus:
    Draft = 0
    Submitted = 1
    Assigned = 2
    InProgress = 3
    Resolved = 4
    Closed = 5
    Rejected = 6

    @staticmethod
    def to_string(value: int) -> str:
        mapping = {
            0: "Draft", 1: "Submitted", 2: "Assigned",
            3: "InProgress", 4: "Resolved", 5: "Closed", 6: "Rejected"
        }
        return mapping.get(value, "Unknown")


class MediaTypeEnum:
    Image = 0
    Video = 1
    Voice = 2
    Document = 3

    @staticmethod
    def to_string(value: int) -> str:
        mapping = {0: "Image", 1: "Video", 2: "Voice", 3: "Document"}
        return mapping.get(value, "Unknown")


class SenderTypeEnum:
    Citizen = 0
    Agent = 1
    System = 2

    @staticmethod
    def to_string(value: int) -> str:
        mapping = {0: "Citizen", 1: "Agent", 2: "System"}
        return mapping.get(value, "Unknown")


# ── Request Schemas ────────────────────────────────────────────────────────

class SubmitComplaintSchema(BaseModel):
    model_config = {"populate_by_name": True}

    title: str
    description: str
    category_id: int = Field(
        validation_alias=AliasChoices("category_id", "categoryId", "CategoryId")
    )
    block_id: int = Field(
        validation_alias=AliasChoices("block_id", "blockId", "BlockId")
    )
    priority: int = 3

    @field_validator("priority")
    @classmethod
    def validate_priority(cls, v: int) -> int:
        if v not in [1, 2, 3, 4]:
            raise ValueError("Priority must be 1 (Critical), 2 (High), 3 (Medium), or 4 (Low)")
        return v

    @field_validator("title")
    @classmethod
    def validate_title(cls, v: str) -> str:
        if len(v.strip()) == 0:
            raise ValueError("Title cannot be empty")
        if len(v) > 500:
            raise ValueError("Title cannot exceed 500 characters")
        return v.strip()


class AssignDepartmentSchema(BaseModel):
    model_config = {"populate_by_name": True}

    department_id: int = Field(
        validation_alias=AliasChoices("department_id", "departmentId", "DepartmentId")
    )


class AssignAgentSchema(BaseModel):
    model_config = {"populate_by_name": True}

    agent_id: uuid.UUID = Field(
        validation_alias=AliasChoices("agent_id", "agentId", "AgentId")
    )


class UpdateStatusSchema(BaseModel):
    status: int
    remarks: Optional[str] = None

    @field_validator("status")
    @classmethod
    def validate_status(cls, v: int) -> int:
        if v not in [0, 1, 2, 3, 4, 5, 6]:
            raise ValueError("Invalid status value")
        return v


class SendMessageSchema(BaseModel):
    message: str
    sender_type: Optional[int] = None

    @field_validator("message")
    @classmethod
    def validate_message(cls, v: str) -> str:
        if not v.strip():
            raise ValueError("Message is required")
        return v.strip()

    @field_validator("sender_type")
    @classmethod
    def validate_sender_type(cls, v: int | None) -> int | None:
        if v is not None and v not in [0, 1, 2]:
            raise ValueError("Invalid sender type")
        return v


class SubmitFeedbackSchema(BaseModel):
    rating: int
    comments: Optional[str] = None

    @field_validator("rating")
    @classmethod
    def validate_rating(cls, v: int) -> int:
        if v < 1 or v > 5:
            raise ValueError("Rating must be between 1 and 5")
        return v


# ── Response Schemas ───────────────────────────────────────────────────────

class ComplaintMediaResponseSchema(BaseModel):
    id: uuid.UUID
    media_type: str
    file_name: str
    file_url: str
    file_size: int
    created_at: datetime

    model_config = {"from_attributes": True}


class ComplaintMessageResponseSchema(BaseModel):
    id: uuid.UUID
    complaint_id: uuid.UUID
    sender_type: str
    sender_id: uuid.UUID
    message: str
    is_read: bool
    created_at: datetime

    model_config = {"from_attributes": True}


class ComplaintFeedbackResponseSchema(BaseModel):
    id: uuid.UUID
    complaint_id: uuid.UUID
    citizen_id: uuid.UUID
    rating: int
    comments: Optional[str] = None
    collected_by_id: Optional[uuid.UUID] = None
    created_at: datetime

    model_config = {"from_attributes": True}


class ComplaintResponseSchema(BaseModel):
    id: uuid.UUID
    ref_number: str
    title: str
    description: str
    status: str
    priority: int
    created_at: datetime
    submitted_at: Optional[datetime] = None
    assigned_at: Optional[datetime] = None
    resolved_at: Optional[datetime] = None

    # Citizen
    citizen_id: uuid.UUID
    citizen_name: str
    citizen_phone: str

    # Location
    block_id: int
    block_name: str

    # Category
    category_id: int
    category_name: str

    # Department
    assigned_department_id: Optional[int] = None
    assigned_department_name: Optional[str] = None

    # Agent
    assigned_agent_id: Optional[uuid.UUID] = None
    assigned_agent_name: Optional[str] = None

    # Media
    media: list[ComplaintMediaResponseSchema] = []

    model_config = {"from_attributes": True}


class PagedComplaintsSchema(BaseModel):
    items: list[ComplaintResponseSchema]
    total_count: int
    page: int
    page_size: int
    total_pages: int
