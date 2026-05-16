from datetime import datetime
from pydantic import BaseModel
import uuid


class NamedCountSchema(BaseModel):
    name: str
    count: int


class LocationCountSchema(BaseModel):
    district: str
    constituency: str
    area: str
    block: str
    count: int


class DashboardSummarySchema(BaseModel):
    complaints_by_status: list[NamedCountSchema]
    complaints_by_department: list[NamedCountSchema]
    complaints_by_location: list[LocationCountSchema]
    average_resolution_hours: float
    sla_breach_count: int
    escalation_count: int


class ComplaintReportItemSchema(BaseModel):
    id: uuid.UUID
    ref_number: str
    title: str
    status: str
    category: str
    department: str | None = None
    district: str
    constituency: str
    area: str
    block: str
    created_at: datetime
    submitted_at: datetime | None = None
    resolved_at: datetime | None = None
    is_sla_breached: bool


class PagedComplaintReportSchema(BaseModel):
    items: list[ComplaintReportItemSchema]
    total_count: int
    page: int
    page_size: int


class DepartmentPerformanceSchema(BaseModel):
    department_id: int | None = None
    department: str
    total_assigned: int
    resolved_count: int
    average_resolution_hours: float
    sla_breach_rate: float


class AgentPerformanceSchema(BaseModel):
    agent_id: uuid.UUID | None = None
    agent_name: str
    complaints_handled: int
    average_resolution_hours: float
    average_feedback_rating: float


class LocationReportSchema(LocationCountSchema):
    pass
