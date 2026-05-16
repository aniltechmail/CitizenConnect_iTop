using Core.Entities;
using Core.Entities.Complaint;

namespace Core.Interfaces.Repositories
{
    public interface IEscalationRepository
    {
        Task<IEnumerable<Complaint>> GetOpenComplaintsAsync();
        Task<SlaPolicy?> GetActivePolicyByCategoryAsync(int categoryId);
        Task<bool> HasEscalationAsync(Guid complaintId, int level);
        Task AddEscalationAsync(EscalationEvent escalationEvent);
        Task<IEnumerable<EscalationEvent>> GetByComplaintAsync(Guid complaintId);
        Task<IEnumerable<EscalationEvent>> GetActiveAsync();
        Task ResolveActiveForComplaintAsync(Guid complaintId);
    }
}
