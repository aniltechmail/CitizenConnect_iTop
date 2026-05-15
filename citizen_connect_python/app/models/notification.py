from datetime import datetime
from sqlalchemy import Integer, String, Boolean, DateTime, ForeignKey, Uuid
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.database import Base
import uuid


class Notification(Base):
    __tablename__ = "notifications"

    id: Mapped[uuid.UUID] = mapped_column("Id", Uuid, primary_key=True, default=uuid.uuid4)
    user_type: Mapped[int] = mapped_column("UserType", Integer, nullable=False)
    user_id: Mapped[uuid.UUID] = mapped_column("UserId", Uuid, nullable=False)
    complaint_id: Mapped[uuid.UUID | None] = mapped_column(
        "ComplaintId", Uuid, ForeignKey("complaints.Id"), nullable=True
    )
    type: Mapped[int] = mapped_column("Type", Integer, nullable=False)
    message: Mapped[str] = mapped_column("Message", String(1000), nullable=False)
    is_read: Mapped[bool] = mapped_column("IsRead", Boolean, default=False)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True))

    complaint: Mapped["Complaint | None"] = relationship("Complaint")
