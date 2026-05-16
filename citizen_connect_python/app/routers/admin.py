from datetime import datetime, timezone
from typing import Optional
from uuid import UUID, uuid4
from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel, EmailStr
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.database import get_db
from app.models.identity import InternalUser, UserRole
from app.models.master import Department, ComplaintCategory, SlaPolicy
from app.models.location import District, Constituency, Area, Block
from app.routers.complaint import require_roles
from app.services.auth_service import pwd_context

router = APIRouter(
    prefix="/api/admin",
    tags=["Admin"],
    dependencies=[Depends(require_roles(["Admin"]))],
)


class InternalUserRequest(BaseModel):
    full_name: str
    email: EmailStr
    password: Optional[str] = None
    role: int
    department_id: Optional[int] = None
    is_active: bool = True


class DepartmentRequest(BaseModel):
    name: str
    code: str
    description: Optional[str] = None
    is_active: bool = True


class CategoryRequest(BaseModel):
    name: str
    description: Optional[str] = None
    department_id: int
    is_active: bool = True


class SlaPolicyRequest(BaseModel):
    category_id: int
    response_hours: int
    resolution_hours: int
    escalation_level1_hours: int
    escalation_level2_hours: int
    escalation_level3_hours: int
    is_active: bool = True


class LocationRequest(BaseModel):
    name: str
    code: str
    is_active: bool = True


class ConstituencyRequest(LocationRequest):
    district_id: int


class AreaRequest(LocationRequest):
    constituency_id: int


class BlockRequest(LocationRequest):
    area_id: int


@router.get("/users")
async def get_users(db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(InternalUser).order_by(InternalUser.full_name))
    return [
        {
            "id": str(x.id),
            "full_name": x.full_name,
            "email": x.email,
            "role": UserRole(x.role).name,
            "department_id": x.department_id,
            "is_active": x.is_active,
        }
        for x in result.scalars().all()
    ]


@router.post("/users")
async def create_user(request: InternalUserRequest, db: AsyncSession = Depends(get_db)):
    existing = await db.execute(select(InternalUser).where(InternalUser.email == request.email))
    if existing.scalar_one_or_none():
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Email already exists.")
    user = InternalUser(
        id=uuid4(),
        full_name=request.full_name,
        email=str(request.email),
        password_hash=pwd_context.hash(request.password or "ChangeMe123"),
        role=request.role,
        department_id=request.department_id,
        is_active=request.is_active,
        created_at=datetime.now(timezone.utc),
    )
    db.add(user)
    await db.flush()
    return {"id": str(user.id), "full_name": user.full_name, "email": user.email}


@router.put("/users/{user_id}")
async def update_user(user_id: UUID, request: InternalUserRequest, db: AsyncSession = Depends(get_db)):
    user = await db.get(InternalUser, user_id)
    if not user:
        raise HTTPException(status_code=404)
    user.full_name = request.full_name
    user.email = str(request.email)
    user.role = request.role
    user.department_id = request.department_id
    user.is_active = request.is_active
    if request.password:
        user.password_hash = pwd_context.hash(request.password)
    await db.flush()
    return {"ok": True}


@router.delete("/users/{user_id}", status_code=204)
async def delete_user(user_id: UUID, db: AsyncSession = Depends(get_db)):
    user = await db.get(InternalUser, user_id)
    if not user:
        raise HTTPException(status_code=404)
    user.is_active = False
    await db.flush()


@router.get("/departments")
async def get_departments(db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(Department).order_by(Department.name))
    return result.scalars().all()


@router.post("/departments")
async def create_department(request: DepartmentRequest, db: AsyncSession = Depends(get_db)):
    item = Department(**request.model_dump(), created_at=datetime.now(timezone.utc))
    db.add(item)
    await db.flush()
    return item


@router.put("/departments/{item_id}")
async def update_department(item_id: int, request: DepartmentRequest, db: AsyncSession = Depends(get_db)):
    item = await db.get(Department, item_id)
    if not item:
        raise HTTPException(status_code=404)
    for key, value in request.model_dump().items():
        setattr(item, key, value)
    await db.flush()
    return item


@router.delete("/departments/{item_id}", status_code=204)
async def delete_department(item_id: int, db: AsyncSession = Depends(get_db)):
    item = await db.get(Department, item_id)
    if not item:
        raise HTTPException(status_code=404)
    item.is_active = False
    await db.flush()


@router.get("/categories")
async def get_categories(db: AsyncSession = Depends(get_db)):
    result = await db.execute(
        select(ComplaintCategory, Department.name)
        .join(Department, ComplaintCategory.department_id == Department.id)
        .order_by(ComplaintCategory.name)
    )
    return [
        {
            "id": x.id,
            "name": x.name,
            "description": x.description,
            "department_id": x.department_id,
            "department_name": department_name,
            "is_active": x.is_active,
        }
        for x, department_name in result.all()
    ]


@router.post("/categories")
async def create_category(request: CategoryRequest, db: AsyncSession = Depends(get_db)):
    item = ComplaintCategory(**request.model_dump(), created_at=datetime.now(timezone.utc))
    db.add(item)
    await db.flush()
    return item


@router.put("/categories/{item_id}")
async def update_category(item_id: int, request: CategoryRequest, db: AsyncSession = Depends(get_db)):
    item = await db.get(ComplaintCategory, item_id)
    if not item:
        raise HTTPException(status_code=404)
    for key, value in request.model_dump().items():
        setattr(item, key, value)
    await db.flush()
    return item


@router.delete("/categories/{item_id}", status_code=204)
async def delete_category(item_id: int, db: AsyncSession = Depends(get_db)):
    item = await db.get(ComplaintCategory, item_id)
    if not item:
        raise HTTPException(status_code=404)
    item.is_active = False
    await db.flush()


