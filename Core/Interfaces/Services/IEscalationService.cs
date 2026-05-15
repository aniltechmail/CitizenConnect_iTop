using Core.DTOs.Escalation;

namespace Core.Interfaces.Services
{
    public interface IEscalationService
    {
        Task<int> ScanAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<EscalationEventResponseDto>> GetByComplaintAsync(Guid complaintId);
        Task<IEnumerable<EscalationEventResponseDto>> GetActiveAsync();
        Task ResolveActiveForComplaintAsync(Guid complaintId);
    }
}
