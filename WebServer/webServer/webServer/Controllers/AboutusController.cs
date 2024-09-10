using Microsoft.AspNetCore.Mvc;

namespace LicenseApplication.Controllers
{
    public class AboutusController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
