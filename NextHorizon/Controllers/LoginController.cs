using Microsoft.AspNetCore.Mvc;

namespace NextHorizon.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult AdminLogin()
        {
            return View();
        }
    }
}
