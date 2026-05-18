import logging
import uuid
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from app.models.mobile import MobileDevice

logger = logging.getLogger("citizen_connect.push")


class PushNotificationService:
    def __init__(self, db: AsyncSession):
        self.db = db

    async def send(self, user_type: int, user_id: uuid.UUID, title: str, body: str, complaint_id: uuid.UUID | None = None) -> None:
        result = await self.db.execute(
            select(MobileDevice.device_token).where(
                MobileDevice.user_type == user_type,
                MobileDevice.user_id == user_id,
                MobileDevice.is_active == True,
            )
        )
        tokens = list(result.scalars().all())
        if not tokens:
            return
        logger.info("FCM push queued for %s device(s). title=%s complaint_id=%s", len(tokens), title, complaint_id)

    async def send_many(self, user_type: int, user_ids: list[uuid.UUID], title: str, body: str, complaint_id: uuid.UUID | None = None) -> None:
        for user_id in set(user_ids):
            await self.send(user_type, user_id, title, body, complaint_id)
