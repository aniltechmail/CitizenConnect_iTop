using Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;
        public LocationController(ILocationService locationService) =>
            _locationService = locationService;

        [HttpGet("districts")]
        public async Task<IActionResult> GetDistricts() =>
            Ok(await _locationService.GetAllDistrictsAsync());

        [HttpGet("districts/{districtId}/constituencies")]
        public async Task<IActionResult> GetConstituencies(int districtId) =>
            Ok(await _locationService.GetConstituenciesByDistrictAsync(districtId));

        [HttpGet("constituencies/{constituencyId}/areas")]
        public async Task<IActionResult> GetAreas(int constituencyId) =>
            Ok(await _locationService.GetAreasByConstituencyAsync(constituencyId));

        [HttpGet("areas/{areaId}/blocks")]
        public async Task<IActionResult> GetBlocks(int areaId) =>
            Ok(await _locationService.GetBlocksByAreaAsync(areaId));
    }
}
