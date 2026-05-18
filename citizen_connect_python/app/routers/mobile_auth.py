from datetime import datetime, timedelta, timezone
import random
import uuid
from fastapi import APIRouter, Depends, HTTPException, status
from jose import jwt
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.config import settings
from app.database import get_db
from app.models.identity import Citizen
from app.models.mobile import CitizenOtp, CitizenRefreshToken
from app.schemas.auth import AuthResponseSchema, UserInfoSchema
from app.schemas.mobile import (
    RefreshTokenRequestSchema,
    SendOtpRequestSchema,
    SendOtpResponseSchema,
    VerifyOtpRequestSchema,
)
from app.services.sms_service import SmsService, fixed_otp_users, normalize_mobile

router = APIRouter(prefix="/api/auth/citizen", tags=["Mobile Auth"])


@router.post("/refresh-token", response_model=AuthResponseSchema)
async def refresh_token(dto: RefreshTokenRequestSchema, db: AsyncSession = Depends(get_db)):
    result = await db.execute(
        select(CitizenRefreshToken).where(
            CitizenRefreshToken.token == dto.refresh_token,
            CitizenRefreshToken.is_revoked == False,
        )
    )
    refresh = result.scalar_one_or_none()
    now = datetime.now(timezone.utc)
    if not refresh or as_utc(refresh.expires_at) <= now:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Invalid or expired refresh token.")

    citizen = await db.get(Citizen, refresh.citizen_id)
    if not citizen or not citizen.is_active:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Citizen account is inactive or not found.")

    refresh.is_revoked = True
    response = build_citizen_auth_response(citizen, db)
    await db.flush()
    return response


@router.post("/register/send-otp", response_model=SendOtpResponseSchema)
@router.post("/request-otp", response_model=SendOtpResponseSchema)
async def send_otp(dto: SendOtpRequestSchema, db: AsyncSession = Depends(get_db)):
    try:
        normalized = normalize_mobile(dto.phone)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc))

    now = datetime.now(timezone.utc)
    fixed_user = next((x for x in fixed_otp_users() if x["mobile_number"] == normalized), None)
    if fixed_user:
        return await handle_fixed_otp_user(normalized, fixed_user["otp"], now, db)

    today = now.replace(hour=0, minute=0, second=0, microsecond=0)
    resend_count_result = await db.execute(
        select(CitizenOtp).where(
            CitizenOtp.phone == normalized,
            CitizenOtp.created_at >= today,
        )
    )
    if len(resend_count_result.scalars().all()) >= settings.otp_max_resends_per_day:
        return SendOtpResponseSchema(
            phone=normalized,
            mobile_number=normalized,
            is_new_otp=False,
            message="OTP resend limit exceeded for today",
        )

    last_result = await db.execute(
        select(CitizenOtp)
        .where(
            CitizenOtp.phone == normalized,
            CitizenOtp.is_used == False,
            CitizenOtp.is_locked == False,
        )
        .order_by(CitizenOtp.created_at.desc())
    )
    last_otp = last_result.scalar_one_or_none()
    if last_otp and as_utc(last_otp.expires_at) >= now:
        if last_otp.last_resend_on and (now - as_utc(last_otp.last_resend_on)).total_seconds() < settings.otp_resend_cooldown_seconds:
            return SendOtpResponseSchema(
                phone=normalized,
                mobile_number=normalized,
                expires_at=last_otp.expires_at,
                expires_on=last_otp.expires_at,
                dev_otp=last_otp.otp_code,
                otp=last_otp.otp_code,
                is_new_otp=False,
                message="OTP already sent. Please wait before requesting again.",
            )

        last_otp.last_resend_on = now
        last_otp.resend_count += 1
        await db.flush()
        await send_otp_sms(normalized, last_otp.otp_code)
        return SendOtpResponseSchema(
            phone=normalized,
            mobile_number=normalized,
            expires_at=last_otp.expires_at,
            expires_on=last_otp.expires_at,
            dev_otp=last_otp.otp_code,
            otp=last_otp.otp_code,
            is_new_otp=False,
            message="OTP resent successfully",
        )

    otp = f"{random.randint(100000, 999999)}"
    expires_at = now + timedelta(minutes=settings.otp_expiry_minutes)
    db.add(CitizenOtp(
        id=uuid.uuid4(),
        phone=normalized,
        otp_code=otp,
        expires_at=expires_at,
        is_used=False,
        is_locked=False,
        resend_count=1,
        last_resend_on=now,
        created_at=now,
    ))
    await db.flush()
    await send_otp_sms(normalized, otp)
    return SendOtpResponseSchema(
        phone=normalized,
        mobile_number=normalized,
        expires_at=expires_at,
        expires_on=expires_at,
        dev_otp=otp,
        otp=otp,
        is_new_otp=True,
        message="OTP sent successfully",
    )


