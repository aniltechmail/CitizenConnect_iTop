from fastapi import APIRouter, Depends, HTTPException, status
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from jose import jwt, JWTError
from sqlalchemy.ext.asyncio import AsyncSession
import uuid

from app.config import settings
from app.database import get_db
from app.schemas.complaint import SenderTypeEnum
from app.schemas.notification import NotificationResponseSchema
from app.services.notification_service import NotificationService

router = APIRouter(prefix="/api/notifications", tags=["Notifications"])
security = HTTPBearer()


def decode_token(credentials: HTTPAuthorizationCredentials = Depends(security)) -> dict:
    try:
        return jwt.decode(
            credentials.credentials,
            settings.jwt_secret_key,
            algorithms=[settings.jwt_algorithm]
        )
    except JWTError:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid or expired token"
        )


def get_notification_user_type(payload: dict) -> int:
    return SenderTypeEnum.Citizen if payload.get("user_type") == "citizen" else SenderTypeEnum.Agent


def get_notification_service(db: AsyncSession = Depends(get_db)) -> NotificationService:
    return NotificationService(db)


@router.get("/my", response_model=list[NotificationResponseSchema])
async def get_my_notifications(
    payload: dict = Depends(decode_token),
    service: NotificationService = Depends(get_notification_service)
):
    return await service.get_my_notifications(
        get_notification_user_type(payload),
        uuid.UUID(payload["sub"])
    )


@router.put("/{notification_id}/read", response_model=NotificationResponseSchema)
async def mark_notification_as_read(
    notification_id: uuid.UUID,
    payload: dict = Depends(decode_token),
    service: NotificationService = Depends(get_notification_service)
):
    try:
        return await service.mark_as_read(
            notification_id,
            get_notification_user_type(payload),
            uuid.UUID(payload["sub"])
        )
    except KeyError as e:
        raise HTTPException(status_code=404, detail=str(e))
    except PermissionError as e:
        raise HTTPException(status_code=403, detail=str(e))
