from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from app.models.identity import InternalUser
import uuid


class InternalUserRepository:
    def __init__(self, db: AsyncSession):
        self.db = db

    async def get_by_id(self, user_id: uuid.UUID) -> InternalUser | None:
        result = await self.db.execute(
            select(InternalUser)
            .where(InternalUser.id == user_id, InternalUser.is_active == True)
        )
        return result.scalar_one_or_none()

    async def get_by_email(self, email: str) -> InternalUser | None:
        result = await self.db.execute(
            select(InternalUser)
            .where(InternalUser.email == email, InternalUser.is_active == True)
        )
        return result.scalar_one_or_none()

    async def get_by_role(self, role: int) -> list[InternalUser]:
        result = await self.db.execute(
            select(InternalUser)
            .where(InternalUser.role == role, InternalUser.is_active == True)
        )
        return list(result.scalars().all())

    async def update(self, user: InternalUser) -> InternalUser:
        await self.db.flush()
        await self.db.refresh(user)
        return user