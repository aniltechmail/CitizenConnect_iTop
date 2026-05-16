from datetime import datetime
from sqlalchemy import Integer, String, Boolean, DateTime, ForeignKey, BigInteger, Uuid, Text
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.database import Base
import uuid


class Complaint(Base):
    __tablename__ = "complaints"

    id: Mapped[uuid.UUID] = mapped_column("Id", Uuid, primary_key=True, default=uuid.uuid4)
    ref_number: Mapped[str] = mapped_column("RefNumber", String(20), nullable=False, unique=True)
    title: Mapped[str] = mapped_column("Title", String(500), nullable=False)
    description: Mapped[str] = mapped_column("Description", Text, nullable=False)
    status: Mapped[int] = mapped_column("Status", Integer, nullable=False, default=1)
    priority: Mapped[int] = mapped_column("Priority", Integer, nullable=False, default=3)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True))
    updated_at: Mapped[datetime] = mapped_column("UpdatedAt", DateTime(timezone=True))
    submitted_at: Mapped[datetime | None] = mapped_column("SubmittedAt", DateTime(timezone=True), nullable=True)
    assigned_at: Mapped[datetime | None] = mapped_column("AssignedAt", DateTime(timezone=True), nullable=True)
    resolved_at: Mapped[datetime | None] = mapped_column("ResolvedAt", DateTime(timezone=True), nullable=True)
    closed_at: Mapped[datetime | None] = mapped_column("ClosedAt", DateTime(timezone=True), nullable=True)
    is_sla_breached: Mapped[bool] = mapped_column("IsSlaBreached", Boolean, default=False)

    citizen_id: Mapped[uuid.UUID] = mapped_column("CitizenId", Uuid, ForeignKey("citizens.Id"), nullable=False)
    block_id: Mapped[int] = mapped_column("BlockId", Integer, ForeignKey("blocks.Id"), nullable=False)
    category_id: Mapped[int] = mapped_column("CategoryId", Integer, ForeignKey("complaint_categories.Id"), nullable=False)
    assigned_department_id: Mapped[int | None] = mapped_column("AssignedDepartmentId", Integer, ForeignKey("departments.Id"), nullable=True)
    assigned_agent_id: Mapped[uuid.UUID | None] = mapped_column("AssignedAgentId", Uuid, ForeignKey("internal_users.Id"), nullable=True)

    citizen: Mapped["Citizen"] = relationship("Citizen", foreign_keys=[citizen_id])
    block: Mapped["Block"] = relationship("Block", foreign_keys=[block_id])
    category: Mapped["ComplaintCategory"] = relationship("ComplaintCategory", foreign_keys=[category_id])
    assigned_department: Mapped["Department"] = relationship("Department", foreign_keys=[assigned_department_id])
    assigned_agent: Mapped["InternalUser"] = relationship("InternalUser", foreign_keys=[assigned_agent_id])
    media: Mapped[list["ComplaintMedia"]] = relationship("ComplaintMedia", back_populates="complaint")
    messages: Mapped[list["ComplaintMessage"]] = relationship("ComplaintMessage", back_populates="complaint")
    feedback: Mapped["ComplaintFeedback | None"] = relationship("ComplaintFeedback", back_populates="complaint", uselist=False)
    itop_mapping: Mapped["ComplaintITopMapping | None"] = relationship("ComplaintITopMapping", back_populates="complaint", uselist=False)


class ComplaintMedia(Base):
    __tablename__ = "ComplaintMedia"

    id: Mapped[uuid.UUID] = mapped_column("Id", Uuid, primary_key=True, default=uuid.uuid4)
    complaint_id: Mapped[uuid.UUID] = mapped_column("ComplaintId", Uuid, ForeignKey("complaints.Id"), nullable=False)
    media_type: Mapped[int] = mapped_column("MediaType", Integer, nullable=False)
    file_name: Mapped[str] = mapped_column("FileName", Text, nullable=False)
    file_path: Mapped[str] = mapped_column("FilePath", Text, nullable=False)
    file_size: Mapped[int] = mapped_column("FileSize", BigInteger, nullable=False)
    mime_type: Mapped[str | None] = mapped_column("MimeType", Text, nullable=True)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True))

    complaint: Mapped["Complaint"] = relationship("Complaint", back_populates="media")


