using Core.DTOs.Complaint;
using Core.Enums;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComplaintController : ControllerBase
    {
        private readonly IComplaintService _complaintService;
        private readonly IEscalationService _escalationService;
        public ComplaintController(
            IComplaintService complaintService,
            IEscalationService escalationService)
        {
            _complaintService = complaintService;
            _escalationService = escalationService;
        }

        // ── Citizen Endpoints ──────────────────────────────────────

        [HttpPost]
        public async Task<IActionResult> SubmitComplaint(
            [FromBody] SubmitComplaintDto dto)
        {
            try
            {
                if (!IsCitizen())
                    return Forbid();
                var citizenId = GetUserId();
                var result = await _complaintService.SubmitComplaintAsync(citizenId, dto);
                return CreatedAtAction(nameof(GetById),
                    new { id = result.Id }, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var result = await _complaintService.GetByIdAsync(id);
                if (!CanAccessComplaint(result.CitizenId))
                    return Forbid();
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyComplaints()
        {
            if (!IsCitizen())
                return Forbid();
            var citizenId = GetUserId();
            var result = await _complaintService.GetMyComplaintsAsync(citizenId);
            return Ok(result);
        }

        [HttpPost("{id:guid}/media")]
        public async Task<IActionResult> UploadMedia(
            Guid id,
            IFormFile file)
        {
            try
            {
                var complaint = await _complaintService.GetByIdAsync(id);
                if (!CanAccessComplaint(complaint.CitizenId))
                    return Forbid();
                var userId = GetUserId();
                var result = await _complaintService.UploadMediaAsync(id, file, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/messages")]
        public async Task<IActionResult> SendMessage(
            Guid id,
            [FromBody] SendComplaintMessageDto dto)
        {
            try
            {
                var complaint = await _complaintService.GetByIdAsync(id);
                if (!CanAccessComplaint(complaint.CitizenId))
                    return Forbid();

                var result = await _complaintService.SendMessageAsync(
                    id,
                    dto,
                    GetUserId(),
                    IsInternalUser());
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:guid}/messages")]
        public async Task<IActionResult> GetMessages(Guid id)
        {
            try
            {
                var complaint = await _complaintService.GetByIdAsync(id);
                if (!CanAccessComplaint(complaint.CitizenId))
                    return Forbid();

                var result = await _complaintService.GetMessagesAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}/messages/{msgId:guid}/read")]
        public async Task<IActionResult> MarkMessageAsRead(Guid id, Guid msgId)
        {
            try
            {
                var complaint = await _complaintService.GetByIdAsync(id);
                if (!CanAccessComplaint(complaint.CitizenId))
                    return Forbid();

                var result = await _complaintService.MarkMessageAsReadAsync(id, msgId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/feedback")]
        public async Task<IActionResult> SubmitFeedback(
            Guid id,
            [FromBody] SubmitFeedbackDto dto)
        {
            try
            {
                var complaint = await _complaintService.GetByIdAsync(id);
                if (!CanAccessComplaint(complaint.CitizenId))
                    return Forbid();

                var result = await _complaintService.SubmitFeedbackAsync(
                    id,
                    dto,
                    GetUserId(),
                    IsInternalUser());
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id:guid}/feedback")]
        public async Task<IActionResult> GetFeedback(Guid id)
        {
            try
            {
                var complaint = await _complaintService.GetByIdAsync(id);
                if (!CanAccessComplaint(complaint.CitizenId))
                    return Forbid();

                var result = await _complaintService.GetFeedbackAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{id:guid}/escalations")]
        public async Task<IActionResult> GetEscalations(Guid id)
        {
            try
            {
                var complaint = await _complaintService.GetByIdAsync(id);
                if (!CanAccessComplaint(complaint.CitizenId))
                    return Forbid();

                var result = await _escalationService.GetByComplaintAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // ── Internal User Endpoints ────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Admin,Assigner,Supervisor,FieldAgent")]
        public async Task<IActionResult> GetAll(
            [FromQuery] ComplaintStatus? status,
            [FromQuery] int? departmentId,
            [FromQuery] int? blockId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _complaintService.GetAllComplaintsAsync(
                status, departmentId, blockId, page, pageSize);
            return Ok(result);
        }

        [HttpPut("{id:guid}/assign-department")]
        [Authorize(Roles = "Admin,Assigner")]
        public async Task<IActionResult> AssignDepartment(
            Guid id,
            [FromBody] AssignComplaintDto dto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _complaintService.AssignDepartmentAsync(
                    id, dto, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}/assign-agent/{agentId:guid}")]
        [Authorize(Roles = "Admin,Assigner")]
        public async Task<IActionResult> AssignAgent(Guid id, Guid agentId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _complaintService.AssignAgentAsync(
                    id, agentId, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}/status")]
        [Authorize(Roles = "Admin,Assigner,FieldAgent,Supervisor")]
        public async Task<IActionResult> UpdateStatus(
            Guid id,
            [FromBody] UpdateComplaintStatusDto dto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _complaintService.UpdateStatusAsync(
                    id, dto, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── Helper ─────────────────────────────────────────────────

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string? GetUserType() =>
            User.FindFirstValue("user_type");

        private bool IsInternalUser() =>
            GetUserType() == "internal";

        private bool IsCitizen() =>
            GetUserType() == "citizen";

        private bool CanAccessComplaint(Guid citizenId) =>
            IsInternalUser() || citizenId == GetUserId();
    }
}
