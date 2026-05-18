from datetime import datetime, timezone
from fastapi import APIRouter, Depends
from sqlalchemy import select
from sqlalchemy.orm import selectinload
from sqlalchemy.ext.asyncio import AsyncSession

from app.database import get_db
from app.models.location import District, Constituency, Area

router = APIRouter(prefix="/api/sync", tags=["Sync"])


@router.get("/location")
async def sync_location(db: AsyncSession = Depends(get_db)):
    result = await db.execute(
        select(District)
        .options(
            selectinload(District.constituencies)
            .selectinload(Constituency.areas)
            .selectinload(Area.blocks)
        )
        .where(District.is_active == True)
        .order_by(District.name)
    )
    districts = []
    for district in result.scalars().unique().all():
        districts.append({
            "id": district.id,
            "name": district.name,
            "code": district.code,
            "constituencies": [
                {
                    "id": constituency.id,
                    "name": constituency.name,
                    "code": constituency.code,
                    "areas": [
                        {
                            "id": area.id,
                            "name": area.name,
                            "code": area.code,
                            "blocks": [
                                {"id": block.id, "name": block.name, "code": block.code}
                                for block in sorted(area.blocks, key=lambda x: x.name)
                                if block.is_active
                            ],
                        }
                        for area in sorted(constituency.areas, key=lambda x: x.name)
                        if area.is_active
                    ],
                }
                for constituency in sorted(district.constituencies, key=lambda x: x.name)
                if constituency.is_active
            ],
        })
    return {"synced_at": datetime.now(timezone.utc), "districts": districts}
