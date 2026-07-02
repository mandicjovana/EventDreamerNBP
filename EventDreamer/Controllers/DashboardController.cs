using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EventDreamer.Controllers
{
    public class DashboardController : Controller
    {
        // GET: Dashboard
        public ActionResult Index()
        {
            // Ovdje ćemo kasnije povlačiti podatke iz baze preko tvog db konteksta
            return View();
        }
    }
}