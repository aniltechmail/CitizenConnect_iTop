from datetime import datetime
from fastapi import APIRouter, Depends, HTTPException, Query, Response, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from jose import JWTError, jwt
from sqlalchemy.ext.asyncio import AsyncSession

from app.config import settings
from app.database import get_db
from app.schemas.report import (
    AgentPerformanceSchema,
    DepartmentPerformanceSchema,
    LocationReportSchema,
    PagedComplaintReportSchema,
)
from app.services.reporting_service import ReportingService

router = APIRouter(prefix="/api/reports", tags=["Reports"])
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


@router.get("/complaints", response_model=PagedComplaintReportSchema)
async def get_complaints(
    from_date: datetime | None = Query(None),
    to_date: datetime | None = Query(None),
    status: int | None = Query(None),
    department_id: int | None = Query(None),
    district_id: int | None = Query(None),
    constituency_id: int | None = Query(None),
    area_id: int | None = Query(None),
    block_id: int | None = Query(None),
    page: int = Query(1, ge=1),
    page_size: int = Query(20, ge=1, le=500),
    payload: dict = Depends(require_internal_user),
    service: ReportingService = Depends(get_reporting_service)
):
    return await service.get_complaint_report(
        from_date, to_date, status, department_id, district_id,
        constituency_id, area_id, block_id, page, page_size
    )


@router.get("/complaints/export")
async def export_complaints(
    from_date: datetime | None = Query(None),
    to_date: datetime | None = Query(None),
    status: int | None = Query(None),
    department_id: int | None = Query(None),
    district_id: int | None = Query(None),
    constituency_id: int | None = Query(None),
    area_id: int | None = Query(None),
    block_id: int | None = Query(None),
    payload: dict = Depends(require_internal_user),
    service: ReportingService = Depends(get_reporting_service)
):
    csv_data = await service.export_complaint_report_csv(
        from_date, to_date, status, department_id, district_id,
        constituency_id, area_id, block_id
    )
    return Response(
        content=csv_data,
        media_type="text/csv",
        headers={"Content-Disposition": "attachment; filename=complaints-report.csv"}
    )


@router.get("/departments", response_model=list[DepartmentPerformanceSchema])
async def get_departments(
    payload: dict = Depends(require_internal_user),
    service: ReportingService = Depends(get_reporting_service)
):
    return await service.get_department_performance()


@router.get("/agents", response_model=list[AgentPerformanceSchema])
async def get_agents(
    payload: dict = Depends(require_internal_user),
    service: ReportingService = Depends(get_reporting_service)
):
    return await service.get_agent_performance()


@router.get("/locations", response_model=list[LocationReportSchema])
async def get_locations(
    payload: dict = Depends(require_internal_user),
    service: ReportingService = Depends(get_reporting_service)
):
    return await service.get_location_report()
