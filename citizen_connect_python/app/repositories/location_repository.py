from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from app.models.location import District, Constituency, Area, Block


class LocationRepository:
    def __init__(self, db: AsyncSession):
        self.db = db

    async def get_all_districts(self) -> list[District]:
        result = await self.db.execute(
            select(District)
            .where(District.is_active == True)
            .order_by(District.name)
        )
        return list(result.scalars().all())

    async def get_constituencies_by_district(
        self, district_id: int
    ) -> list[Constituency]:
        result = await self.db.execute(
            select(Constituency)
            .where(
                Constituency.district_id == district_id,
                Constituency.is_active == True
            )
            .order_by(Constituency.name)
        )
        return list(result.scalars().all())

    async def get_areas_by_constituency(
        self, constituency_id: int
    ) -> list[Area]:
        result = await self.db.execute(
            select(Area)
            .where(
                Area.constituency_id == constituency_id,
                Area.is_active == True
            )
            .order_by(Area.name)
        )
        return list(result.scalars().all())

    async def get_blocks_by_area(self, area_id: int) -> list[Block]:
        result = await self.db.execute(
            select(Block)
            .where(
                Block.area_id == area_id,
                Block.is_active == True
            )
            .order_by(Block.name)
        )
        return list(result.scalars().all())

    async def get_block_by_id(self, block_id: int) -> Block | None:
        result = await self.db.execute(
            select(Block).where(Block.id == block_id, Block.is_active == True)
        )
        return result.scalar_one_or_none()