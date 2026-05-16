from fastapi import APIRouter, Depends, HTTPException, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from jose import JWTError, jwt
from sqlalchemy.ext.asyncio import AsyncSession

from app.config import settings
from app.database import get_db
from app.schemas.report import DashboardSummarySchema
from app.services.reporting_service import ReportingService

router = APIRouter(prefix="/api/dashboard", tags=["Dashboard"])
security = HTTPBearer()


def decode_token(credentials: HTTPAuthorizationCredentials = Depends(security)) -> dict:
    try:
        return jwt.decode(
            credentials.credentials,
            settings.jwt_secret_key,
            algorithms=[settings.jwt_algorithm]
        )
    except JWTError:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Invalid or expired token")


def require_internal_user(payload: dict = Depends(decode_token)) -> dict:
    if payload.get("user_type") != "internal":
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="Internal users only")
    return payload


def get_reporting_service(db: AsyncSession = Depends(get_db)) -> ReportingService:
    return ReportingService(db)


@router.get("/summary", response_model=DashboardSummarySchema)
async def get_summary(
    payload: dict = Depends(require_internal_user),
    service: ReportingService = Depends(get_reporting_service)
):
    return await service.get_dashboard_summary()
