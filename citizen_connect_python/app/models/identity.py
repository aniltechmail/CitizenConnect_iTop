from datetime import datetime
from enum import IntEnum as PyIntEnum
from sqlalchemy import Integer, String, Boolean, DateTime, ForeignKey, Uuid
from sqlalchemy.orm import Mapped, mapped_column, relationship
from app.database import Base
import uuid


class UserRole(PyIntEnum):
    Admin = 0
    Assigner = 1
    FieldAgent = 2
    Supervisor = 3
    DepartmentHead = 4
    TopManagement = 5


class Citizen(Base):
    __tablename__ = "citizens"

    id: Mapped[uuid.UUID] = mapped_column("Id", Uuid, primary_key=True, default=uuid.uuid4)
    full_name: Mapped[str] = mapped_column("FullName", String(200), nullable=False)
    phone: Mapped[str] = mapped_column("Phone", String(15), nullable=False, unique=True)
    email: Mapped[str | None] = mapped_column("Email", String(200), nullable=True)
    password_hash: Mapped[str] = mapped_column("PasswordHash", nullable=False)
    is_verified: Mapped[bool] = mapped_column("IsVerified", Boolean, default=False)
    is_active: Mapped[bool] = mapped_column("IsActive", Boolean, default=True)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True))
    last_login_at: Mapped[datetime | None] = mapped_column(
        "LastLoginAt", DateTime(timezone=True), nullable=True
    )
    block_id: Mapped[int] = mapped_column(
        "BlockId", Integer, ForeignKey("blocks.Id"), nullable=False
    )

    block: Mapped["Block"] = relationship(back_populates="citizens")


class InternalUser(Base):
    __tablename__ = "internal_users"

    id: Mapped[uuid.UUID] = mapped_column("Id", Uuid, primary_key=True, default=uuid.uuid4)
    full_name: Mapped[str] = mapped_column("FullName", String(200), nullable=False)
    email: Mapped[str] = mapped_column("Email", String(200), nullable=False, unique=True)
    password_hash: Mapped[str] = mapped_column("PasswordHash", nullable=False)
    role: Mapped[int] = mapped_column("Role", Integer, nullable=False)
    department_id: Mapped[int | None] = mapped_column(
        "DepartmentId", Integer, nullable=True
    )
    is_active: Mapped[bool] = mapped_column("IsActive", Boolean, default=True)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True))
    last_login_at: Mapped[datetime | None] = mapped_column(
        "LastLoginAt", DateTime(timezone=True), nullable=True
    )
