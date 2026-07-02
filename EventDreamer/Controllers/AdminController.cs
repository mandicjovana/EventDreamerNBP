using System;
using System.Web.Mvc;

namespace EventDreamer.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        public ActionResult Index()
        {
            if (Session["UserEmail"] == null || Session["IsAdmin"] == null || !(bool)Session["IsAdmin"])
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }
    }
}