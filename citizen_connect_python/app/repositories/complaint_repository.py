from datetime import datetime, timezone
from sqlalchemy import select, func, and_
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.orm import selectinload
from app.models.complaint import (
    Complaint, ComplaintMedia, ComplaintMessage,
    ComplaintFeedback, ComplaintITopMapping
)
import uuid


class ComplaintRepository:
    def __init__(self, db: AsyncSession):
        self.db = db

    async def get_by_id(self, complaint_id: uuid.UUID) -> Complaint | None:
        result = await self.db.execute(
            select(Complaint)
            .options(
                selectinload(Complaint.citizen),
                selectinload(Complaint.block),
                selectinload(Complaint.category),
                selectinload(Complaint.assigned_department),
                selectinload(Complaint.assigned_agent),
                selectinload(Complaint.media),
                selectinload(Complaint.messages)
            )
            .where(Complaint.id == complaint_id)
        )
        return result.scalar_one_or_none()

    async def get_by_ref_number(self, ref_number: str) -> Complaint | None:
        result = await self.db.execute(
            select(Complaint)
            .options(
                selectinload(Complaint.citizen),
                selectinload(Complaint.category)
            )
            .where(Complaint.ref_number == ref_number)
        )
        return result.scalar_one_or_none()

    async def get_by_citizen_id(self, citizen_id: uuid.UUID) -> list[Complaint]:
        result = await self.db.execute(
            select(Complaint)
            .options(
                selectinload(Complaint.citizen),
                selectinload(Complaint.block),
                selectinload(Complaint.category),
                selectinload(Complaint.assigned_department),
                selectinload(Complaint.assigned_agent),
                selectinload(Complaint.media)
            )
            .where(Complaint.citizen_id == citizen_id)
            .order_by(Complaint.created_at.desc())
        )
        return list(result.scalars().all())

    async def get_all(
        self,
        status: int | None,
        department_id: int | None,
        block_id: int | None,
        page: int,
        page_size: int
    ) -> list[Complaint]:
        query = self._build_filter_query(status, department_id, block_id)
        query = query.options(
            selectinload(Complaint.citizen),
            selectinload(Complaint.category),
            selectinload(Complaint.assigned_department),
            selectinload(Complaint.assigned_agent),
            selectinload(Complaint.block),
            selectinload(Complaint.media)
        ).order_by(Complaint.created_at.desc())
        query = query.offset((page - 1) * page_size).limit(page_size)
        result = await self.db.execute(query)
        return list(result.scalars().all())

    async def get_total_count(
        self,
        status: int | None,
        department_id: int | None,
        block_id: int | None
    ) -> int:
        query = self._build_filter_query(status, department_id, block_id)
        count_query = select(func.count()).select_from(query.subquery())
        result = await self.db.execute(count_query)
        return result.scalar_one()

    async def create(self, complaint: Complaint) -> Complaint:
        self.db.add(complaint)
        await self.db.flush()
        await self.db.refresh(complaint)
        return complaint

    async def update(self, complaint: Complaint) -> Complaint:
        await self.db.flush()
        await self.db.refresh(complaint)
        return complaint

    async def add_media(self, media: ComplaintMedia) -> ComplaintMedia:
        self.db.add(media)
        await self.db.flush()
        await self.db.refresh(media)
        return media

    async def add_message(self, message: ComplaintMessage) -> ComplaintMessage:
        self.db.add(message)
        await self.db.flush()
        await self.db.refresh(message)
        return message

    async def add_itop_mapping(
        self, mapping: ComplaintITopMapping
    ) -> ComplaintITopMapping:
        self.db.add(mapping)
        await self.db.flush()
        await self.db.refresh(mapping)
        return mapping

    async def generate_ref_number(self) -> str:
        today = datetime.now(timezone.utc)
        prefix = f"CC{today.strftime('%Y%m%d')}"
        result = await self.db.execute(
            select(func.count(Complaint.id))
            .where(Complaint.ref_number.like(f"{prefix}%"))
        )
        count = result.scalar_one()
        return f"{prefix}{(count + 1):04d}"

    def _build_filter_query(
        self,
        status: int | None,
        department_id: int | None,
        block_id: int | None
    ):
        query = select(Complaint)
        conditions = []
        if status is not None:
            conditions.append(Complaint.status == status)
        if department_id is not None:
            conditions.append(Complaint.assigned_department_id == department_id)
        if block_id is not None:
            conditions.append(Complaint.block_id == block_id)
        if conditions:
            query = query.where(and_(*conditions))
        return query
