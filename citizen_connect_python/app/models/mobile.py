from datetime import datetime
from sqlalchemy import Boolean, DateTime, Integer, String, Uuid
from sqlalchemy.orm import Mapped, mapped_column
from app.database import Base
import uuid


class CitizenRefreshToken(Base):
    __tablename__ = "CitizenRefreshTokens"

    id: Mapped[uuid.UUID] = mapped_column("Id", Uuid, primary_key=True, default=uuid.uuid4)
    citizen_id: Mapped[uuid.UUID] = mapped_column("CitizenId", Uuid, nullable=False)
    token: Mapped[str] = mapped_column("Token", String(200), nullable=False, unique=True)
    expires_at: Mapped[datetime] = mapped_column("ExpiresAt", DateTime(timezone=True), nullable=False)
    is_revoked: Mapped[bool] = mapped_column("IsRevoked", Boolean, default=False)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True), nullable=False)


class CitizenOtp(Base):
    __tablename__ = "CitizenOtps"

    id: Mapped[uuid.UUID] = mapped_column("Id", Uuid, primary_key=True, default=uuid.uuid4)
    phone: Mapped[str] = mapped_column("Phone", String(20), nullable=False)
    otp_code: Mapped[str] = mapped_column("OtpCode", String(10), nullable=False)
    expires_at: Mapped[datetime] = mapped_column("ExpiresAt", DateTime(timezone=True), nullable=False)
    is_used: Mapped[bool] = mapped_column("IsUsed", Boolean, default=False)
    verify_attempts: Mapped[int] = mapped_column("VerifyAttempts", Integer, default=0)
    resend_count: Mapped[int] = mapped_column("ResendCount", Integer, default=0)
    last_resend_on: Mapped[datetime | None] = mapped_column("LastResendOn", DateTime(timezone=True), nullable=True)
    is_locked: Mapped[bool] = mapped_column("IsLocked", Boolean, default=False)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True), nullable=False)


class MobileDevice(Base):
    __tablename__ = "MobileDevices"

    id: Mapped[uuid.UUID] = mapped_column("Id", Uuid, primary_key=True, default=uuid.uuid4)
    user_type: Mapped[int] = mapped_column("UserType", Integer, nullable=False)
    user_id: Mapped[uuid.UUID] = mapped_column("UserId", Uuid, nullable=False)
    device_token: Mapped[str] = mapped_column("DeviceToken", String(500), nullable=False)
    platform: Mapped[str] = mapped_column("Platform", String(50), nullable=False)
    is_active: Mapped[bool] = mapped_column("IsActive", Boolean, default=True)
    created_at: Mapped[datetime] = mapped_column("CreatedAt", DateTime(timezone=True), nullable=False)
    updated_at: Mapped[datetime] = mapped_column("UpdatedAt", DateTime(timezone=True), nullable=False)
