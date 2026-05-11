using Core.DTOs.Complaint;
using Core.Entities.Complaint;
using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly IComplaintRepository _complaintRepo;
        private readonly ILocationRepository _locationRepo;
        private readonly IInternalUserRepository _userRepo;
        private readonly IStorageService _storage;

        public ComplaintService(IComplaintRepository complaintRepo, ILocationRepository locationRepo, IInternalUserRepository userRepo, IStorageService storage)
        {
            _complaintRepo = complaintRepo;
            _locationRepo = locationRepo;
            _userRepo = userRepo;
            _storage = storage;
        }

        public async Task<ComplaintResponseDto> SubmitComplaintAsync(Guid citizenId, SubmitComplaintDto dto)
        {
            var block = await _locationRepo.GetBlockByIdAsync(dto.BlockId)
                ?? throw new KeyNotFoundException("Block not found.");

            var refNumber = await _complaintRepo.GenerateRefNumberAsync();

            var complaint = new Complaint
            {
                Id = Guid.NewGuid(),
                RefNumber = refNumber,
                Title = dto.Title,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                BlockId = dto.BlockId,
                CitizenId = citizenId,
                Priority = dto.Priority,
                Status = ComplaintStatus.Submitted,
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _complaintRepo.CreateAsync(complaint);

            // Reload with includes
            var created = await _complaintRepo.GetByIdAsync(complaint.Id)
                ?? throw new Exception("Failed to retrieve created complaint.");

            return MapToDto(created);
        }

        public async Task<ComplaintResponseDto> GetByIdAsync(Guid id)
        {
            var complaint = await _complaintRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Complaint not found.");
            return MapToDto(complaint);
        }

        public async Task<IEnumerable<ComplaintResponseDto>> GetMyComplaintsAsync(Guid citizenId)
        {
            var complaints = await _complaintRepo.GetByCitizenIdAsync(citizenId);
            return complaints.Select(MapToDto);
        }

        public async Task<PagedComplaintsDto> GetAllComplaintsAsync(ComplaintStatus? status, int? departmentId, int? blockId, int page, int pageSize)
        {
            var items = await _complaintRepo.GetAllAsync(
                status, departmentId, blockId, page, pageSize);
            var total = await _complaintRepo.GetTotalCountAsync(
                status, departmentId, blockId);

            return new PagedComplaintsDto
            {
                Items = items.Select(MapToDto),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ComplaintResponseDto> AssignDepartmentAsync(Guid complaintId, AssignComplaintDto dto, Guid assignedById)
        {
            var complaint = await _complaintRepo.GetByIdAsync(complaintId)
                ?? throw new KeyNotFoundException("Complaint not found.");

            if (complaint.Status != ComplaintStatus.Submitted)
                throw new InvalidOperationException("Only submitted complaints can be assigned.");

            complaint.AssignedDepartmentId = dto.DepartmentId;
            complaint.Status = ComplaintStatus.Assigned;
            complaint.AssignedAt = DateTime.UtcNow;

            await _complaintRepo.UpdateAsync(complaint);

            var updated = await _complaintRepo.GetByIdAsync(complaintId)!;
            return MapToDto(updated!);
        }

        public async Task<ComplaintResponseDto> AssignAgentAsync(Guid complaintId, Guid agentId, Guid assignedById)
        {
            var complaint = await _complaintRepo.GetByIdAsync(complaintId)
                ?? throw new KeyNotFoundException("Complaint not found.");

            var agent = await _userRepo.GetByIdAsync(agentId)
                ?? throw new KeyNotFoundException("Agent not found.");

            complaint.AssignedAgentId = agentId;
            if (complaint.Status == ComplaintStatus.Assigned)
                complaint.Status = ComplaintStatus.InProgress;

            await _complaintRepo.UpdateAsync(complaint);

            var updated = await _complaintRepo.GetByIdAsync(complaintId)!;
            return MapToDto(updated!);
        }

        public async Task<ComplaintResponseDto> UpdateStatusAsync(Guid complaintId, UpdateComplaintStatusDto dto, Guid updatedById)
        {
            var complaint = await _complaintRepo.GetByIdAsync(complaintId)
                ?? throw new KeyNotFoundException("Complaint not found.");

            complaint.Status = dto.Status;

            if (dto.Status == ComplaintStatus.Resolved)
                complaint.ResolvedAt = DateTime.UtcNow;
            else if (dto.Status == ComplaintStatus.Closed)
                complaint.ClosedAt = DateTime.UtcNow;

            // Save complaint status change first
            await _complaintRepo.UpdateAsync(complaint);

            // Add system message separately
            var message = new ComplaintMessage
            {
                Id = Guid.NewGuid(),
                ComplaintId = complaintId,
                SenderType = SenderType.System,
                SenderId = updatedById,
                Message = dto.Remarks ?? $"Status updated to {dto.Status}",
                CreatedAt = DateTime.UtcNow
            };
            await _complaintRepo.AddMessageAsync(message);

            var updated = await _complaintRepo.GetByIdAsync(complaintId)!;
            return MapToDto(updated!);
        }

        public async Task<ComplaintMediaResponseDto> UploadMediaAsync(Guid complaintId, IFormFile file, Guid uploadedById)
        {
            // Verify complaint exists
            var complaint = await _complaintRepo.GetByIdAsync(complaintId)
                ?? throw new KeyNotFoundException("Complaint not found.");

            var mediaType = DetermineMediaType(file.ContentType);
            var folder = $"complaints/{complaintId}";

            var filePath = await _storage.SaveFileAsync(
                file.OpenReadStream(),
                file.FileName,
                folder
            );

            var media = new ComplaintMedia
            {
                Id = Guid.NewGuid(),
                ComplaintId = complaintId,
                MediaType = mediaType,
                FileName = file.FileName,
                FilePath = filePath,
                FileSize = file.Length,
                MimeType = file.ContentType,
                CreatedAt = DateTime.UtcNow
            };

            // Save media directly — don't touch the complaint entity
            await _complaintRepo.AddMediaAsync(media);

            return new ComplaintMediaResponseDto
            {
                Id = media.Id,
                MediaType = media.MediaType.ToString(),
                FileName = media.FileName,
                FileUrl = _storage.GetFileUrl(media.FilePath),
                FileSize = media.FileSize,
                CreatedAt = media.CreatedAt
            };
        }

        // ── Helpers ────────────────────────────────────────────────

        private MediaType DetermineMediaType(string contentType) =>
            contentType switch
            {
                var ct when ct.StartsWith("image/") => MediaType.Image,
                var ct when ct.StartsWith("video/") => MediaType.Video,
                var ct when ct.StartsWith("audio/") => MediaType.Voice,
                _ => MediaType.Document
            };

        private ComplaintResponseDto MapToDto(Complaint c) => new()
        {
            Id = c.Id,
            RefNumber = c.RefNumber,
            Title = c.Title,
            Description = c.Description,
            Status = c.Status.ToString(),
            Priority = c.Priority,
            CreatedAt = c.CreatedAt,
            SubmittedAt = c.SubmittedAt,
            AssignedAt = c.AssignedAt,
            ResolvedAt = c.ResolvedAt,
            CitizenId = c.CitizenId,
            CitizenName = c.Citizen?.FullName ?? string.Empty,
            CitizenPhone = c.Citizen?.Phone ?? string.Empty,
            BlockId = c.BlockId,
            BlockName = c.Block?.Name ?? string.Empty,
            CategoryId = c.CategoryId,
            CategoryName = c.Category?.Name ?? string.Empty,
            AssignedDepartmentId = c.AssignedDepartmentId,
            AssignedDepartmentName = c.AssignedDepartment?.Name,
            AssignedAgentId = c.AssignedAgentId,
            AssignedAgentName = c.AssignedAgent?.FullName,
            Media = c.Media.Select(m => new ComplaintMediaResponseDto
            {
                Id = m.Id,
                MediaType = m.MediaType.ToString(),
                FileName = m.FileName,
                FileUrl = _storage.GetFileUrl(m.FilePath),
                FileSize = m.FileSize,
                CreatedAt = m.CreatedAt
            }).ToList()
        };
    }
}
