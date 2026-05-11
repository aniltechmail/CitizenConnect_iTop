using Core.Entities.Identity;
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
    public class CitizenRepository : ICitizenRepository
    {
        private readonly AppDbContext _db;
        public CitizenRepository(AppDbContext db) => _db = db;

        public async Task<Citizen?> GetByPhoneAsync(string phone) =>
            await _db.Citizens.Include(x => x.Block)
                .FirstOrDefaultAsync(x => x.Phone == phone);

        public async Task<Citizen?> GetByIdAsync(Guid id) =>
            await _db.Citizens.Include(x => x.Block)
                .FirstOrDefaultAsync(x => x.Id == id);

        public async Task<bool> PhoneExistsAsync(string phone) =>
            await _db.Citizens.AnyAsync(x => x.Phone == phone);

        public async Task<Citizen> CreateAsync(Citizen citizen)
        {
            _db.Citizens.Add(citizen);
            await _db.SaveChangesAsync();
            return citizen;
        }

        public async Task UpdateAsync(Citizen citizen)
        {
            _db.Citizens.Update(citizen);
            await _db.SaveChangesAsync();
        }
    }
}
