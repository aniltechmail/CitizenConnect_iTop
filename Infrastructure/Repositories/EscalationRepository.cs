using Core.Entities;
using Core.Entities.Complaint;
using Core.Enums;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class EscalationRepository : IEscalationRepository
    {
        private readonly AppDbContext _db;
        public EscalationRepository(AppDbContext db) => _db = db;

        public async Task<IEnumerable<Complaint>> GetOpenComplaintsAsync() =>
            await _db.Complaints
                .Include(c => c.ITopMapping)
                .Where(c => c.Status != ComplaintStatus.Resolved &&
                            c.Status != ComplaintStatus.Closed &&
                            c.Status != ComplaintStatus.Rejected)
                .ToListAsync();

        public async Task<SlaPolicy?> GetActivePolicyByCategoryAsync(int categoryId) =>
            await _db.SlaPolicies
                .FirstOrDefaultAsync(p => p.CategoryId == categoryId && p.IsActive);

        public async Task<bool> HasEscalationAsync(Guid complaintId, int level) =>
            await _db.EscalationEvents
                .AnyAsync(e => e.ComplaintId == complaintId && e.Level == level);

        public async Task AddEscalationAsync(EscalationEvent escalationEvent)
        {
            _db.EscalationEvents.Add(escalationEvent);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<EscalationEvent>> GetByComplaintAsync(Guid complaintId) =>
            await _db.EscalationEvents
                .Include(e => e.Complaint)
                .Where(e => e.ComplaintId == complaintId)
                .OrderBy(e => e.TriggeredAt)
                .ToListAsync();

        public async Task<IEnumerable<EscalationEvent>> GetActiveAsync() =>
            await _db.EscalationEvents
                .Include(e => e.Complaint)
                .Where(e => e.ResolvedAt == null)
                .OrderByDescending(e => e.TriggeredAt)
                .ToListAsync();

        public async Task ResolveActiveForComplaintAsync(Guid complaintId)
        {
            var active = await _db.EscalationEvents
                .Where(e => e.ComplaintId == complaintId && e.ResolvedAt == null)
                .ToListAsync();

            foreach (var item in active)
                item.ResolvedAt = DateTime.UtcNow;

            if (active.Count > 0)
                await _db.SaveChangesAsync();
        }
    }
}
