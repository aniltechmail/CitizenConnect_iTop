from datetime import datetime, timedelta, timezone
from jose import jwt
from passlib.context import CryptContext
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select

from app.config import settings
from app.models.identity import Citizen, InternalUser, UserRole
from app.repositories.citizen_repository import CitizenRepository
from app.schemas.auth import (
    CitizenRegisterSchema, LoginSchema, AuthResponseSchema, UserInfoSchema
)
import uuid

pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")


class AuthService:
    def __init__(self, db: AsyncSession):
        self.db = db
        self.citizen_repo = CitizenRepository(db)

    async def register_citizen(
        self, dto: CitizenRegisterSchema
    ) -> AuthResponseSchema:
        if await self.citizen_repo.phone_exists(dto.phone):
            raise ValueError("Phone number already registered.")

        citizen = Citizen(
            id=uuid.uuid4(),
            full_name=dto.full_name,
            phone=dto.phone,
            email=dto.email,
            password_hash=pwd_context.hash(dto.password),
            block_id=dto.block_id,
            is_verified=False,
            is_active=True,
            created_at=datetime.now(timezone.utc)
        )
        await self.citizen_repo.create(citizen)
        return self._generate_citizen_token(citizen)

    async def login_citizen(self, dto: LoginSchema) -> AuthResponseSchema:
        citizen = await self.citizen_repo.get_by_phone(dto.identifier)
        if not citizen or not pwd_context.verify(dto.password, citizen.password_hash):
            raise PermissionError("Invalid credentials.")
        if not citizen.is_active:
            raise PermissionError("Account is deactivated.")

        citizen.last_login_at = datetime.now(timezone.utc)
        await self.citizen_repo.update(citizen)
        return self._generate_citizen_token(citizen)

    async def login_internal_user(self, dto: LoginSchema) -> AuthResponseSchema:
        result = await self.db.execute(
            select(InternalUser).where(
                InternalUser.email == dto.identifier,
                InternalUser.is_active == True
            )
        )
        user = result.scalar_one_or_none()
        if not user or not pwd_context.verify(dto.password, user.password_hash):
            raise PermissionError("Invalid credentials.")

        user.last_login_at = datetime.now(timezone.utc)
        await self.db.flush()
        return self._generate_internal_token(user)

    def _generate_citizen_token(self, citizen: Citizen) -> AuthResponseSchema:
        expiry = datetime.now(timezone.utc) + timedelta(
            hours=settings.jwt_expiry_hours
        )
        payload = {
            "sub": str(citizen.id),
            "name": citizen.full_name,
            "phone": citizen.phone,
            "user_type": "citizen",
            "block_id": citizen.block_id,
            "exp": expiry
        }
        token = jwt.encode(
            payload, settings.jwt_secret_key, algorithm=settings.jwt_algorithm
        )
        return AuthResponseSchema(
            token=token,
            refresh_token=str(uuid.uuid4()),
            expires_at=expiry.isoformat(),
            user=UserInfoSchema(
                id=str(citizen.id),
                full_name=citizen.full_name,
                phone=citizen.phone,
                email=citizen.email,
                user_type="citizen"
            )
        )

    def _generate_internal_token(self, user: InternalUser) -> AuthResponseSchema:
        expiry = datetime.now(timezone.utc) + timedelta(
            hours=settings.jwt_expiry_hours
        )
        role_name = UserRole(user.role).name
        payload = {
            "sub": str(user.id),
            "name": user.full_name,
            "email": user.email,
            "user_type": "internal",
            "role": role_name,
            "exp": expiry
        }
        token = jwt.encode(
            payload, settings.jwt_secret_key, algorithm=settings.jwt_algorithm
        )
        return AuthResponseSchema(
            token=token,
            refresh_token=str(uuid.uuid4()),
            expires_at=expiry.isoformat(),
            user=UserInfoSchema(
                id=str(user.id),
                full_name=user.full_name,
                email=user.email,
                user_type="internal",
                role=role_name
            )
        )