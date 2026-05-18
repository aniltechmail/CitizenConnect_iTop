from datetime import datetime, timezone
import uuid
from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.database import get_db
from app.models.mobile import MobileDevice
from app.routers.complaint import decode_token
from app.schemas.mobile import RegisterDeviceRequestSchema

router = APIRouter(prefix="/api/devices", tags=["Devices"])


@router.post("/register")
async def register_device(
    dto: RegisterDeviceRequestSchema,
    payload: dict = Depends(decode_token),
    db: AsyncSession = Depends(get_db),
):
    if not dto.device_token.strip():
        raise HTTPException(status_code=400, detail="Device token is required.")

    user_id = uuid.UUID(payload["sub"])
    user_type = 0 if payload.get("user_type") == "citizen" else 1
    result = await db.execute(
        select(MobileDevice).where(
            MobileDevice.user_id == user_id,
            MobileDevice.device_token == dto.device_token,
        )
    )
    device = result.scalar_one_or_none()
    if not device:
        device = MobileDevice(
            id=uuid.uuid4(),
            user_id=user_id,
            user_type=user_type,
            device_token=dto.device_token,
            created_at=datetime.now(timezone.utc),
        )
        db.add(device)
    device.platform = dto.platform
    device.is_active = True
    device.updated_at = datetime.now(timezone.utc)
    await db.flush()
    return {"registered": True, "id": str(device.id)}
