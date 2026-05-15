from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.orm import selectinload
from app.models.complaint import Complaint, EscalationEvent
from app.models.master import SlaPolicy
from app.schemas.complaint import ComplaintStatus
from datetime import datetime, timezone
import uuid


class EscalationRepository:
    def __init__(self, db: AsyncSession):
        self.db = db

    async def get_open_complaints(self) -> list[Complaint]:
        result = await self.db.execute(
            select(Complaint)
            .options(selectinload(Complaint.itop_mapping))
            .where(
                Complaint.status != ComplaintStatus.Resolved,
                Complaint.status != ComplaintStatus.Closed,
                Complaint.status != ComplaintStatus.Rejected
            )
        )
        return list(result.scalars().all())

    async def get_active_policy_by_category(self, category_id: int) -> SlaPolicy | None:
        result = await self.db.execute(
            select(SlaPolicy)
            .where(SlaPolicy.category_id == category_id, SlaPolicy.is_active == True)
        )
        return result.scalar_one_or_none()

    async def has_escalation(self, complaint_id: uuid.UUID, level: int) -> bool:
        result = await self.db.execute(
            select(EscalationEvent)
            .where(
                EscalationEvent.complaint_id == complaint_id,
                EscalationEvent.level == level
            )
        )
        return result.scalar_one_or_none() is not None

    async def add_escalation(self, event: EscalationEvent) -> EscalationEvent:
        self.db.add(event)
        await self.db.flush()
        await self.db.refresh(event)
        return event

    async def get_by_complaint(self, complaint_id: uuid.UUID) -> list[EscalationEvent]:
        result = await self.db.execute(
            select(EscalationEvent)
            .options(selectinload(EscalationEvent.complaint))
            .where(EscalationEvent.complaint_id == complaint_id)
            .order_by(EscalationEvent.triggered_at.asc())
        )
        return list(result.scalars().all())

    async def get_active(self) -> list[EscalationEvent]:
        result = await self.db.execute(
            select(EscalationEvent)
            .options(selectinload(EscalationEvent.complaint))
            .where(EscalationEvent.resolved_at == None)
            .order_by(EscalationEvent.triggered_at.desc())
        )
        return list(result.scalars().all())

    async def resolve_active_for_complaint(self, complaint_id: uuid.UUID) -> None:
        result = await self.db.execute(
            select(EscalationEvent)
            .where(
                EscalationEvent.complaint_id == complaint_id,
                EscalationEvent.resolved_at == None
            )
        )
        events = list(result.scalars().all())
        for event in events:
            event.resolved_at = datetime.now(timezone.utc)
        if events:
            await self.db.flush()
