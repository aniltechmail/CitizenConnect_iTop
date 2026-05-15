from sqlalchemy.ext.asyncio import AsyncSession
from app.repositories.notification_repository import NotificationRepository
from app.schemas.complaint import SenderTypeEnum
from app.schemas.notification import NotificationResponseSchema, NotificationType
import uuid


class NotificationService:
    def __init__(self, db: AsyncSession):
        self.notification_repo = NotificationRepository(db)

    async def get_my_notifications(
        self,
        user_type: int,
        user_id: uuid.UUID
    ) -> list[NotificationResponseSchema]:
        notifications = await self.notification_repo.get_by_user(user_type, user_id)
        return [self._map_to_schema(n) for n in notifications]

    async def mark_as_read(
        self,
        notification_id: uuid.UUID,
        user_type: int,
        user_id: uuid.UUID
    ) -> NotificationResponseSchema:
        notification = await self.notification_repo.get_by_id(notification_id)
        if not notification:
            raise KeyError("Notification not found.")
        if notification.user_type != user_type or notification.user_id != user_id:
            raise PermissionError("You do not have access to this notification.")

        notification.is_read = True
        await self.notification_repo.update(notification)
        return self._map_to_schema(notification)

    @staticmethod
    def _map_to_schema(notification) -> NotificationResponseSchema:
        return NotificationResponseSchema(
            id=notification.id,
            user_type=SenderTypeEnum.to_string(notification.user_type),
            user_id=notification.user_id,
            complaint_id=notification.complaint_id,
            type=NotificationType.to_string(notification.type),
            message=notification.message,
            is_read=notification.is_read,
            created_at=notification.created_at
        )
