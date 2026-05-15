using Core.DTOs.Escalation;
using Core.DTOs.ITop;
using Core.Entities;
using Core.Entities.Complaint;
using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Application.Services
{
    public class EscalationService : IEscalationService
    {
        private readonly IEscalationRepository _escalationRepo;
        private readonly IComplaintRepository _complaintRepo;
        private readonly IInternalUserRepository _userRepo;
        private readonly INotificationRepository _notificationRepo;
        private readonly IITopTicketAdapter _itopAdapter;

        public EscalationService(
            IEscalationRepository escalationRepo,
            IComplaintRepository complaintRepo,
            IInternalUserRepository userRepo,
            INotificationRepository notificationRepo,
            IITopTicketAdapter itopAdapter)
        {
            _escalationRepo = escalationRepo;
            _complaintRepo = complaintRepo;
            _userRepo = userRepo;
            _notificationRepo = notificationRepo;
            _itopAdapter = itopAdapter;
        }

        public async Task<int> ScanAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var created = 0;
            var complaints = await _escalationRepo.GetOpenComplaintsAsync();

            foreach (var complaint in complaints)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var policy = await _escalationRepo.GetActivePolicyByCategoryAsync(complaint.CategoryId);
                if (policy == null)
                    continue;

                var startedAt = complaint.SubmittedAt ?? complaint.CreatedAt;
                var ageHours = (now - startedAt).TotalHours;

                if (complaint.AssignedAt == null && ageHours >= policy.EscalationLevel1Hours)
                    created += await CreateEscalationIfNeededAsync(complaint, 1, "Complaint has not been assigned within Level 1 escalation threshold.");

                if (complaint.ResolvedAt == null && ageHours >= policy.EscalationLevel2Hours)
                    created += await CreateEscalationIfNeededAsync(complaint, 2, "Complaint has not been resolved within Level 2 escalation threshold.");

                if (complaint.ResolvedAt == null && ageHours >= policy.EscalationLevel3Hours)
                    created += await CreateEscalationIfNeededAsync(complaint, 3, "Complaint is still unresolved within Level 3 escalation threshold.");

                var responseBreached = complaint.AssignedAt == null && ageHours >= policy.ResponseHours;
                var resolutionBreached = complaint.ResolvedAt == null && ageHours >= policy.ResolutionHours;
                if ((responseBreached || resolutionBreached) && !complaint.IsSlaBreached)
                {
                    complaint.IsSlaBreached = true;
                    complaint.UpdatedAt = DateTime.UtcNow;
                    await _complaintRepo.UpdateAsync(complaint);
                }
            }

            return created;
        }

        public async Task<IEnumerable<EscalationEventResponseDto>> GetByComplaintAsync(Guid complaintId)
        {
            var events = await _escalationRepo.GetByComplaintAsync(complaintId);
            return events.Select(MapToDto);
        }

        public async Task<IEnumerable<EscalationEventResponseDto>> GetActiveAsync()
        {
            var events = await _escalationRepo.GetActiveAsync();
            return events.Select(MapToDto);
        }

        public async Task ResolveActiveForComplaintAsync(Guid complaintId)
        {
            await _escalationRepo.ResolveActiveForComplaintAsync(complaintId);
        }

        private async Task<int> CreateEscalationIfNeededAsync(
            Complaint complaint,
            int level,
            string reason)
        {
            if (await _escalationRepo.HasEscalationAsync(complaint.Id, level))
                return 0;

            var escalation = new EscalationEvent
            {
                Id = Guid.NewGuid(),
                ComplaintId = complaint.Id,
                Level = level,
                Reason = reason,
                TriggeredAt = DateTime.UtcNow
            };

            await _escalationRepo.AddEscalationAsync(escalation);
            complaint.IsSlaBreached = true;
            complaint.UpdatedAt = DateTime.UtcNow;
            await _complaintRepo.UpdateAsync(complaint);

            await NotifyEscalationAsync(complaint, level, reason);
            await SyncEscalationToITopAsync(complaint, level, reason);

            return 1;
        }

        private async Task NotifyEscalationAsync(Complaint complaint, int level, string reason)
        {
            var recipients = new List<Guid>();

            if (level == 1)
            {
                recipients.AddRange((await _userRepo.GetByRoleAsync(UserRole.Supervisor)).Select(u => u.Id));
            }
            else if (level == 2)
            {
                recipients.AddRange((await _userRepo.GetByRoleAsync(UserRole.Admin)).Select(u => u.Id));
                if (complaint.AssignedDepartmentId.HasValue)
                {
                    recipients.AddRange((await _userRepo.GetByRoleAndDepartmentAsync(
                        UserRole.DepartmentHead,
                        complaint.AssignedDepartmentId.Value)).Select(u => u.Id));
                }
            }
            else
            {
                recipients.AddRange((await _userRepo.GetByRoleAsync(UserRole.TopManagement)).Select(u => u.Id));
            }

            var notifications = recipients.Distinct().Select(userId => new Notification
            {
                Id = Guid.NewGuid(),
                UserType = SenderType.Agent,
                UserId = userId,
                ComplaintId = complaint.Id,
                Type = NotificationType.EscalationTriggered,
                Message = $"Level {level} escalation triggered for complaint {complaint.RefNumber}: {reason}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            if (notifications.Count > 0)
                await _notificationRepo.AddRangeAsync(notifications);
        }

        private async Task SyncEscalationToITopAsync(Complaint complaint, int level, string reason)
        {
            var mapping = complaint.ITopMapping;
            if (mapping == null || mapping.SyncStatus != SyncStatus.Synced)
                return;

            var result = await _itopAdapter.AddPrivateLogAsync(new ITopTicketLogRequest
            {
                ITopTicketId = mapping.ITopTicketId,
                ITopClass = mapping.ITopClass,
                ComplaintRefNumber = complaint.RefNumber,
                Message = $"[Escalation Level {level}] {reason}",
                IsPrivate = true
            });

            mapping.LastSyncedAt = result.Success ? DateTime.UtcNow : mapping.LastSyncedAt;
            mapping.LastSyncError = result.Success ? null : $"Escalation sync failed: {result.Error}";
            await _complaintRepo.UpdateITopMappingAsync(mapping);
        }

        private static EscalationEventResponseDto MapToDto(EscalationEvent e) => new()
        {
            Id = e.Id,
            ComplaintId = e.ComplaintId,
            ComplaintRefNumber = e.Complaint?.RefNumber ?? string.Empty,
            Level = e.Level,
            Reason = e.Reason,
            TriggeredAt = e.TriggeredAt,
            ResolvedAt = e.ResolvedAt
        };
    }
}
