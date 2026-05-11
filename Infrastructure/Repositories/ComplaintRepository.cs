using Core.Entities.Complaint;
using Core.Enums;
using Core.Interfaces.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ComplaintRepository : IComplaintRepository
    {
        private readonly AppDbContext _db;
        public ComplaintRepository(AppDbContext db) => _db = db;

        public async Task<Complaint?> GetByIdAsync(Guid id) =>
            await _db.Complaints
                .Include(c => c.Citizen)
                .Include(c => c.Block)
                .Include(c => c.Category)
                .Include(c => c.AssignedDepartment)
                .Include(c => c.AssignedAgent)
                .Include(c => c.Media)
                .FirstOrDefaultAsync(c => c.Id == id);

        public async Task<Complaint?> GetByRefNumberAsync(string refNumber) =>
            await _db.Complaints
                .Include(c => c.Citizen)
                .Include(c => c.Category)
                .FirstOrDefaultAsync(c => c.RefNumber == refNumber);

        public async Task<IEnumerable<Complaint>> GetByCitizenIdAsync(Guid citizenId) =>
            await _db.Complaints
                .Include(c => c.Category)
                .Include(c => c.AssignedDepartment)
                .Include(c => c.Media)
                .Where(c => c.CitizenId == citizenId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Complaint>> GetAllAsync(ComplaintStatus? status, int? departmentId, int? blockId, int page, int pageSize)
        {
            var query = BuildFilterQuery(status, departmentId, blockId);
            return await query
                .Include(c => c.Citizen)
                .Include(c => c.Category)
                .Include(c => c.AssignedDepartment)
                .Include(c => c.Block)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(ComplaintStatus? status, int? departmentId, int? blockId) =>
            await BuildFilterQuery(status, departmentId, blockId).CountAsync();

        public async Task<Complaint> CreateAsync(Complaint complaint)
        {
            _db.Complaints.Add(complaint);
            await _db.SaveChangesAsync();
            return complaint;
        }

        public async Task UpdateAsync(Complaint complaint)
        {
            // Use Entry to only mark scalar properties as modified
            // not the entire object graph
            var entry = _db.Entry(complaint);
            if (entry.State == EntityState.Detached)
            {
                _db.Complaints.Attach(complaint);
            }
            entry.State = EntityState.Modified;

            // Don't mark navigation properties as modified
            entry.Reference(c => c.Citizen).IsModified = false;
            entry.Reference(c => c.Block).IsModified = false;
            entry.Reference(c => c.Category).IsModified = false;
            entry.Reference(c => c.AssignedDepartment).IsModified = false;
            entry.Reference(c => c.AssignedAgent).IsModified = false;

            await _db.SaveChangesAsync();
        }

        public async Task AddMediaAsync(ComplaintMedia media)
        {
            _db.ComplaintMedia.Add(media);
            await _db.SaveChangesAsync();
        }

        public async Task AddMessageAsync(ComplaintMessage message)
        {
            _db.ComplaintMessages.Add(message);
            await _db.SaveChangesAsync();
        }

        public async Task<string> GenerateRefNumberAsync()
        {
            var today = DateTime.UtcNow;
            var prefix = $"CC{today:yyyyMMdd}";
            var count = await _db.Complaints
                .CountAsync(c => c.RefNumber.StartsWith(prefix));
            return $"{prefix}{(count + 1):D4}";
        }

        private IQueryable<Complaint> BuildFilterQuery(ComplaintStatus? status, int? departmentId, int? blockId)
        {
            var query = _db.Complaints.AsQueryable();
            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);
            if (departmentId.HasValue)
                query = query.Where(c => c.AssignedDepartmentId == departmentId.Value);
            if (blockId.HasValue)
                query = query.Where(c => c.BlockId == blockId.Value);
            return query;
        }
    }
}
