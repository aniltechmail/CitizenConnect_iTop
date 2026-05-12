from datetime import datetime, timezone
from sqlalchemy.ext.asyncio import AsyncSession
from fastapi import UploadFile

from app.models.complaint import Complaint, ComplaintMedia, ComplaintMessage
from app.repositories.complaint_repository import ComplaintRepository
from app.repositories.internal_user_repository import InternalUserRepository
from app.repositories.location_repository import LocationRepository
from app.schemas.complaint import (
    SubmitComplaintSchema, AssignDepartmentSchema, UpdateStatusSchema,
    ComplaintResponseSchema, ComplaintMediaResponseSchema,
    PagedComplaintsSchema, ComplaintStatus, MediaTypeEnum, SenderTypeEnum
)
from app.services.storage_service import LocalStorageService
import uuid


class ComplaintService:
    def __init__(self, db: AsyncSession):
        self.db = db
        self.complaint_repo = ComplaintRepository(db)
        self.user_repo = InternalUserRepository(db)
        self.location_repo = LocationRepository(db)
        self.storage = LocalStorageService()

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

        content = await file.read()
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