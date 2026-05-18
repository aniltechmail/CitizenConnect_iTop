from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from app.models.notification import Notification
from app.services.push_notification_service import PushNotificationService
import uuid


class NotificationRepository:
    def __init__(self, db: AsyncSession):
        self.db = db
        self.push = PushNotificationService(db)

    async def add(self, notification: Notification) -> Notification:
        self.db.add(notification)
        await self.db.flush()
        await self.db.refresh(notification)
        await self.push.send(
            notification.user_type,
            notification.user_id,
            "Citizen Connect",
            notification.message,
            notification.complaint_id,
        )
        return notification

    async def add_many(self, notifications: list[Notification]) -> None:
        if not notifications:
            return
        self.db.add_all(notifications)
        await self.db.flush()
        for notification in notifications:
            await self.push.send(
                notification.user_type,
                notification.user_id,
                "Citizen Connect",
                notification.message,
                notification.complaint_id,
            )

    async def get_by_user(self, user_type: int, user_id: uuid.UUID) -> list[Notification]:
        result = await self.db.execute(
            select(Notification)
            .where(Notification.user_type == user_type, Notification.user_id == user_id)
            .order_by(Notification.created_at.desc())
        )
        return list(result.scalars().all())

    async def get_by_id(self, notification_id: uuid.UUID) -> Notification | None:
        result = await self.db.execute(
            select(Notification).where(Notification.id == notification_id)
        )
        return result.scalar_one_or_none()

    async def update(self, notification: Notification) -> Notification:
        await self.db.flush()
        await self.db.refresh(notification)
        return notification
