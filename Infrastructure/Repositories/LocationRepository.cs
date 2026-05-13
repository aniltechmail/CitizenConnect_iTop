using Core.Entities.Location;
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
    public class LocationRepository : ILocationRepository
    {
        private readonly AppDbContext _db;
        public LocationRepository(AppDbContext db) => _db = db;

        public async Task<IEnumerable<District>> GetAllDistrictsAsync() =>
            await _db.Districts.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();

        public async Task<District?> GetDistrictByIdAsync(int id) =>
            await _db.Districts.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

        public async Task<IEnumerable<Constituency>> GetConstituenciesByDistrictAsync(int districtId) =>
            await _db.Constituencies
                .Where(x => x.DistrictId == districtId && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

        public async Task<IEnumerable<Area>> GetAreasByConstituencyAsync(int constituencyId) =>
            await _db.Areas
                .Where(x => x.ConstituencyId == constituencyId && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

        public async Task<IEnumerable<Block>> GetBlocksByAreaAsync(int areaId) =>
            await _db.Blocks
                .Where(x => x.AreaId == areaId && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

        public async Task<Block?> GetBlockByIdAsync(int id) =>
            await _db.Blocks.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
    }
}
