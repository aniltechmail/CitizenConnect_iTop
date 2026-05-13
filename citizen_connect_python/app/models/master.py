from datetime import datetime
from sqlalchemy import Integer, String, Boolean, DateTime, ForeignKey
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.database import Base


class Department(Base):
    __tablename__ = "departments"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    name: Mapped[str] = mapped_column("Name", String(200), nullable=False)
    code: Mapped[str] = mapped_column("Code", String(20), nullable=False, unique=True)
    description: Mapped[str | None] = mapped_column("Description", nullable=True)
    is_active: Mapped[bool] = mapped_column("IsActive", Boolean, default=True)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True))


class ComplaintCategory(Base):
    __tablename__ = "complaint_categories"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    name: Mapped[str] = mapped_column("Name", String(200), nullable=False)
    description: Mapped[str | None] = mapped_column("Description", nullable=True)
    department_id: Mapped[int] = mapped_column(
        "DepartmentId", Integer, ForeignKey("departments.Id"), nullable=False
    )
    is_active: Mapped[bool] = mapped_column("IsActive", Boolean, default=True)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True))

    department: Mapped["Department"] = relationship()


class SlaPolicy(Base):
    __tablename__ = "sla_policies"

    id: Mapped[int] = mapped_column("Id", Integer, primary_key=True)
    category_id: Mapped[int] = mapped_column(
        "CategoryId", Integer, ForeignKey("complaint_categories.Id"), nullable=False
    )
    response_hours: Mapped[int] = mapped_column("ResponseHours", Integer)
    resolution_hours: Mapped[int] = mapped_column("ResolutionHours", Integer)
    escalation_level1_hours: Mapped[int] = mapped_column("EscalationLevel1Hours", Integer)
    escalation_level2_hours: Mapped[int] = mapped_column("EscalationLevel2Hours", Integer)
    escalation_level3_hours: Mapped[int] = mapped_column("EscalationLevel3Hours", Integer)
    is_active: Mapped[bool] = mapped_column("IsActive", Boolean, default=True)