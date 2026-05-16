from fastapi import APIRouter, Depends, HTTPException, status
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from jose import jwt, JWTError
from sqlalchemy.ext.asyncio import AsyncSession

from app.config import settings
from app.database import get_db
from app.schemas.escalation import EscalationEventResponseSchema
from app.services.escalation_service import EscalationService

router = APIRouter(prefix="/api/escalations", tags=["Escalations"])
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


def require_admin_or_supervisor(payload: dict = Depends(decode_token)) -> dict:
    if payload.get("user_type") != "internal" or payload.get("role") not in ["Admin", "Supervisor"]:
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Admin or Supervisor role required"
        )
    return payload


def get_escalation_service(db: AsyncSession = Depends(get_db)) -> EscalationService:
    return EscalationService(db)


@router.get("", response_model=list[EscalationEventResponseSchema])
async def get_active_escalations(
    payload: dict = Depends(require_admin_or_supervisor),
    service: EscalationService = Depends(get_escalation_service)
):
    return await service.get_active()


@router.post("/scan")
async def scan_escalations(
    payload: dict = Depends(require_admin_or_supervisor),
    service: EscalationService = Depends(get_escalation_service)
):
    created = await service.scan()
    return {"created": created}
