from pathlib import Path
import uuid
from fastapi import APIRouter, Depends, HTTPException
from fastapi.responses import FileResponse
from sqlalchemy import select
from sqlalchemy.orm import selectinload
from sqlalchemy.ext.asyncio import AsyncSession

from app.database import get_db
from app.models.complaint import Complaint, ComplaintMedia
from app.routers.complaint import decode_token

router = APIRouter(prefix="/api/complaint", tags=["Mobile Complaint"])


@router.get("/{complaint_id}/media/{media_id}")
async def download_media(
    complaint_id: uuid.UUID,
    media_id: uuid.UUID,
    payload: dict = Depends(decode_token),
    db: AsyncSession = Depends(get_db),
):
    result = await db.execute(
        select(Complaint)
        .options(selectinload(Complaint.media))
        .where(Complaint.id == complaint_id)
    )
    complaint = result.scalar_one_or_none()
    if not complaint:
        raise HTTPException(status_code=404, detail="Complaint not found.")
    if payload.get("user_type") != "internal" and str(complaint.citizen_id) != str(payload.get("sub")):
        raise HTTPException(status_code=403, detail="You do not have access to this complaint.")

    media = next((m for m in complaint.media if m.id == media_id), None)
    if not media:
        media = await db.get(ComplaintMedia, media_id)
    if not media or media.complaint_id != complaint_id:
        raise HTTPException(status_code=404, detail="Media not found.")

    path = Path.cwd() / "uploads" / media.file_path
    if not path.exists():
        raise HTTPException(status_code=404, detail="File not found on disk.")
    return FileResponse(path, media_type=media.mime_type or "application/octet-stream", filename=media.file_name)
