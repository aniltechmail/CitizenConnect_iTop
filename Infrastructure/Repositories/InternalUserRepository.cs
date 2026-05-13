using Core.Entities.Identity;
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
    public class InternalUserRepository : IInternalUserRepository
    {
        private readonly AppDbContext _db;
        public InternalUserRepository(AppDbContext db) => _db = db;

        public async Task<InternalUser?> GetByIdAsync(Guid id) =>
            await _db.InternalUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

        public async Task<InternalUser?> GetByEmailAsync(string email) =>
            await _db.InternalUsers
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        public async Task<IEnumerable<InternalUser>> GetByRoleAsync(UserRole role) =>
            await _db.InternalUsers
                .Where(u => u.Role == role && u.IsActive)
                .ToListAsync();

        public async Task UpdateAsync(InternalUser user)
        {
            _db.InternalUsers.Update(user);
            await _db.SaveChangesAsync();
        }
    }
}
