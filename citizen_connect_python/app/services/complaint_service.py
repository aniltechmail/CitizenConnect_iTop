from datetime import datetime, timezone
from sqlalchemy.ext.asyncio import AsyncSession
from fastapi import UploadFile

from app.models.complaint import (
    Complaint, ComplaintMedia, ComplaintMessage, ComplaintITopMapping
)
from app.repositories.complaint_repository import ComplaintRepository
from app.repositories.internal_user_repository import InternalUserRepository
from app.repositories.location_repository import LocationRepository
from app.schemas.complaint import (
    SubmitComplaintSchema, AssignDepartmentSchema, UpdateStatusSchema,
    ComplaintResponseSchema, ComplaintMediaResponseSchema,
    PagedComplaintsSchema, ComplaintStatus, MediaTypeEnum, SenderTypeEnum
)
from app.services.storage_service import LocalStorageService
from app.services.itop_adapter import ITopTicketAdapter, ITopTicketCreateRequest
import uuid


class ComplaintService:
    def __init__(self, db: AsyncSession):
        self.db = db
        self.complaint_repo = ComplaintRepository(db)
        self.user_repo = InternalUserRepository(db)
        self.location_repo = LocationRepository(db)
        self.storage = LocalStorageService()
        self.itop_adapter = ITopTicketAdapter()

    async def submit_complaint(
        self,
        citizen_id: uuid.UUID,
        dto: SubmitComplaintSchema
    ) -> ComplaintResponseSchema:
        # Validate block exists
        block = await self.location_repo.get_block_by_id(dto.block_id)
        if not block:
            raise KeyError("Block not found.")

        ref_number = await self.complaint_repo.generate_ref_number()
        now = datetime.now(timezone.utc)

        complaint = Complaint(
            id=uuid.uuid4(),
            ref_number=ref_number,
            title=dto.title,
            description=dto.description,
            category_id=dto.category_id,
            block_id=dto.block_id,
            citizen_id=citizen_id,
            priority=dto.priority,
            status=ComplaintStatus.Submitted,
            submitted_at=now,
            created_at=now,
            updated_at=now
        )

        await self.complaint_repo.create(complaint)

        # Reload with all relations
        created = await self.complaint_repo.get_by_id(complaint.id)
        if not created:
            raise Exception("Failed to retrieve created complaint.")
        await self._create_itop_ticket_mapping(created)
        return self._map_to_schema(created)

    async def get_by_id(self, complaint_id: uuid.UUID) -> ComplaintResponseSchema:
        complaint = await self.complaint_repo.get_by_id(complaint_id)
        if not complaint:
            raise KeyError("Complaint not found.")
        return self._map_to_schema(complaint)

    async def get_my_complaints(
        self, citizen_id: uuid.UUID
    ) -> list[ComplaintResponseSchema]:
        complaints = await self.complaint_repo.get_by_citizen_id(citizen_id)
        return [self._map_to_schema(c) for c in complaints]

    async def get_all_complaints(
        self,
        status: int | None,
        department_id: int | None,
        block_id: int | None,
        page: int,
        page_size: int
    ) -> PagedComplaintsSchema:
        items = await self.complaint_repo.get_all(
            status, department_id, block_id, page, page_size
        )
        total = await self.complaint_repo.get_total_count(
            status, department_id, block_id
        )
        total_pages = -(-total // page_size)  # ceiling division
        return PagedComplaintsSchema(
            items=[self._map_to_schema(c) for c in items],
            total_count=total,
            page=page,
            page_size=page_size,
            total_pages=total_pages
        )

    async def assign_department(
        self,
        complaint_id: uuid.UUID,
        dto: AssignDepartmentSchema,
        assigned_by_id: uuid.UUID
    ) -> ComplaintResponseSchema:
        complaint = await self.complaint_repo.get_by_id(complaint_id)
        if not complaint:
            raise KeyError("Complaint not found.")
        if complaint.status != ComplaintStatus.Submitted:
            raise ValueError("Only submitted complaints can be assigned.")

        complaint.assigned_department_id = dto.department_id
        complaint.status = ComplaintStatus.Assigned
        complaint.assigned_at = datetime.now(timezone.utc)
        complaint.updated_at = datetime.now(timezone.utc)

        await self.complaint_repo.update(complaint)
        updated = await self.complaint_repo.get_by_id(complaint_id)
        return self._map_to_schema(updated)

    async def assign_agent(
        self,
        complaint_id: uuid.UUID,
        agent_id: uuid.UUID,
        assigned_by_id: uuid.UUID
    ) -> ComplaintResponseSchema:
        complaint = await self.complaint_repo.get_by_id(complaint_id)
        if not complaint:
            raise KeyError("Complaint not found.")

        agent = await self.user_repo.get_by_id(agent_id)
        if not agent:
            raise KeyError("Agent not found.")

        complaint.assigned_agent_id = agent_id
        if complaint.status == ComplaintStatus.Assigned:
            complaint.status = ComplaintStatus.InProgress
        complaint.updated_at = datetime.now(timezone.utc)

        await self.complaint_repo.update(complaint)
        updated = await self.complaint_repo.get_by_id(complaint_id)
        return self._map_to_schema(updated)

    async def update_status(
        self,
        complaint_id: uuid.UUID,
        dto: UpdateStatusSchema,
        updated_by_id: uuid.UUID
    ) -> ComplaintResponseSchema:
        complaint = await self.complaint_repo.get_by_id(complaint_id)
        if not complaint:
            raise KeyError("Complaint not found.")

        self._validate_status_transition(complaint.status, dto.status)
        complaint.status = dto.status
        complaint.updated_at = datetime.now(timezone.utc)

        if dto.status == ComplaintStatus.Resolved:
            complaint.resolved_at = datetime.now(timezone.utc)
        elif dto.status == ComplaintStatus.Closed:
            complaint.closed_at = datetime.now(timezone.utc)

        await self.complaint_repo.update(complaint)

        # Add system message separately
        message = ComplaintMessage(
            id=uuid.uuid4(),
            complaint_id=complaint_id,
            sender_type=SenderTypeEnum.System,
            sender_id=updated_by_id,
            message=dto.remarks or f"Status updated to {ComplaintStatus.to_string(dto.status)}",
            is_read=False,
            created_at=datetime.now(timezone.utc)
        )
        await self.complaint_repo.add_message(message)

        updated = await self.complaint_repo.get_by_id(complaint_id)
        return self._map_to_schema(updated)

    async def upload_media(
        self,
        complaint_id: uuid.UUID,
        file: UploadFile,
        uploaded_by_id: uuid.UUID
    ) -> ComplaintMediaResponseSchema:
        complaint = await self.complaint_repo.get_by_id(complaint_id)
        if not complaint:
            raise KeyError("Complaint not found.")

        self._validate_upload(file)
        content = await file.read()
        if len(content) == 0:
            raise ValueError("Uploaded file is empty.")
        if len(content) > 50 * 1024 * 1024:
            raise ValueError("Uploaded file exceeds the 50 MB limit.")
        media_type = self.storage.determine_media_type(file.content_type or "")
        folder = f"complaints/{complaint_id}"

        file_path = await self.storage.save_file(
            content,
            file.filename or "upload",
            folder
        )

        media = ComplaintMedia(
            id=uuid.uuid4(),
            complaint_id=complaint_id,
            media_type=media_type,
            file_name=file.filename or "upload",
            file_path=file_path,
            file_size=len(content),
            mime_type=file.content_type,
            created_at=datetime.now(timezone.utc)
        )

        await self.complaint_repo.add_media(media)

        return ComplaintMediaResponseSchema(
            id=media.id,
            media_type=MediaTypeEnum.to_string(media.media_type),
            file_name=media.file_name,
            file_url=self.storage.get_file_url(media.file_path),
            file_size=media.file_size,
            created_at=media.created_at
        )

    # ── Mapper ─────────────────────────────────────────────────────────────

    async def _create_itop_ticket_mapping(self, complaint: Complaint) -> None:
        result = await self.itop_adapter.create_ticket(
            ITopTicketCreateRequest(
                complaint_id=str(complaint.id),
                ref_number=complaint.ref_number,
                title=complaint.title,
                description=complaint.description,
                citizen_name=complaint.citizen.full_name if complaint.citizen else "",
                citizen_phone=complaint.citizen.phone if complaint.citizen else "",
                category_name=complaint.category.name if complaint.category else "",
                block_name=complaint.block.name if complaint.block else "",
                priority=complaint.priority
            )
        )
        if not result.was_attempted:
            return

        mapping = ComplaintITopMapping(
            id=uuid.uuid4(),
            complaint_id=complaint.id,
            itop_ticket_ref=result.ticket_ref or "",
            itop_ticket_id=result.ticket_id or "",
            itop_class="UserRequest",
            last_synced_at=datetime.now(timezone.utc) if result.success else None,
            sync_status=1 if result.success else 2,
            last_sync_error=None if result.success else result.error
        )
        await self.complaint_repo.add_itop_mapping(mapping)

    @staticmethod
    def _validate_status_transition(current: int, new_status: int) -> None:
        allowed = {
            ComplaintStatus.Submitted: {ComplaintStatus.Assigned, ComplaintStatus.Rejected},
            ComplaintStatus.Assigned: {ComplaintStatus.InProgress, ComplaintStatus.Rejected},
            ComplaintStatus.InProgress: {ComplaintStatus.Resolved, ComplaintStatus.Rejected},
            ComplaintStatus.Resolved: {ComplaintStatus.Closed, ComplaintStatus.InProgress},
        }
        if new_status not in allowed.get(current, set()):
            raise ValueError(
                f"Invalid status transition from "
                f"{ComplaintStatus.to_string(current)} to "
                f"{ComplaintStatus.to_string(new_status)}."
            )

    @staticmethod
    def _validate_upload(file: UploadFile) -> None:
        max_bytes = 50 * 1024 * 1024
        if not file.filename:
            raise ValueError("Uploaded file name is required.")
        size = getattr(file, "size", None)
        if size is not None and size <= 0:
            raise ValueError("Uploaded file is empty.")
        if size is not None and size > max_bytes:
            raise ValueError("Uploaded file exceeds the 50 MB limit.")

    def _map_to_schema(self, c: Complaint) -> ComplaintResponseSchema:
        return ComplaintResponseSchema(
            id=c.id,
            ref_number=c.ref_number,
            title=c.title,
            description=c.description,
            status=ComplaintStatus.to_string(c.status),
            priority=c.priority,
            created_at=c.created_at,
            submitted_at=c.submitted_at,
            assigned_at=c.assigned_at,
            resolved_at=c.resolved_at,
            citizen_id=c.citizen_id,
            citizen_name=c.citizen.full_name if c.citizen else "",
            citizen_phone=c.citizen.phone if c.citizen else "",
            block_id=c.block_id,
            block_name=c.block.name if c.block else "",
            category_id=c.category_id,
            category_name=c.category.name if c.category else "",
            assigned_department_id=c.assigned_department_id,
            assigned_department_name=c.assigned_department.name if c.assigned_department else None,
            assigned_agent_id=c.assigned_agent_id,
            assigned_agent_name=c.assigned_agent.full_name if c.assigned_agent else None,
            media=[
                ComplaintMediaResponseSchema(
                    id=m.id,
                    media_type=MediaTypeEnum.to_string(m.media_type),
                    file_name=m.file_name,
                    file_url=self.storage.get_file_url(m.file_path),
                    file_size=m.file_size,
                    created_at=m.created_at
                )
                for m in c.media
            ]
        )