class ComplaintMessage(Base):
    __tablename__ = "ComplaintMessages"

    id: Mapped[uuid.UUID] = mapped_column("Id", Uuid, primary_key=True, default=uuid.uuid4)
    complaint_id: Mapped[uuid.UUID] = mapped_column("ComplaintId", Uuid, ForeignKey("complaints.Id"), nullable=False)
    sender_type: Mapped[int] = mapped_column("SenderType", Integer, nullable=False)
    sender_id: Mapped[uuid.UUID] = mapped_column("SenderId", Uuid, nullable=False)
    message: Mapped[str] = mapped_column("Message", Text, nullable=False)
    is_read: Mapped[bool] = mapped_column("IsRead", Boolean, default=False)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True))

    complaint: Mapped["Complaint"] = relationship("Complaint", back_populates="messages")


class ComplaintFeedback(Base):
    __tablename__ = "ComplaintFeedbacks"

    id: Mapped[uuid.UUID] = mapped_column("Id", Uuid, primary_key=True, default=uuid.uuid4)
    complaint_id: Mapped[uuid.UUID] = mapped_column("ComplaintId", Uuid, ForeignKey("complaints.Id"), nullable=False)
    citizen_id: Mapped[uuid.UUID] = mapped_column("CitizenId", Uuid, ForeignKey("citizens.Id"), nullable=False)
    rating: Mapped[int] = mapped_column("Rating", Integer, nullable=False)
    comments: Mapped[str | None] = mapped_column("Comments", Text, nullable=True)
    collected_by_id: Mapped[uuid.UUID | None] = mapped_column("CollectedById", Uuid, ForeignKey("internal_users.Id"), nullable=True)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True))

    complaint: Mapped["Complaint"] = relationship("Complaint", back_populates="feedback")


class ComplaintITopMapping(Base):
    __tablename__ = "ComplaintITopMappings"

    id: Mapped[uuid.UUID] = mapped_column("Id", Uuid, primary_key=True, default=uuid.uuid4)
    complaint_id: Mapped[uuid.UUID] = mapped_column("ComplaintId", Uuid, ForeignKey("complaints.Id"), nullable=False)
    itop_ticket_ref: Mapped[str] = mapped_column("ITopTicketRef", Text, nullable=False)
    itop_ticket_id: Mapped[str] = mapped_column("ITopTicketId", Text, nullable=False)
    itop_class: Mapped[str] = mapped_column("ITopClass", Text, nullable=False, default="UserRequest")
    last_synced_at: Mapped[datetime | None] = mapped_column("LastSyncedAt", DateTime(timezone=True), nullable=True)
    sync_status: Mapped[int] = mapped_column("SyncStatus", Integer, nullable=False, default=0)
    last_sync_error: Mapped[str | None] = mapped_column("LastSyncError", Text, nullable=True)

    complaint: Mapped["Complaint"] = relationship("Complaint", back_populates="itop_mapping")


class EscalationEvent(Base):
    __tablename__ = "escalation_events"

    id: Mapped[uuid.UUID] = mapped_column("Id", Uuid, primary_key=True, default=uuid.uuid4)
    complaint_id: Mapped[uuid.UUID] = mapped_column("ComplaintId", Uuid, ForeignKey("complaints.Id"), nullable=False)
    level: Mapped[int] = mapped_column("Level", Integer, nullable=False)
    reason: Mapped[str] = mapped_column("Reason", Text, nullable=False)
    triggered_at: Mapped[datetime] = mapped_column("TriggeredAt", DateTime(timezone=True))
    resolved_at: Mapped[datetime | None] = mapped_column("ResolvedAt", DateTime(timezone=True), nullable=True)

    complaint: Mapped["Complaint"] = relationship("Complaint")
