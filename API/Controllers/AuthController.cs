using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
