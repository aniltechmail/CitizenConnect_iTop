from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from app.models.identity import Citizen
import uuid


class CitizenRepository:
    def __init__(self, db: AsyncSession):
        self.db = db

    async def get_by_phone(self, phone: str) -> Citizen | None:
        result = await self.db.execute(
            select(Citizen).where(Citizen.phone == phone)
        )
        return result.scalar_one_or_none()

    async def get_by_id(self, citizen_id: uuid.UUID) -> Citizen | None:
        result = await self.db.execute(
            select(Citizen).where(Citizen.id == citizen_id)
        )
        return result.scalar_one_or_none()

    async def phone_exists(self, phone: str) -> bool:
        result = await self.db.execute(
            select(Citizen.id).where(Citizen.phone == phone)
        )
        return result.scalar_one_or_none() is not None

    async def create(self, citizen: Citizen) -> Citizen:
        self.db.add(citizen)
        await self.db.flush()
        await self.db.refresh(citizen)
        return citizen

    async def update(self, citizen: Citizen) -> Citizen:
        await self.db.flush()
        await self.db.refresh(citizen)
        return citizen