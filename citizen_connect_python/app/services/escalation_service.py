from datetime import datetime, timezone
from sqlalchemy.ext.asyncio import AsyncSession
from app.models.complaint import Complaint, EscalationEvent
from app.models.identity import UserRole
from app.models.notification import Notification
from app.repositories.complaint_repository import ComplaintRepository
from app.repositories.escalation_repository import EscalationRepository
from app.repositories.internal_user_repository import InternalUserRepository
from app.repositories.notification_repository import NotificationRepository
from app.schemas.complaint import ComplaintStatus, SenderTypeEnum
from app.schemas.escalation import EscalationEventResponseSchema
from app.schemas.notification import NotificationType
from app.services.itop_adapter import ITopTicketAdapter, ITopTicketLogRequest
import uuid


class EscalationService:
    def __init__(self, db: AsyncSession):
        self.complaint_repo = ComplaintRepository(db)
        self.escalation_repo = EscalationRepository(db)
        self.user_repo = InternalUserRepository(db)
        self.notification_repo = NotificationRepository(db)
        self.itop_adapter = ITopTicketAdapter()

    async def scan(self) -> int:
        now = datetime.now(timezone.utc)
        created = 0
        complaints = await self.escalation_repo.get_open_complaints()

        for complaint in complaints:
            policy = await self.escalation_repo.get_active_policy_by_category(
                complaint.category_id
            )
            if not policy:
                continue

            started_at = complaint.submitted_at or complaint.created_at
            age_hours = (now - started_at).total_seconds() / 3600

            if complaint.assigned_at is None and age_hours >= policy.escalation_level1_hours:
                created += await self._create_escalation_if_needed(
                    complaint,
                    1,
                    "Complaint has not been assigned within Level 1 escalation threshold."
                )

            if complaint.resolved_at is None and age_hours >= policy.escalation_level2_hours:
                created += await self._create_escalation_if_needed(
                    complaint,
                    2,
                    "Complaint has not been resolved within Level 2 escalation threshold."
                )

            if complaint.resolved_at is None and age_hours >= policy.escalation_level3_hours:
                created += await self._create_escalation_if_needed(
                    complaint,
                    3,
                    "Complaint is still unresolved within Level 3 escalation threshold."
                )

            response_breached = complaint.assigned_at is None and age_hours >= policy.response_hours
            resolution_breached = complaint.resolved_at is None and age_hours >= policy.resolution_hours
            if (response_breached or resolution_breached) and not complaint.is_sla_breached:
                complaint.is_sla_breached = True
                complaint.updated_at = datetime.now(timezone.utc)
                await self.complaint_repo.update(complaint)

        return created

    async def get_by_complaint(
        self,
        complaint_id: uuid.UUID
    ) -> list[EscalationEventResponseSchema]:
        events = await self.escalation_repo.get_by_complaint(complaint_id)
        return [self._map_to_schema(e) for e in events]

    async def get_active(self) -> list[EscalationEventResponseSchema]:
        events = await self.escalation_repo.get_active()
        return [self._map_to_schema(e) for e in events]

    async def resolve_active_for_complaint(self, complaint_id: uuid.UUID) -> None:
        await self.escalation_repo.resolve_active_for_complaint(complaint_id)

    async def _create_escalation_if_needed(
        self,
        complaint: Complaint,
        level: int,
        reason: str
    ) -> int:
        if await self.escalation_repo.has_escalation(complaint.id, level):
            return 0

        event = EscalationEvent(
            id=uuid.uuid4(),
            complaint_id=complaint.id,
            level=level,
            reason=reason,
            triggered_at=datetime.now(timezone.utc)
        )
        await self.escalation_repo.add_escalation(event)

        complaint.is_sla_breached = True
        complaint.updated_at = datetime.now(timezone.utc)
        await self.complaint_repo.update(complaint)

        await self._notify_escalation(complaint, level, reason)
        await self._sync_escalation_to_itop(complaint, level, reason)
        return 1

    async def _notify_escalation(
        self,
        complaint: Complaint,
        level: int,
        reason: str
    ) -> None:
        recipients: list[uuid.UUID] = []
        if level == 1:
            recipients.extend([u.id for u in await self.user_repo.get_by_role(int(UserRole.Supervisor))])
        elif level == 2:
            recipients.extend([u.id for u in await self.user_repo.get_by_role(int(UserRole.Admin))])
            if complaint.assigned_department_id is not None:
                recipients.extend([
                    u.id for u in await self.user_repo.get_by_role_and_department(
                        int(UserRole.DepartmentHead),
                        complaint.assigned_department_id
                    )
                ])
        else:
            recipients.extend([u.id for u in await self.user_repo.get_by_role(int(UserRole.TopManagement))])

        notifications = [
            Notification(
                id=uuid.uuid4(),
                user_type=SenderTypeEnum.Agent,
                user_id=user_id,
                complaint_id=complaint.id,
                type=NotificationType.EscalationTriggered,
                message=f"Level {level} escalation triggered for complaint {complaint.ref_number}: {reason}",
                is_read=False,
                created_at=datetime.now(timezone.utc)
            )
            for user_id in set(recipients)
        ]
        await self.notification_repo.add_many(notifications)

    async def _sync_escalation_to_itop(
        self,
        complaint: Complaint,
        level: int,
        reason: str
    ) -> None:
        mapping = complaint.itop_mapping
        if not mapping or mapping.sync_status != 1:
            return

        result = await self.itop_adapter.add_private_log(
            ITopTicketLogRequest(
                itop_ticket_id=mapping.itop_ticket_id,
                itop_class=mapping.itop_class,
                complaint_ref_number=complaint.ref_number,
                message=f"[Escalation Level {level}] {reason}",
                is_private=True
            )
        )

        if result.success:
            mapping.last_synced_at = datetime.now(timezone.utc)
            mapping.last_sync_error = None
        else:
            mapping.last_sync_error = f"Escalation sync failed: {result.error}"
        await self.complaint_repo.update_itop_mapping(mapping)

    @staticmethod
    def _map_to_schema(event: EscalationEvent) -> EscalationEventResponseSchema:
        return EscalationEventResponseSchema(
            id=event.id,
            complaint_id=event.complaint_id,
            complaint_ref_number=event.complaint.ref_number if event.complaint else "",
            level=event.level,
            reason=event.reason,
            triggered_at=event.triggered_at,
            resolved_at=event.resolved_at
        )
