from app.repositories.location_repository import LocationRepository
from app.schemas.location import (
    DistrictSchema, ConstituencySchema, AreaSchema, BlockSchema
)


class LocationService:
    def __init__(self, repo: LocationRepository):
        self.repo = repo

    async def get_all_districts(self) -> list[DistrictSchema]:
        districts = await self.repo.get_all_districts()
        return [DistrictSchema.model_validate(d) for d in districts]

    async def get_constituencies_by_district(
        self, district_id: int
    ) -> list[ConstituencySchema]:
        items = await self.repo.get_constituencies_by_district(district_id)
        return [ConstituencySchema.model_validate(i) for i in items]

    async def get_areas_by_constituency(
        self, constituency_id: int
    ) -> list[AreaSchema]:
        items = await self.repo.get_areas_by_constituency(constituency_id)
        return [AreaSchema.model_validate(i) for i in items]

    async def get_blocks_by_area(self, area_id: int) -> list[BlockSchema]:
        items = await self.repo.get_blocks_by_area(area_id)
        return [BlockSchema.model_validate(i) for i in items]