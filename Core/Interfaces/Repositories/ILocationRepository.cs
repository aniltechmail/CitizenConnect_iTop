using Core.Entities.Location;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.Repositories
{
    public interface ILocationRepository
    {
        Task<IEnumerable<District>> GetAllDistrictsAsync();
        Task<District?> GetDistrictByIdAsync(int id);
        Task<IEnumerable<Constituency>> GetConstituenciesByDistrictAsync(int districtId);
        Task<IEnumerable<Area>> GetAreasByConstituencyAsync(int constituencyId);
        Task<IEnumerable<Block>> GetBlocksByAreaAsync(int areaId);
        Task<Block?> GetBlockByIdAsync(int id);
    }
}
