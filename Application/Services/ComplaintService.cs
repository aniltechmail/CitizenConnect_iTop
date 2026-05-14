using Core.DTOs.Complaint;
using Core.DTOs.ITop;
using Core.Entities.Complaint;
using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Application.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly IComplaintRepository _complaintRepo;
        private readonly ILocationRepository _locationRepo;
        private readonly IInternalUserRepository _userRepo;
        private readonly IStorageService _storage;
        private readonly IITopTicketAdapter _itopAdapter;

        public ComplaintService(
            IComplaintRepository complaintRepo,
            ILocationRepository locationRepo,
            IInternalUserRepository userRepo,
            IStorageService storage,
            IITopTicketAdapter itopAdapter)
        {
            _complaintRepo = complaintRepo;
            _locationRepo = locationRepo;
            _userRepo = userRepo;
            _storage = storage;
            _itopAdapter = itopAdapter;
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

            await CreateITopTicketMappingAsync(created);

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
            complaint.UpdatedAt = DateTime.UtcNow;

            await _complaintRepo.UpdateAsync(complaint);

            await SyncStatusToITopAsync(
                complaint,
                ComplaintStatus.Assigned,
                $"Assigned to department ID {dto.DepartmentId}");

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
            complaint.UpdatedAt = DateTime.UtcNow;

            await _complaintRepo.UpdateAsync(complaint);

            if (complaint.Status == ComplaintStatus.InProgress)
            {
                await SyncStatusToITopAsync(
                    complaint,
                    ComplaintStatus.InProgress,
                    $"Field agent assigned: {agent.FullName}");
            }

            var updated = await _complaintRepo.GetByIdAsync(complaintId)!;
            return MapToDto(updated!);
        }

        public async Task<ComplaintResponseDto> UpdateStatusAsync(
            Guid complaintId,
            UpdateComplaintStatusDto dto,
            Guid updatedById)
        {
            var complaint = await _complaintRepo.GetByIdAsync(complaintId)
                ?? throw new KeyNotFoundException("Complaint not found.");

            ValidateStatusTransition(complaint.Status, dto.Status);

            complaint.Status = dto.Status;
            complaint.UpdatedAt = DateTime.UtcNow;

            if (dto.Status == ComplaintStatus.Resolved)
                complaint.ResolvedAt = DateTime.UtcNow;
            else if (dto.Status == ComplaintStatus.Closed)
                complaint.ClosedAt = DateTime.UtcNow;

            await _complaintRepo.UpdateAsync(complaint);

            // System message
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

            // iTop sync
            await SyncStatusToITopAsync(complaint, dto);

            var updated = await _complaintRepo.GetByIdAsync(complaintId)!;
            return MapToDto(updated!);
        }

        public async Task<ComplaintMediaResponseDto> UploadMediaAsync(Guid complaintId, IFormFile file, Guid uploadedById)
        {
            // Verify complaint exists
            var complaint = await _complaintRepo.GetByIdAsync(complaintId)
                ?? throw new KeyNotFoundException("Complaint not found.");

            ValidateUpload(file);

            var mediaType = DetermineMediaType(file.ContentType ?? string.Empty);
            var folder = $"complaints/{complaintId}";
            var fileName = Path.GetFileName(file.FileName);
            byte[] fileContent;

            await using (var buffer = new MemoryStream())
            {
                await file.CopyToAsync(buffer);
                fileContent = buffer.ToArray();
            }

            var filePath = await _storage.SaveFileAsync(
                new MemoryStream(fileContent),
                fileName,
                folder
            );

            var media = new ComplaintMedia
            {
                Id = Guid.NewGuid(),
                ComplaintId = complaintId,
                MediaType = mediaType,
                FileName = fileName,
                FilePath = filePath,
                FileSize = file.Length,
                MimeType = file.ContentType,
                CreatedAt = DateTime.UtcNow
            };

            // Save media directly — don't touch the complaint entity
            await _complaintRepo.AddMediaAsync(media);
            await SyncAttachmentToITopAsync(complaint, media, fileContent);

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

        private static void ValidateUpload(IFormFile file)
        {
            const long maxBytes = 50 * 1024 * 1024;
            if (file.Length <= 0)
                throw new InvalidOperationException("Uploaded file is empty.");
            if (file.Length > maxBytes)
                throw new InvalidOperationException("Uploaded file exceeds the 50 MB limit.");
            if (string.IsNullOrWhiteSpace(file.FileName))
                throw new InvalidOperationException("Uploaded file name is required.");
        }

        private static void ValidateStatusTransition(ComplaintStatus current, ComplaintStatus next)
        {
            var allowed = current switch
            {
                ComplaintStatus.Submitted => new[] { ComplaintStatus.Assigned, ComplaintStatus.Rejected },
                ComplaintStatus.Assigned => new[] { ComplaintStatus.InProgress, ComplaintStatus.Rejected },
                ComplaintStatus.InProgress => new[] { ComplaintStatus.Resolved, ComplaintStatus.Rejected },
                ComplaintStatus.Resolved => new[] { ComplaintStatus.Closed, ComplaintStatus.InProgress },
                _ => Array.Empty<ComplaintStatus>()
            };

            if (!allowed.Contains(next))
                throw new InvalidOperationException($"Invalid status transition from {current} to {next}.");
        }

        private async Task CreateITopTicketMappingAsync(Complaint complaint)
        {
            var result = await _itopAdapter.CreateTicketAsync(new ITopTicketCreateRequest
            {
                ComplaintId = complaint.Id,
                RefNumber = complaint.RefNumber,
                Title = complaint.Title,
                Description = complaint.Description,
                CitizenName = complaint.Citizen?.FullName ?? string.Empty,
                CitizenPhone = complaint.Citizen?.Phone ?? string.Empty,
                CategoryName = complaint.Category?.Name ?? string.Empty,
                BlockName = complaint.Block?.Name ?? string.Empty,
                Priority = complaint.Priority
            });

            if (!result.WasAttempted)
                return;

            var mapping = new ComplaintITopMapping
            {
                Id = Guid.NewGuid(),
                ComplaintId = complaint.Id,
                ITopTicketId = result.TicketId ?? string.Empty,
                ITopTicketRef = result.TicketRef ?? string.Empty,
                ITopClass = "UserRequest",
                LastSyncedAt = result.Success ? DateTime.UtcNow : null,
                SyncStatus = result.Success ? SyncStatus.Synced : SyncStatus.SyncFailed,
                LastSyncError = result.Success ? null : result.Error
            };

            await _complaintRepo.AddITopMappingAsync(mapping);
        }

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

        private async Task SyncStatusToITopAsync(
            Complaint complaint,
            UpdateComplaintStatusDto dto)
        {
            await SyncStatusToITopAsync(complaint, dto.Status, dto.Remarks);
        }

        private async Task SyncStatusToITopAsync(
            Complaint complaint,
            ComplaintStatus newStatus,
            string? remarks)
        {
            var mapping = await _complaintRepo
                .GetITopMappingByComplaintIdAsync(complaint.Id);

            if (mapping == null || mapping.SyncStatus != SyncStatus.Synced)
                return;

            var result = await _itopAdapter.UpdateTicketAsync(new ITopTicketUpdateRequest
            {
                ITopTicketId = mapping.ITopTicketId,
                ITopClass = mapping.ITopClass,
                NewStatus = newStatus.ToString(),
                Remarks = remarks,
                ComplaintRefNumber = complaint.RefNumber
            });

            mapping.LastSyncedAt = DateTime.UtcNow;
            mapping.SyncStatus = result.Success ? SyncStatus.Synced : SyncStatus.SyncFailed;
            mapping.LastSyncError = result.Success ? null : result.Error;

            await _complaintRepo.UpdateITopMappingAsync(mapping);
        }

        private async Task SyncAttachmentToITopAsync(
            Complaint complaint,
            ComplaintMedia media,
            byte[] fileContent)
        {
            var mapping = await _complaintRepo
                .GetITopMappingByComplaintIdAsync(complaint.Id);

            if (mapping == null || mapping.SyncStatus != SyncStatus.Synced)
                return;

            var result = await _itopAdapter.CreateAttachmentAsync(new ITopAttachmentCreateRequest
            {
                ITopTicketId = mapping.ITopTicketId,
                ITopClass = mapping.ITopClass,
                FileName = media.FileName,
                MimeType = media.MimeType ?? "application/octet-stream",
                Content = fileContent,
                ComplaintRefNumber = complaint.RefNumber
            });

            if (!result.WasAttempted)
                return;

            mapping.LastSyncedAt = result.Success ? DateTime.UtcNow : mapping.LastSyncedAt;
            mapping.LastSyncError = result.Success
                ? null
                : $"Attachment sync failed for {media.FileName}: {result.Error}";

            await _complaintRepo.UpdateITopMappingAsync(mapping);
        }
    }
}
