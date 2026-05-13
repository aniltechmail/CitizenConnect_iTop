import os
import uuid
from pathlib import PurePath
from pathlib import Path
from app.config import settings


class LocalStorageService:
    def __init__(self):
        self.base_path = Path(os.getcwd()) / "uploads"
        self.base_url = settings.file_storage_base_url

    async def save_file(
        self,
        file_content: bytes,
        file_name: str,
        folder: str
    ) -> str:
        folder_path = self.base_path / folder
        folder_path.mkdir(parents=True, exist_ok=True)

        safe_name = PurePath(file_name).name or "upload"
        unique_name = f"{uuid.uuid4()}_{safe_name}"
        file_path = folder_path / unique_name

        with open(file_path, "wb") as f:
            f.write(file_content)

        # Return relative path with forward slashes
        return f"{folder}/{unique_name}"

    async def delete_file(self, file_path: str) -> None:
        full_path = self.base_path / file_path
        if full_path.exists():
            full_path.unlink()

    def get_file_url(self, file_path: str) -> str:
        return f"{self.base_url}/{file_path}"

    def determine_media_type(self, content_type: str) -> int:
        if content_type.startswith("image/"):
            return 0  # Image
        elif content_type.startswith("video/"):
            return 1  # Video
        elif content_type.startswith("audio/"):
            return 2  # Voice
        else:
            return 3  # Document