@router.post("/register/verify-otp")
@router.post("/verify-otp")
async def verify_otp(dto: VerifyOtpRequestSchema, db: AsyncSession = Depends(get_db)):
    try:
        normalized = normalize_mobile(dto.phone)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc))

    result = await db.execute(
        select(CitizenOtp)
        .where(
            CitizenOtp.phone == normalized,
            CitizenOtp.is_used == False,
            CitizenOtp.is_locked == False,
            CitizenOtp.expires_at >= datetime.now(timezone.utc),
        )
        .order_by(CitizenOtp.created_at.desc())
    )
    otp = result.scalar_one_or_none()
    if not otp:
        raise HTTPException(status_code=400, detail="Invalid or expired OTP.")

    if otp.otp_code != dto.otp:
        otp.verify_attempts += 1
        if otp.verify_attempts >= settings.otp_max_verify_attempts:
            otp.is_locked = True
        await db.flush()
        raise HTTPException(status_code=400, detail="Invalid or expired OTP.")

    otp.is_used = True
    citizen_result = await db.execute(select(Citizen).where((Citizen.phone == normalized) | (Citizen.phone == dto.phone)))
    citizen = citizen_result.scalar_one_or_none()
    if citizen:
        citizen.is_verified = True
        citizen.phone = normalized
    await db.flush()
    return {"verified": True, "phone": normalized}


async def handle_fixed_otp_user(normalized: str, otp_code: str, now: datetime, db: AsyncSession) -> SendOtpResponseSchema:
    expires_at = now + timedelta(minutes=10)
    result = await db.execute(
        select(CitizenOtp)
        .where(
            CitizenOtp.phone == normalized,
            CitizenOtp.otp_code == otp_code,
            CitizenOtp.is_used == False,
            CitizenOtp.is_locked == False,
        )
        .order_by(CitizenOtp.created_at.desc())
    )
    existing = result.scalar_one_or_none()
    if existing and as_utc(existing.expires_at) >= now:
        if existing.last_resend_on and (now - as_utc(existing.last_resend_on)).total_seconds() < settings.otp_resend_cooldown_seconds:
            return SendOtpResponseSchema(
                phone=normalized,
                mobile_number=normalized,
                expires_at=existing.expires_at,
                expires_on=existing.expires_at,
                dev_otp=otp_code,
                otp=otp_code,
                is_new_otp=False,
                message="OTP already sent. Please wait before requesting again.",
            )
        existing.last_resend_on = now
        existing.resend_count += 1
        existing.expires_at = expires_at
        await db.flush()
        await send_otp_sms(normalized, otp_code)
        return SendOtpResponseSchema(
            phone=normalized,
            mobile_number=normalized,
            expires_at=existing.expires_at,
            expires_on=existing.expires_at,
            dev_otp=otp_code,
            otp=otp_code,
            is_new_otp=False,
            message="OTP resent successfully",
        )

    db.add(CitizenOtp(
        id=uuid.uuid4(),
        phone=normalized,
        otp_code=otp_code,
        expires_at=expires_at,
        is_used=False,
        is_locked=False,
        resend_count=0,
        last_resend_on=now,
        created_at=now,
    ))
    await db.flush()
    await send_otp_sms(normalized, otp_code)
    return SendOtpResponseSchema(
        phone=normalized,
        mobile_number=normalized,
        expires_at=expires_at,
        expires_on=expires_at,
        dev_otp=otp_code,
        otp=otp_code,
        is_new_otp=True,
        message="OTP sent successfully",
    )


async def send_otp_sms(normalized: str, otp_code: str) -> None:
    await SmsService().send(
        normalized,
        f"Your OTP to login to MLA Sampark is {otp_code}. Please enter this code to verify your identity. For security, do not share this code. https://mlasampark.com/",
    )


def build_citizen_auth_response(citizen: Citizen, db: AsyncSession) -> AuthResponseSchema:
    expiry = datetime.now(timezone.utc) + timedelta(hours=settings.jwt_expiry_hours)
    payload = {
        "sub": str(citizen.id),
        "name": citizen.full_name,
        "phone": citizen.phone,
        "user_type": "citizen",
        "block_id": citizen.block_id,
        "exp": expiry,
    }
    token = jwt.encode(payload, settings.jwt_secret_key, algorithm=settings.jwt_algorithm)
    refresh_token = str(uuid.uuid4()).replace("-", "") + str(uuid.uuid4()).replace("-", "")
    db.add(CitizenRefreshToken(
        id=uuid.uuid4(),
        citizen_id=citizen.id,
        token=refresh_token,
        expires_at=datetime.now(timezone.utc) + timedelta(days=30),
        is_revoked=False,
        created_at=datetime.now(timezone.utc),
    ))
    return AuthResponseSchema(
        token=token,
        refresh_token=refresh_token,
        expires_at=expiry.isoformat(),
        user=UserInfoSchema(
            id=str(citizen.id),
            full_name=citizen.full_name,
            phone=citizen.phone,
            email=citizen.email,
            user_type="citizen",
        )
    )


def as_utc(value: datetime) -> datetime:
    if value.tzinfo is None:
        return value.replace(tzinfo=timezone.utc)
    return value.astimezone(timezone.utc)
