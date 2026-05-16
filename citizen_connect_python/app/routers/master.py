from fastapi import APIRouter, Depends
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from app.database import get_db
from app.models.master import Department, ComplaintCategory
from app.models.location import Block
from app.models.identity import InternalUser, UserRole

router = APIRouter(prefix="/api/master", tags=["Master"])


@router.get("/departments")
async def get_departments(db: AsyncSession = Depends(get_db)):
    result = await db.execute(
        select(Department).where(Department.is_active == True).order_by(Department.name)
    )
    return [
        {
            "id": x.id,
            "name": x.name,
            "code": x.code,
            "description": x.description,
            "is_active": x.is_active,
        }
        for x in result.scalars().all()
    ]


@router.get("/categories")
async def get_categories(db: AsyncSession = Depends(get_db)):
    result = await db.execute(
        select(ComplaintCategory, Department.name)
        .join(Department, ComplaintCategory.department_id == Department.id)
        .where(ComplaintCategory.is_active == True)
        .order_by(ComplaintCategory.name)
    )
    return [
        {
            "id": category.id,
            "name": category.name,
            "description": category.description,
            "department_id": category.department_id,
            "department_name": department_name,
            "is_active": category.is_active,
        }
        for category, department_name in result.all()
    ]


@router.get("/blocks")
async def get_blocks(db: AsyncSession = Depends(get_db)):
    result = await db.execute(
        select(Block).where(Block.is_active == True).order_by(Block.name)
    )
    return [
        {
            "id": x.id,
            "name": x.name,
            "code": x.code,
            "area_id": x.area_id,
            "is_active": x.is_active,
        }
        for x in result.scalars().all()
    ]


@router.get("/agents")
async def get_agents(db: AsyncSession = Depends(get_db)):
    result = await db.execute(
        select(InternalUser)
        .where(
            InternalUser.is_active == True,
            InternalUser.role == UserRole.FieldAgent.value,
        )
        .order_by(InternalUser.full_name)
    )
    return [
        {
            "id": str(x.id),
            "full_name": x.full_name,
            "email": x.email,
            "department_id": x.department_id,
        }
        for x in result.scalars().all()
    ]
