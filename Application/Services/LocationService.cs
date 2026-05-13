using Core.DTOs.Location;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class LocationService : ILocationService
    {
        private readonly ILocationRepository _repo;
        public LocationService(ILocationRepository repo) => _repo = repo;

        public async Task<IEnumerable<DistrictDto>> GetAllDistrictsAsync()
        {
            var districts = await _repo.GetAllDistrictsAsync();
            return districts.Select(d => new DistrictDto
            {
                Id = d.Id,
                Name = d.Name,
                Code = d.Code
            });
        }

        public async Task<IEnumerable<ConstituencyDto>> GetConstituenciesByDistrictAsync(int districtId)
        {
            var items = await _repo.GetConstituenciesByDistrictAsync(districtId);
            return items.Select(x => new ConstituencyDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                DistrictId = x.DistrictId
            });
        }

        public async Task<IEnumerable<AreaDto>> GetAreasByConstituencyAsync(int constituencyId)
        {
            var items = await _repo.GetAreasByConstituencyAsync(constituencyId);
            return items.Select(x => new AreaDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                ConstituencyId = x.ConstituencyId
            });
        }

        public async Task<IEnumerable<BlockDto>> GetBlocksByAreaAsync(int areaId)
        {
            var items = await _repo.GetBlocksByAreaAsync(areaId);
            return items.Select(x => new BlockDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                AreaId = x.AreaId
            });
        }
    }
}
