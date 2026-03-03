using Microsoft.AspNetCore.Mvc;
using MyAspNetApp2.Models;
using System.Diagnostics;

namespace MyAspNetApp2.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult ProfileView()
        {
            return View();
        }

        public IActionResult UpdateProfile()
        {
            return View();
        }

        public IActionResult UploadActivity()
        {
            return View();
        }

        public IActionResult ShippingAddress()
        {
            return View();
        }

        public IActionResult PaymentMethods()
        {
            return View();
        }

        public IActionResult MyPurchases()
        {
            return View();
        }


        public IActionResult FitnessOverview()
        {
            return View();
        }

        public IActionResult WeeklyFitnessOverview()
        {
            return View();
        }

        public IActionResult MonthlyFitnessOverview()
        {
            return View();
        }

        public IActionResult YearlyFitnessOverview()
        {
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