@router.get("/sla-policies")
async def get_sla_policies(db: AsyncSession = Depends(get_db)):
    result = await db.execute(
        select(SlaPolicy, ComplaintCategory.name)
        .join(ComplaintCategory, SlaPolicy.category_id == ComplaintCategory.id)
        .order_by(ComplaintCategory.name)
    )
    return [
        {
            "id": x.id,
            "category_id": x.category_id,
            "category_name": category_name,
            "response_hours": x.response_hours,
            "resolution_hours": x.resolution_hours,
            "escalation_level1_hours": x.escalation_level1_hours,
            "escalation_level2_hours": x.escalation_level2_hours,
            "escalation_level3_hours": x.escalation_level3_hours,
            "is_active": x.is_active,
        }
        for x, category_name in result.all()
    ]


@router.post("/sla-policies")
async def create_sla_policy(request: SlaPolicyRequest, db: AsyncSession = Depends(get_db)):
    item = SlaPolicy(**request.model_dump())
    db.add(item)
    await db.flush()
    return item


@router.put("/sla-policies/{item_id}")
async def update_sla_policy(item_id: int, request: SlaPolicyRequest, db: AsyncSession = Depends(get_db)):
    item = await db.get(SlaPolicy, item_id)
    if not item:
        raise HTTPException(status_code=404)
    for key, value in request.model_dump().items():
        setattr(item, key, value)
    await db.flush()
    return item


@router.delete("/sla-policies/{item_id}", status_code=204)
async def delete_sla_policy(item_id: int, db: AsyncSession = Depends(get_db)):
    item = await db.get(SlaPolicy, item_id)
    if not item:
        raise HTTPException(status_code=404)
    item.is_active = False
    await db.flush()


@router.get("/locations/districts")
async def get_districts(db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(District).order_by(District.name))
    return result.scalars().all()


@router.post("/locations/districts")
async def create_district(request: LocationRequest, db: AsyncSession = Depends(get_db)):
    item = District(**request.model_dump(), created_at=datetime.now(timezone.utc))
    db.add(item)
    await db.flush()
    return item


@router.put("/locations/districts/{item_id}")
async def update_district(item_id: int, request: LocationRequest, db: AsyncSession = Depends(get_db)):
    item = await db.get(District, item_id)
    if not item:
        raise HTTPException(status_code=404)
    for key, value in request.model_dump().items():
        setattr(item, key, value)
    await db.flush()
    return item


@router.delete("/locations/districts/{item_id}", status_code=204)
async def delete_district(item_id: int, db: AsyncSession = Depends(get_db)):
    item = await db.get(District, item_id)
    if not item:
        raise HTTPException(status_code=404)
    item.is_active = False
    await db.flush()


@router.get("/locations/constituencies")
async def get_constituencies(db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(Constituency).order_by(Constituency.name))
    return result.scalars().all()


@router.post("/locations/constituencies")
async def create_constituency(request: ConstituencyRequest, db: AsyncSession = Depends(get_db)):
    item = Constituency(**request.model_dump(), created_at=datetime.now(timezone.utc))
    db.add(item)
    await db.flush()
    return item


@router.put("/locations/constituencies/{item_id}")
async def update_constituency(item_id: int, request: ConstituencyRequest, db: AsyncSession = Depends(get_db)):
    item = await db.get(Constituency, item_id)
    if not item:
        raise HTTPException(status_code=404)
    for key, value in request.model_dump().items():
        setattr(item, key, value)
    await db.flush()
    return item


@router.delete("/locations/constituencies/{item_id}", status_code=204)
async def delete_constituency(item_id: int, db: AsyncSession = Depends(get_db)):
    item = await db.get(Constituency, item_id)
    if not item:
        raise HTTPException(status_code=404)
    item.is_active = False
    await db.flush()


@router.get("/locations/areas")
async def get_areas(db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(Area).order_by(Area.name))
    return result.scalars().all()


@router.post("/locations/areas")
async def create_area(request: AreaRequest, db: AsyncSession = Depends(get_db)):
    item = Area(**request.model_dump(), created_at=datetime.now(timezone.utc))
    db.add(item)
    await db.flush()
    return item


@router.put("/locations/areas/{item_id}")
async def update_area(item_id: int, request: AreaRequest, db: AsyncSession = Depends(get_db)):
    item = await db.get(Area, item_id)
    if not item:
        raise HTTPException(status_code=404)
    for key, value in request.model_dump().items():
        setattr(item, key, value)
    await db.flush()
    return item


@router.delete("/locations/areas/{item_id}", status_code=204)
async def delete_area(item_id: int, db: AsyncSession = Depends(get_db)):
    item = await db.get(Area, item_id)
    if not item:
        raise HTTPException(status_code=404)
    item.is_active = False
    await db.flush()


@router.get("/locations/blocks")
async def get_blocks(db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(Block).order_by(Block.name))
    return result.scalars().all()


@router.post("/locations/blocks")
async def create_block(request: BlockRequest, db: AsyncSession = Depends(get_db)):
    item = Block(**request.model_dump(), created_at=datetime.now(timezone.utc))
    db.add(item)
    await db.flush()
    return item


@router.put("/locations/blocks/{item_id}")
async def update_block(item_id: int, request: BlockRequest, db: AsyncSession = Depends(get_db)):
    item = await db.get(Block, item_id)
    if not item:
        raise HTTPException(status_code=404)
    for key, value in request.model_dump().items():
        setattr(item, key, value)
    await db.flush()
    return item


@router.delete("/locations/blocks/{item_id}", status_code=204)
async def delete_block(item_id: int, db: AsyncSession = Depends(get_db)):
    item = await db.get(Block, item_id)
    if not item:
        raise HTTPException(status_code=404)
    item.is_active = False
    await db.flush()
