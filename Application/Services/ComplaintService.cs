using Core.DTOs.Complaint;
using Core.DTOs.ITop;
using Core.Entities.Complaint;
using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace Application.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly IComplaintRepository _complaintRepo;
        private readonly ILocationRepository _locationRepo;
        private readonly IInternalUserRepository _userRepo;
        private readonly INotificationRepository _notificationRepo;
        private readonly IEscalationService _escalationService;
        private readonly IStorageService _storage;
        private readonly IITopTicketAdapter _itopAdapter;
        private readonly IPushNotificationService _pushNotificationService;

        public ComplaintService(
            IComplaintRepository complaintRepo,
            ILocationRepository locationRepo,
            IInternalUserRepository userRepo,
            INotificationRepository notificationRepo,
            IEscalationService escalationService,
            IStorageService storage,
            IITopTicketAdapter itopAdapter,
            IPushNotificationService pushNotificationService)
        {
            _complaintRepo = complaintRepo;
            _locationRepo = locationRepo;
            _userRepo = userRepo;
            _notificationRepo = notificationRepo;
            _escalationService = escalationService;
            _storage = storage;
            _itopAdapter = itopAdapter;
            _pushNotificationService = pushNotificationService;
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
            await NotifyAssignersAsync(created);

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
            await NotifyDepartmentAssignedAsync(complaint);

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
                await NotifyCitizenAsync(
                    complaint,
                    NotificationType.ComplaintInProgress,
                    $"Your complaint {complaint.RefNumber} is now in progress.");
            }
            await NotifyUserAsync(
                SenderType.Agent,
                agent.Id,
                complaint.Id,
                NotificationType.ComplaintAssigned,
                $"Complaint {complaint.RefNumber} has been assigned to you.");

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
            await NotifyStatusChangedAsync(complaint, dto.Status);
            if (dto.Status == ComplaintStatus.Resolved || dto.Status == ComplaintStatus.Closed)
                await _escalationService.ResolveActiveForComplaintAsync(complaint.Id);

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

        public async Task<ComplaintMessageResponseDto> SendMessageAsync(
            Guid complaintId,
            SendComplaintMessageDto dto,
            Guid senderId,
            bool isInternalUser)
        {
            var complaint = await _complaintRepo.GetByIdAsync(complaintId)
                ?? throw new KeyNotFoundException("Complaint not found.");

            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new InvalidOperationException("Message is required.");

            var senderType = ResolveSenderType(dto.SenderType, isInternalUser);
            var message = new ComplaintMessage
            {
                Id = Guid.NewGuid(),
                ComplaintId = complaintId,
                SenderType = senderType,
                SenderId = senderId,
                Message = dto.Message.Trim(),
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _complaintRepo.AddMessageAsync(message);
            await SyncMessageToITopAsync(complaint, message);
            await NotifyMessageRecipientsAsync(complaint, message);
            return MapMessageToDto(message);
        }

        public async Task<IEnumerable<ComplaintMessageResponseDto>> GetMessagesAsync(Guid complaintId)
        {
            _ = await _complaintRepo.GetByIdAsync(complaintId)
                ?? throw new KeyNotFoundException("Complaint not found.");

            var messages = await _complaintRepo.GetMessagesAsync(complaintId);
            return messages.Select(MapMessageToDto);
        }

        public async Task<ComplaintMessageResponseDto> MarkMessageAsReadAsync(
            Guid complaintId,
            Guid messageId)
        {
            var message = await _complaintRepo.GetMessageByIdAsync(complaintId, messageId)
                ?? throw new KeyNotFoundException("Message not found.");

            message.IsRead = true;
            await _complaintRepo.UpdateMessageAsync(message);
            return MapMessageToDto(message);
        }

        public async Task<ComplaintFeedbackResponseDto> SubmitFeedbackAsync(
            Guid complaintId,
            SubmitFeedbackDto dto,
            Guid submittedById,
            bool isInternalUser)
        {
            var complaint = await _complaintRepo.GetByIdAsync(complaintId)
                ?? throw new KeyNotFoundException("Complaint not found.");

            if (complaint.Status != ComplaintStatus.Resolved &&
                complaint.Status != ComplaintStatus.Closed)
                throw new InvalidOperationException("Feedback can be submitted only after resolution.");

            if (dto.Rating < 1 || dto.Rating > 5)
                throw new InvalidOperationException("Rating must be between 1 and 5.");

            var existing = await _complaintRepo.GetFeedbackAsync(complaintId);
            if (existing != null)
                throw new InvalidOperationException("Feedback already exists for this complaint.");

            var feedback = new ComplaintFeedback
            {
                Id = Guid.NewGuid(),
                ComplaintId = complaintId,
                CitizenId = complaint.CitizenId,
                Rating = dto.Rating,
                Comments = dto.Comments,
                CollectedById = isInternalUser ? submittedById : null,
                CreatedAt = DateTime.UtcNow
            };

            await _complaintRepo.AddFeedbackAsync(feedback);
            return MapFeedbackToDto(feedback);
        }

        public async Task<ComplaintFeedbackResponseDto> GetFeedbackAsync(Guid complaintId)
        {
            _ = await _complaintRepo.GetByIdAsync(complaintId)
                ?? throw new KeyNotFoundException("Complaint not found.");

            var feedback = await _complaintRepo.GetFeedbackAsync(complaintId)
                ?? throw new KeyNotFoundException("Feedback not found.");

            return MapFeedbackToDto(feedback);
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

        private static SenderType ResolveSenderType(SenderType? requested, bool isInternalUser)
        {
            if (!isInternalUser)
            {
                if (requested.HasValue && requested.Value != SenderType.Citizen)
                    throw new InvalidOperationException("Citizens can send only citizen messages.");
                return SenderType.Citizen;
            }

            if (requested == SenderType.System)
                return SenderType.System;

            return SenderType.Agent;
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

        private static ComplaintMessageResponseDto MapMessageToDto(ComplaintMessage m) => new()
        {
            Id = m.Id,
            ComplaintId = m.ComplaintId,
            SenderType = m.SenderType.ToString(),
            SenderId = m.SenderId,
            Message = m.Message,
            IsRead = m.IsRead,
            CreatedAt = m.CreatedAt
        };

        private static ComplaintFeedbackResponseDto MapFeedbackToDto(ComplaintFeedback f) => new()
        {
            Id = f.Id,
            ComplaintId = f.ComplaintId,
            CitizenId = f.CitizenId,
            Rating = f.Rating,
            Comments = f.Comments,
            CollectedById = f.CollectedById,
            CreatedAt = f.CreatedAt
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

        private async Task SyncMessageToITopAsync(Complaint complaint, ComplaintMessage message)
        {
            var mapping = await _complaintRepo
                .GetITopMappingByComplaintIdAsync(complaint.Id);

            if (mapping == null || mapping.SyncStatus != SyncStatus.Synced)
                return;

            var result = await _itopAdapter.AddTicketLogAsync(new ITopTicketLogRequest
            {
                ITopTicketId = mapping.ITopTicketId,
                ITopClass = mapping.ITopClass,
                ComplaintRefNumber = complaint.RefNumber,
                Message = FormatITopMessageLog(message),
                IsPrivate = message.SenderType == SenderType.System
            });

            if (!result.WasAttempted)
                return;

            mapping.LastSyncedAt = result.Success ? DateTime.UtcNow : mapping.LastSyncedAt;
            mapping.LastSyncError = result.Success
                ? null
                : $"Message sync failed for {message.Id}: {result.Error}";

            await _complaintRepo.UpdateITopMappingAsync(mapping);
        }

        private static string FormatITopMessageLog(ComplaintMessage message)
        {
            var sender = message.SenderType switch
            {
                SenderType.Citizen => "Citizen",
                SenderType.Agent => "Agent",
                SenderType.System => "System",
                _ => "Unknown"
            };

            return $"[{sender}] {message.Message}";
        }

        private async Task NotifyAssignersAsync(Complaint complaint)
        {
            var assigners = await _userRepo.GetByRoleAsync(UserRole.Assigner);
            await NotifyUsersAsync(
                assigners.Select(u => u.Id),
                SenderType.Agent,
                complaint.Id,
                NotificationType.ComplaintSubmitted,
                $"New complaint {complaint.RefNumber} has been submitted.");
        }

        private async Task NotifyDepartmentAssignedAsync(Complaint complaint)
        {
            await NotifyCitizenAsync(
                complaint,
                NotificationType.ComplaintAssigned,
                $"Your complaint {complaint.RefNumber} has been assigned to a department.");

            if (complaint.AssignedDepartmentId.HasValue)
            {
                var agents = await _userRepo.GetByRoleAndDepartmentAsync(
                    UserRole.FieldAgent,
                    complaint.AssignedDepartmentId.Value);

                await NotifyUsersAsync(
                    agents.Select(a => a.Id),
                    SenderType.Agent,
                    complaint.Id,
                    NotificationType.ComplaintAssigned,
                    $"Complaint {complaint.RefNumber} has been assigned to your department.");
            }
        }

        private async Task NotifyStatusChangedAsync(Complaint complaint, ComplaintStatus status)
        {
            if (status == ComplaintStatus.InProgress)
            {
                await NotifyCitizenAsync(
                    complaint,
                    NotificationType.ComplaintInProgress,
                    $"Your complaint {complaint.RefNumber} is now in progress.");
            }
            else if (status == ComplaintStatus.Resolved)
            {
                await NotifyCitizenAsync(
                    complaint,
                    NotificationType.ComplaintResolved,
                    $"Your complaint {complaint.RefNumber} has been resolved. Please share feedback.");
            }
        }

        private async Task NotifyMessageRecipientsAsync(Complaint complaint, ComplaintMessage message)
        {
            if (message.SenderType == SenderType.Citizen)
            {
                if (complaint.AssignedAgentId.HasValue)
                {
                    await NotifyUserAsync(
                        SenderType.Agent,
                        complaint.AssignedAgentId.Value,
                        complaint.Id,
                        NotificationType.MessageReceived,
                        $"New citizen message on complaint {complaint.RefNumber}.");
                    return;
                }

                var assigners = await _userRepo.GetByRoleAsync(UserRole.Assigner);
                await NotifyUsersAsync(
                    assigners.Select(u => u.Id),
                    SenderType.Agent,
                    complaint.Id,
                    NotificationType.MessageReceived,
                    $"New citizen message on complaint {complaint.RefNumber}.");
            }
            else
            {
                await NotifyCitizenAsync(
                    complaint,
                    NotificationType.MessageReceived,
                    $"New message on your complaint {complaint.RefNumber}.");
            }
        }

        private async Task NotifyCitizenAsync(
            Complaint complaint,
            NotificationType type,
            string message)
        {
            await NotifyUserAsync(SenderType.Citizen, complaint.CitizenId, complaint.Id, type, message);
        }

        private async Task NotifyUserAsync(
            SenderType userType,
            Guid userId,
            Guid complaintId,
            NotificationType type,
            string message)
        {
            await _notificationRepo.AddAsync(new Core.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserType = userType,
                UserId = userId,
                ComplaintId = complaintId,
                Type = type,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _pushNotificationService.SendAsync(userType, userId, "Citizen Connect", message, complaintId);
        }

        private async Task NotifyUsersAsync(
            IEnumerable<Guid> userIds,
            SenderType userType,
            Guid complaintId,
            NotificationType type,
            string message)
        {
            var notifications = userIds.Distinct().Select(userId => new Core.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserType = userType,
                UserId = userId,
                ComplaintId = complaintId,
                Type = type,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            if (notifications.Count > 0)
            {
                await _notificationRepo.AddRangeAsync(notifications);
                await _pushNotificationService.SendManyAsync(userType, notifications.Select(x => x.UserId), "Citizen Connect", message, complaintId);
            }
        }
    }
}
