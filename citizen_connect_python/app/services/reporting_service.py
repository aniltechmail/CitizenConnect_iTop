import csv
import io
from collections import defaultdict
from datetime import datetime
from sqlalchemy import func, select
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.orm import selectinload

from app.models.complaint import Complaint, EscalationEvent
from app.models.location import Area, Block, Constituency
from app.schemas.complaint import ComplaintStatus
from app.schemas.report import (
    AgentPerformanceSchema,
    ComplaintReportItemSchema,
    DashboardSummarySchema,
    DepartmentPerformanceSchema,
    LocationCountSchema,
    LocationReportSchema,
    NamedCountSchema,
    PagedComplaintReportSchema,
)


class ReportingService:
    def __init__(self, db: AsyncSession):
        self.db = db

    async def get_dashboard_summary(self) -> DashboardSummarySchema:
        complaints = await self._load_complaints()
        escalation_count = await self.db.scalar(select(func.count(EscalationEvent.id)))

        return DashboardSummarySchema(
            complaints_by_status=[
                NamedCountSchema(name=name, count=count)
                for name, count in sorted(self._count_by(complaints, lambda c: ComplaintStatus.to_string(c.status)).items())
            ],
            complaints_by_department=[
                NamedCountSchema(name=name, count=count)
                for name, count in sorted(self._count_by(complaints, lambda c: c.assigned_department.name if c.assigned_department else "Unassigned").items())
            ],
            complaints_by_location=[
                LocationCountSchema(
                    district=key[0],
                    constituency=key[1],
                    area=key[2],
                    block=key[3],
                    count=count
                )
                for key, count in self._location_counts(complaints).items()
            ],
            average_resolution_hours=self._average_resolution_hours(complaints),
            sla_breach_count=sum(1 for c in complaints if c.is_sla_breached),
            escalation_count=escalation_count or 0
        )

    async def get_complaint_report(
        self,
        from_date: datetime | None,
        to_date: datetime | None,
        status: int | None,
        department_id: int | None,
        district_id: int | None,
        constituency_id: int | None,
        area_id: int | None,
        block_id: int | None,
        page: int,
        page_size: int
    ) -> PagedComplaintReportSchema:
        complaints = await self._load_complaints()
        filtered = self._apply_filters(
            complaints, from_date, to_date, status, department_id,
            district_id, constituency_id, area_id, block_id
        )
        total = len(filtered)
        start = (page - 1) * page_size
        items = filtered[start:start + page_size]
        return PagedComplaintReportSchema(
            items=[self._map_complaint(c) for c in items],
            total_count=total,
            page=page,
            page_size=page_size
        )

    async def export_complaint_report_csv(
        self,
        from_date: datetime | None,
        to_date: datetime | None,
        status: int | None,
        department_id: int | None,
        district_id: int | None,
        constituency_id: int | None,
        area_id: int | None,
        block_id: int | None
    ) -> str:
        complaints = await self._load_complaints()
        filtered = self._apply_filters(
            complaints, from_date, to_date, status, department_id,
            district_id, constituency_id, area_id, block_id
        )
        output = io.StringIO()
        writer = csv.writer(output)
        writer.writerow([
            "RefNumber", "Title", "Status", "Category", "Department",
            "District", "Constituency", "Area", "Block",
            "CreatedAt", "SubmittedAt", "ResolvedAt", "IsSlaBreached"
        ])
        for item in [self._map_complaint(c) for c in filtered]:
            writer.writerow([
                item.ref_number, item.title, item.status, item.category, item.department or "",
                item.district, item.constituency, item.area, item.block,
                item.created_at.isoformat(),
                item.submitted_at.isoformat() if item.submitted_at else "",
                item.resolved_at.isoformat() if item.resolved_at else "",
                item.is_sla_breached
            ])
        return output.getvalue()

    async def get_department_performance(self) -> list[DepartmentPerformanceSchema]:
        complaints = await self._load_complaints()
        groups: dict[tuple[int | None, str], list[Complaint]] = defaultdict(list)
        for c in complaints:
            groups[(c.assigned_department_id, c.assigned_department.name if c.assigned_department else "Unassigned")].append(c)

        result = []
        for (department_id, name), items in groups.items():
            total = len(items)
            result.append(DepartmentPerformanceSchema(
                department_id=department_id,
                department=name,
                total_assigned=total,
                resolved_count=sum(1 for c in items if c.status in {ComplaintStatus.Resolved, ComplaintStatus.Closed}),
                average_resolution_hours=self._average_resolution_hours(items),
                sla_breach_rate=round((sum(1 for c in items if c.is_sla_breached) * 100 / total) if total else 0, 2)
            ))
        return sorted(result, key=lambda x: x.department)

    async def get_agent_performance(self) -> list[AgentPerformanceSchema]:
        complaints = [c for c in await self._load_complaints() if c.assigned_agent_id]
        groups: dict[tuple[object, str], list[Complaint]] = defaultdict(list)
        for c in complaints:
            groups[(c.assigned_agent_id, c.assigned_agent.full_name if c.assigned_agent else "Unknown")].append(c)

        result = []
        for (agent_id, name), items in groups.items():
            ratings = [c.feedback.rating for c in items if c.feedback]
            result.append(AgentPerformanceSchema(
                agent_id=agent_id,
                agent_name=name,
                complaints_handled=len(items),
                average_resolution_hours=self._average_resolution_hours(items),
                average_feedback_rating=round(sum(ratings) / len(ratings), 2) if ratings else 0
            ))
        return sorted(result, key=lambda x: x.agent_name)

    async def get_location_report(self) -> list[LocationReportSchema]:
        complaints = await self._load_complaints()
        return [
            LocationReportSchema(
                district=key[0],
                constituency=key[1],
                area=key[2],
                block=key[3],
                count=count
            )
            for key, count in sorted(self._location_counts(complaints).items())
        ]

    async def _load_complaints(self) -> list[Complaint]:
        result = await self.db.execute(
            select(Complaint)
            .options(
                selectinload(Complaint.category),
                selectinload(Complaint.assigned_department),
                selectinload(Complaint.assigned_agent),
                selectinload(Complaint.feedback),
                selectinload(Complaint.block)
                .selectinload(Block.area)
                .selectinload(Area.constituency)
                .selectinload(Constituency.district)
            )
            .order_by(Complaint.created_at.desc())
        )
        return list(result.scalars().all())

    @staticmethod
    def _apply_filters(
        complaints: list[Complaint],
        from_date: datetime | None,
        to_date: datetime | None,
        status: int | None,
        department_id: int | None,
        district_id: int | None,
        constituency_id: int | None,
        area_id: int | None,
        block_id: int | None
    ) -> list[Complaint]:
        items = complaints
        if from_date:
            items = [c for c in items if c.created_at >= from_date]
        if to_date:
            items = [c for c in items if c.created_at <= to_date]
        if status is not None:
            items = [c for c in items if c.status == status]
        if department_id is not None:
            items = [c for c in items if c.assigned_department_id == department_id]
        if district_id is not None:
            items = [c for c in items if c.block.area.constituency.district_id == district_id]
        if constituency_id is not None:
            items = [c for c in items if c.block.area.constituency_id == constituency_id]
        if area_id is not None:
            items = [c for c in items if c.block.area_id == area_id]
        if block_id is not None:
            items = [c for c in items if c.block_id == block_id]
        return items

    @staticmethod
    def _map_complaint(c: Complaint) -> ComplaintReportItemSchema:
        return ComplaintReportItemSchema(
            id=c.id,
            ref_number=c.ref_number,
            title=c.title,
            status=ComplaintStatus.to_string(c.status),
            category=c.category.name if c.category else "",
            department=c.assigned_department.name if c.assigned_department else None,
            district=c.block.area.constituency.district.name,
            constituency=c.block.area.constituency.name,
            area=c.block.area.name,
            block=c.block.name,
            created_at=c.created_at,
            submitted_at=c.submitted_at,
            resolved_at=c.resolved_at,
            is_sla_breached=c.is_sla_breached
        )

    @staticmethod
    def _average_resolution_hours(complaints) -> float:
        durations = [
            (c.resolved_at - (c.submitted_at or c.created_at)).total_seconds() / 3600
            for c in complaints
            if c.resolved_at
        ]
        return round(sum(durations) / len(durations), 2) if durations else 0

    @staticmethod
    def _count_by(complaints, key_func):
        counts = defaultdict(int)
        for complaint in complaints:
            counts[key_func(complaint)] += 1
        return counts

    @staticmethod
    def _location_counts(complaints):
        counts = defaultdict(int)
        for c in complaints:
            counts[(
                c.block.area.constituency.district.name,
                c.block.area.constituency.name,
                c.block.area.name,
                c.block.name
            )] += 1
        return counts
