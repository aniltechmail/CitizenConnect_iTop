import asyncio
import json
from dataclasses import dataclass
from urllib.parse import urlencode
from urllib.request import Request, urlopen
from urllib.error import HTTPError, URLError

from app.config import settings


@dataclass
class ITopTicketCreateRequest:
    complaint_id: str
    ref_number: str
    title: str
    description: str
    citizen_name: str
    citizen_phone: str
    category_name: str
    block_name: str
    priority: int


@dataclass
class ITopTicketCreateResult:
    was_attempted: bool
    success: bool
    ticket_id: str | None = None
    ticket_ref: str | None = None
    error: str | None = None


class ITopTicketAdapter:
    async def create_ticket(
        self, request: ITopTicketCreateRequest
    ) -> ITopTicketCreateResult:
        if not settings.itop_enabled:
            return ITopTicketCreateResult(
                was_attempted=False,
                success=False,
                error="iTop integration is disabled."
            )

        if not settings.itop_base_url or not settings.itop_username or not settings.itop_password:
            return ITopTicketCreateResult(
                was_attempted=False,
                success=False,
                error="iTop BaseUrl, Username, or Password is missing."
            )

        return await asyncio.to_thread(self._create_ticket_sync, request)

    def _create_ticket_sync(
        self, request: ITopTicketCreateRequest
    ) -> ITopTicketCreateResult:
        fields = {
            "title": f"[{request.ref_number}] {request.title}",
            "description": self._build_description(request)
        }
        self._add_configured_field(fields, "org_id", settings.itop_organization_id)
        self._add_configured_field(fields, "caller_id", settings.itop_caller_id)
        self._add_configured_field(fields, "service_id", settings.itop_service_id)
        self._add_configured_field(
            fields,
            "servicesubcategory_id",
            settings.itop_service_subcategory_id
        )

        payload = {
            "operation": "core/create",
            "class": settings.itop_ticket_class,
            "comment": f"Created from CitizenConnect complaint {request.ref_number}",
            "fields": fields
        }
        form = urlencode({
            "auth_user": settings.itop_username,
            "auth_pwd": settings.itop_password,
            "json_data": json.dumps(payload)
        }).encode("utf-8")
        endpoint = (
            f"{settings.itop_base_url.rstrip('/')}/webservices/rest.php"
            f"?version={settings.itop_api_version}"
        )

        try:
            http_request = Request(endpoint, data=form, method="POST")
            http_request.add_header(
                "Content-Type", "application/x-www-form-urlencoded"
            )
            with urlopen(http_request, timeout=30) as response:
                body = response.read().decode("utf-8")
            return self._parse_create_response(body)
        except HTTPError as exc:
            body = exc.read().decode("utf-8", errors="replace")
            return ITopTicketCreateResult(
                was_attempted=True,
                success=False,
                error=f"iTop returned HTTP {exc.code}: {body}"
            )
        except URLError as exc:
            return ITopTicketCreateResult(
                was_attempted=True,
                success=False,
                error=str(exc.reason)
            )
        except Exception as exc:
            return ITopTicketCreateResult(
                was_attempted=True,
                success=False,
                error=str(exc)
            )

    @staticmethod
    def _build_description(request: ITopTicketCreateRequest) -> str:
        return (
            f"Complaint Ref: {request.ref_number}\n"
            f"Citizen: {request.citizen_name} ({request.citizen_phone})\n"
            f"Category: {request.category_name}\n"
            f"Block: {request.block_name}\n"
            f"Priority: {request.priority}\n\n"
            f"{request.description}"
        )

    @staticmethod
    def _add_configured_field(
        fields: dict[str, str | int], field_name: str, value: str
    ) -> None:
        if value:
            fields[field_name] = value

    @staticmethod
    def _parse_create_response(body: str) -> ITopTicketCreateResult:
        parsed = json.loads(body)
        if parsed.get("code", 0) != 0:
            return ITopTicketCreateResult(
                was_attempted=True,
                success=False,
                error=parsed.get("message", body)
            )

        objects = parsed.get("objects") or {}
        for object_name, ticket in objects.items():
            ticket_id = str(ticket.get("key") or object_name)
            fields = ticket.get("fields") or {}
            ticket_ref = str(fields.get("ref") or ticket_id)
            return ITopTicketCreateResult(
                was_attempted=True,
                success=True,
                ticket_id=ticket_id,
                ticket_ref=ticket_ref
            )

        return ITopTicketCreateResult(
            was_attempted=True,
            success=False,
            error=f"No ticket object returned by iTop: {body}"
        )
