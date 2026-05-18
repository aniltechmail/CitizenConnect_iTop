import asyncio
from urllib.parse import urlencode
from urllib.request import Request, urlopen

from app.config import settings


def normalize_mobile(mobile: str) -> str:
    if not mobile or not mobile.strip():
        raise ValueError("Mobile number is required")
    value = mobile.strip().replace(" ", "").replace("-", "")
    if value.startswith("+"):
        value = value[1:]
    if value.startswith("91") and len(value) > 10:
        value = value[-10:]
    if len(value) != 10 or not value.isdigit():
        raise ValueError("Invalid mobile number format")
    return f"+91{value}"


def sms_provider_mobile(mobile: str) -> str:
    normalized = normalize_mobile(mobile)
    return normalized[3:] if normalized.startswith("+91") else normalized


def fixed_otp_users() -> list[dict[str, str]]:
    users: list[dict[str, str]] = []
    for item in settings.otp_fixed_users.split(","):
        parts = [x.strip() for x in item.split(":")]
        if len(parts) == 3:
            users.append({"mobile_number": normalize_mobile(parts[0]), "otp": parts[1], "user_type": parts[2]})
    return users


class SmsService:
    async def send(self, mobile_number: str, message: str) -> None:
        mobile = sms_provider_mobile(mobile_number)
        query = urlencode({
            "AUTH_KEY": settings.sms_provider_auth_key,
            "message": message,
            "senderId": settings.sms_provider_sender_id,
            "routeId": settings.sms_provider_route_id,
            "mobileNos": mobile,
            "smsContentType": settings.sms_provider_sms_content_type,
        })
        url = f"{settings.sms_provider_base_url}?{query}"

        def send_request() -> tuple[int, str]:
            request = Request(
                url,
                headers={
                    "Cache-Control": "no-cache",
                    "User-Agent": "Mozilla/5.0 (FastAPI)",
                },
                method="GET",
            )
            with urlopen(request, timeout=20) as response:
                return response.status, response.read().decode("utf-8", errors="replace")

        status, text = await asyncio.to_thread(send_request)
        if status < 200 or status >= 300 or "error" in text.lower():
            raise RuntimeError(f"SMS failed. Status: {status}, Response: {text}")
