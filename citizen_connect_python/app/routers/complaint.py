from fastapi import APIRouter, Depends, HTTPException, status, UploadFile, File, Query
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from sqlalchemy.ext.asyncio import AsyncSession
from jose import jwt, JWTError
from typing import Optional
import uuid

from app.database import get_db
from app.config import settings
from app.services.complaint_service import ComplaintService
from app.schemas.complaint import (
    SubmitComplaintSchema, AssignDepartmentSchema,
    UpdateStatusSchema, ComplaintResponseSchema,
    ComplaintMediaResponseSchema, PagedComplaintsSchema
)

router = APIRouter(prefix="/api/complaint", tags=["Complaint"])
security = HTTPBearer()


# ── Auth Helpers ───────────────────────────────────────────────────────────

def decode_token(credentials: HTTPAuthorizationCredentials = Depends(security)) -> dict:
    try:
        payload = jwt.decode(
            credentials.credentials,
            settings.jwt_secret_key,
            algorithms=[settings.jwt_algorithm]
        )
        return payload
    except JWTError:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid or expired token"
        )


def get_current_user_id(payload: dict = Depends(decode_token)) -> uuid.UUID:
    user_id = payload.get("sub")
    if not user_id:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid token payload"
        )
    return uuid.UUID(user_id)


def require_internal_user(payload: dict = Depends(decode_token)) -> dict:
    if payload.get("user_type") != "internal":
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Internal users only"
        )
    return payload


def require_citizen(payload: dict = Depends(decode_token)) -> dict:
    if payload.get("user_type") != "citizen":
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Citizen users only"
        )
    return payload


def require_roles(allowed_roles: list[str]):
    def checker(payload: dict = Depends(require_internal_user)) -> dict:
        role = payload.get("role")
        if role not in allowed_roles:
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail=f"Required roles: {', '.join(allowed_roles)}"
            )
        return payload
    return checker


def ensure_can_access_complaint(complaint: ComplaintResponseSchema, payload: dict) -> None:
    if payload.get("user_type") == "internal":
        return
    if str(complaint.citizen_id) != str(payload.get("sub")):
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="You do not have access to this complaint"
        )


def get_complaint_service(db: AsyncSession = Depends(get_db)) -> ComplaintService:
    return ComplaintService(db)


# ── Citizen Endpoints ──────────────────────────────────────────────────────

@router.post("", response_model=ComplaintResponseSchema, status_code=201)
async def submit_complaint(
    dto: SubmitComplaintSchema,
    payload: dict = Depends(require_citizen),
    service: ComplaintService = Depends(get_complaint_service)
):
    try:
        citizen_id = uuid.UUID(payload["sub"])
        return await service.submit_complaint(citizen_id, dto)
    except KeyError as e:
        raise HTTPException(status_code=404, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.get("/my", response_model=list[ComplaintResponseSchema])
async def get_my_complaints(
    payload: dict = Depends(require_citizen),
    service: ComplaintService = Depends(get_complaint_service)
):
    citizen_id = uuid.UUID(payload["sub"])
    return await service.get_my_complaints(citizen_id)


@router.get("/{complaint_id}", response_model=ComplaintResponseSchema)
async def get_by_id(
    complaint_id: uuid.UUID,
    payload: dict = Depends(decode_token),
    service: ComplaintService = Depends(get_complaint_service)
):
    try:
        complaint = await service.get_by_id(complaint_id)
        ensure_can_access_complaint(complaint, payload)
        return complaint
    except KeyError as e:
        raise HTTPException(status_code=404, detail=str(e))


@router.post("/{complaint_id}/media", response_model=ComplaintMediaResponseSchema)
async def upload_media(
    complaint_id: uuid.UUID,
    file: UploadFile = File(...),
    payload: dict = Depends(decode_token),
    service: ComplaintService = Depends(get_complaint_service)
):
    try:
        complaint = await service.get_by_id(complaint_id)
        ensure_can_access_complaint(complaint, payload)
        user_id = uuid.UUID(payload["sub"])
        return await service.upload_media(complaint_id, file, user_id)
    except KeyError as e:
        raise HTTPException(status_code=404, detail=str(e))
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))


# ── Internal User Endpoints ────────────────────────────────────────────────

@router.get("", response_model=PagedComplaintsSchema)
async def get_all_complaints(
    status: Optional[int] = Query(None),
    department_id: Optional[int] = Query(None),
    block_id: Optional[int] = Query(None),
    page: int = Query(1, ge=1),
    page_size: int = Query(20, ge=1, le=100),
    payload: dict = Depends(require_internal_user),
    service: ComplaintService = Depends(get_complaint_service)
):
    return await service.get_all_complaints(
        status, department_id, block_id, page, page_size
    )


@router.put("/{complaint_id}/assign-department",
            response_model=ComplaintResponseSchema)
async def assign_department(
    complaint_id: uuid.UUID,
    dto: AssignDepartmentSchema,
    payload: dict = Depends(require_roles(["Admin", "Assigner"])),
    service: ComplaintService = Depends(get_complaint_service)
):
    try:
        user_id = uuid.UUID(payload["sub"])
        return await service.assign_department(complaint_id, dto, user_id)
    except KeyError as e:
        raise HTTPException(status_code=404, detail=str(e))
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))


@router.put("/{complaint_id}/assign-agent/{agent_id}",
            response_model=ComplaintResponseSchema)
async def assign_agent(
    complaint_id: uuid.UUID,
    agent_id: uuid.UUID,
    payload: dict = Depends(require_roles(["Admin", "Assigner"])),
    service: ComplaintService = Depends(get_complaint_service)
):
    try:
        user_id = uuid.UUID(payload["sub"])
        return await service.assign_agent(complaint_id, agent_id, user_id)
    except KeyError as e:
        raise HTTPException(status_code=404, detail=str(e))


@router.put("/{complaint_id}/status", response_model=ComplaintResponseSchema)
async def update_status(
    complaint_id: uuid.UUID,
    dto: UpdateStatusSchema,
    payload: dict = Depends(require_roles(["Admin", "Assigner", "FieldAgent", "Supervisor"])),
    service: ComplaintService = Depends(get_complaint_service)
):
    try:
        user_id = uuid.UUID(payload["sub"])
        return await service.update_status(complaint_id, dto, user_id)
    except KeyError as e:
        raise HTTPException(status_code=404, detail=str(e))
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e))
