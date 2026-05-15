import asyncio
import base64
import json
from dataclasses import dataclass
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode
from urllib.request import Request, urlopen

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


@dataclass
class ITopTicketUpdateRequest:
    itop_ticket_id: str
    itop_class: str
    new_status: str
    complaint_ref_number: str
    remarks: str | None = None


@dataclass
class ITopTicketUpdateResult:
    was_attempted: bool
    success: bool
    error: str | None = None


@dataclass
class ITopTicketLogRequest:
    itop_ticket_id: str
    itop_class: str
    complaint_ref_number: str
    message: str
    is_private: bool = False


@dataclass
class ITopAttachmentCreateRequest:
    itop_ticket_id: str
    itop_class: str
    file_name: str
    mime_type: str
    content: bytes
    complaint_ref_number: str


@dataclass
class ITopAttachmentCreateResult:
    was_attempted: bool
    success: bool
    attachment_id: str | None = None
    error: str | None = None


class ITopTicketAdapter:
    async def create_ticket(
        self, request: ITopTicketCreateRequest
    ) -> ITopTicketCreateResult:
        validation_error = self._validate_ticket_settings()
        if validation_error:
            return ITopTicketCreateResult(
                was_attempted=False,
                success=False,
                error=validation_error
            )

        return await asyncio.to_thread(self._create_ticket_sync, request)

    async def update_ticket(
        self, request: ITopTicketUpdateRequest
    ) -> ITopTicketUpdateResult:
        validation_error = self._validate_ticket_settings()
        if validation_error:
            return ITopTicketUpdateResult(
                was_attempted=False,
                success=False,
                error=validation_error
            )

        return await asyncio.to_thread(self._update_ticket_sync, request)

    async def add_ticket_log(
        self, request: ITopTicketLogRequest
    ) -> ITopTicketUpdateResult:
        validation_error = self._validate_ticket_settings()
        if validation_error:
            return ITopTicketUpdateResult(
                was_attempted=False,
                success=False,
                error=validation_error
            )

        return await asyncio.to_thread(self._add_ticket_log_sync, request)

    async def create_attachment(
        self, request: ITopAttachmentCreateRequest
    ) -> ITopAttachmentCreateResult:
        validation_error = self._validate_ticket_settings()
        if validation_error:
            return ITopAttachmentCreateResult(
                was_attempted=False,
                success=False,
                error=validation_error
            )

        if not settings.itop_organization_id:
            return ITopAttachmentCreateResult(
                was_attempted=False,
                success=False,
                error="iTop OrganizationId is required for attachments."
            )

        return await asyncio.to_thread(self._create_attachment_sync, request)

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

        try:
            body = self._post_payload(payload)
            return self._parse_create_ticket_response(body)
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

    def _update_ticket_sync(
        self, request: ITopTicketUpdateRequest
    ) -> ITopTicketUpdateResult:
        fields: dict = {
            "status": self._map_status_to_itop(request.new_status)
        }
        if request.remarks:
            fields["public_log"] = request.remarks

        payload = {
            "operation": "core/update",
            "class": request.itop_class,
            "key": f"SELECT {request.itop_class} WHERE id = {request.itop_ticket_id}",
            "comment": f"Status update from CitizenConnect [{request.complaint_ref_number}]",
            "fields": fields
        }

        try:
            body = self._post_payload(payload)
            return self._parse_update_response(body)
        except HTTPError as exc:
            body = exc.read().decode("utf-8", errors="replace")
            return ITopTicketUpdateResult(
                was_attempted=True,
                success=False,
                error=f"iTop returned HTTP {exc.code}: {body}"
            )
        except Exception as exc:
            return ITopTicketUpdateResult(
                was_attempted=True,
                success=False,
                error=str(exc)
            )

    def _add_ticket_log_sync(
        self, request: ITopTicketLogRequest
    ) -> ITopTicketUpdateResult:
        fields = {
            "private_log" if request.is_private else "public_log": request.message
        }
        payload = {
            "operation": "core/update",
            "class": request.itop_class,
            "key": f"SELECT {request.itop_class} WHERE id = {request.itop_ticket_id}",
            "comment": f"Message sync from CitizenConnect [{request.complaint_ref_number}]",
            "fields": fields
        }

        try:
            body = self._post_payload(payload)
            return self._parse_update_response(body)
        except HTTPError as exc:
            body = exc.read().decode("utf-8", errors="replace")
            return ITopTicketUpdateResult(
                was_attempted=True,
                success=False,
                error=f"iTop returned HTTP {exc.code}: {body}"
            )
        except Exception as exc:
            return ITopTicketUpdateResult(
                was_attempted=True,
                success=False,
                error=str(exc)
            )

    def _create_attachment_sync(
        self, request: ITopAttachmentCreateRequest
    ) -> ITopAttachmentCreateResult:
        payload = {
            "operation": "core/create",
            "class": "Attachment",
            "comment": f"Attachment from CitizenConnect [{request.complaint_ref_number}]",
            "fields": {
                "item_class": request.itop_class,
                "item_id": request.itop_ticket_id,
                "item_org_id": settings.itop_organization_id,
                "contents": {
                    "data": base64.b64encode(request.content).decode("ascii"),
                    "filename": request.file_name,
                    "mimetype": request.mime_type or "application/octet-stream"
                }
            }
        }

        try:
            body = self._post_payload(payload)
            return self._parse_attachment_create_response(body)
        except HTTPError as exc:
            body = exc.read().decode("utf-8", errors="replace")
            return ITopAttachmentCreateResult(
                was_attempted=True,
                success=False,
                error=f"iTop returned HTTP {exc.code}: {body}"
            )
        except Exception as exc:
            return ITopAttachmentCreateResult(
                was_attempted=True,
                success=False,
                error=str(exc)
            )

    def _post_payload(self, payload: dict) -> str:
        form = urlencode({
            "auth_user": settings.itop_username,
            "auth_pwd": settings.itop_password,
            "json_data": json.dumps(payload)
        }).encode("utf-8")
        endpoint = (
            f"{settings.itop_base_url.rstrip('/')}/webservices/rest.php"
            f"?version={settings.itop_api_version}"
        )
        http_request = Request(endpoint, data=form, method="POST")
        http_request.add_header(
            "Content-Type", "application/x-www-form-urlencoded"
        )
        with urlopen(http_request, timeout=30) as response:
            return response.read().decode("utf-8")

    @staticmethod
    def _validate_ticket_settings() -> str | None:
        if not settings.itop_enabled:
            return "iTop integration is disabled."
        if not settings.itop_base_url or not settings.itop_username or not settings.itop_password:
            return "iTop BaseUrl, Username, or Password is missing."
        return None

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
    def _parse_create_ticket_response(body: str) -> ITopTicketCreateResult:
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

    @staticmethod
    def _parse_update_response(body: str) -> ITopTicketUpdateResult:
        parsed = json.loads(body)
        if parsed.get("code", 0) != 0:
            return ITopTicketUpdateResult(
                was_attempted=True,
                success=False,
                error=parsed.get("message", body)
            )
        return ITopTicketUpdateResult(was_attempted=True, success=True)

    @staticmethod
    def _parse_attachment_create_response(body: str) -> ITopAttachmentCreateResult:
        parsed = json.loads(body)
        if parsed.get("code", 0) != 0:
            return ITopAttachmentCreateResult(
                was_attempted=True,
                success=False,
                error=parsed.get("message", body)
            )

        objects = parsed.get("objects") or {}
        for object_name, attachment in objects.items():
            attachment_id = str(attachment.get("key") or object_name)
            return ITopAttachmentCreateResult(
                was_attempted=True,
                success=True,
                attachment_id=attachment_id
            )

        return ITopAttachmentCreateResult(
            was_attempted=True,
            success=False,
            error=f"No attachment object returned by iTop: {body}"
        )

    @staticmethod
    def _map_status_to_itop(app_status: str) -> str:
        mapping = {
            "Assigned": "assigned",
            "InProgress": "assigned",
            "Resolved": "resolved",
            "Closed": "closed",
            "Rejected": "rejected"
        }
        return mapping.get(app_status, "new")
