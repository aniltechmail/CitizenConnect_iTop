from fastapi import APIRouter, Depends
from sqlalchemy.ext.asyncio import AsyncSession
from app.database import get_db
from app.repositories.location_repository import LocationRepository
from app.services.location_service import LocationService
from app.schemas.location import (
    DistrictSchema, ConstituencySchema, AreaSchema, BlockSchema
)

router = APIRouter(prefix="/api/location", tags=["Location"])


def get_location_service(db: AsyncSession = Depends(get_db)) -> LocationService:
    return LocationService(LocationRepository(db))


@router.get("/districts", response_model=list[DistrictSchema])
async def get_districts(service: LocationService = Depends(get_location_service)):
    return await service.get_all_districts()


@router.get("/districts/{district_id}/constituencies",
            response_model=list[ConstituencySchema])
async def get_constituencies(
    district_id: int,
    service: LocationService = Depends(get_location_service)
):
    return await service.get_constituencies_by_district(district_id)


@router.get("/constituencies/{constituency_id}/areas",
            response_model=list[AreaSchema])
async def get_areas(
    constituency_id: int,
    service: LocationService = Depends(get_location_service)
):
    return await service.get_areas_by_constituency(constituency_id)


@router.get("/areas/{area_id}/blocks", response_model=list[BlockSchema])
async def get_blocks(
    area_id: int,
    service: LocationService = Depends(get_location_service)
):
    return await service.get_blocks_by_area(area_id)