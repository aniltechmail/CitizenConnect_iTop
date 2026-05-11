from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.ext.asyncio import AsyncSession
from app.database import get_db
from app.services.auth_service import AuthService
from app.schemas.auth import CitizenRegisterSchema, LoginSchema, AuthResponseSchema

router = APIRouter(prefix="/api/auth", tags=["Auth"])


def get_auth_service(db: AsyncSession = Depends(get_db)) -> AuthService:
    return AuthService(db)


@router.post("/citizen/register", response_model=AuthResponseSchema)
async def register_citizen(
    dto: CitizenRegisterSchema,
    service: AuthService = Depends(get_auth_service)
):
    try:
        return await service.register_citizen(dto)
    except ValueError as e:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail=str(e))


@router.post("/citizen/login", response_model=AuthResponseSchema)
async def login_citizen(
    dto: LoginSchema,
    service: AuthService = Depends(get_auth_service)
):
    try:
        return await service.login_citizen(dto)
    except PermissionError as e:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail=str(e))


@router.post("/internal/login", response_model=AuthResponseSchema)
async def login_internal(
    dto: LoginSchema,
    service: AuthService = Depends(get_auth_service)
):
    try:
        return await service.login_internal_user(dto)
    except PermissionError as e:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail=str(e))