using Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/escalations")]
    [Authorize(Roles = "Admin,Supervisor")]
    public class EscalationsController : ControllerBase
    {
        private readonly IEscalationService _escalationService;

        public EscalationsController(IEscalationService escalationService)
        {
            _escalationService = escalationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetActive()
        {
            var result = await _escalationService.GetActiveAsync();
            return Ok(result);
        }

        [HttpPost("scan")]
        public async Task<IActionResult> Scan()
        {
            var created = await _escalationService.ScanAsync();
            return Ok(new { created });
        }
    }
}
