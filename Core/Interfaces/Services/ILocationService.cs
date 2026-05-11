using Core.DTOs.Location;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.Services
{
    public interface ILocationService
    {
        Task<IEnumerable<DistrictDto>> GetAllDistrictsAsync();
        Task<IEnumerable<ConstituencyDto>> GetConstituenciesByDistrictAsync(int districtId);
        Task<IEnumerable<AreaDto>> GetAreasByConstituencyAsync(int constituencyId);
        Task<IEnumerable<BlockDto>> GetBlocksByAreaAsync(int areaId);
    }
}
