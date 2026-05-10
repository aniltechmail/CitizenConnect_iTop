using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class LocationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
