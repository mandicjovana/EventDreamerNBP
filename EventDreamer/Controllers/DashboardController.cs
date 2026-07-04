using System;
using System.Linq;
using System.Web.Mvc;
using EventDreamer.Models;

namespace EventDreamer.Controllers
{
    public class DashboardController : Controller
    {
        // Povezujemo se na pravu bazu
        private EventDreamerDBEntities db = new EventDreamerDBEntities();

        // GET: Dashboard
        public ActionResult Index()
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];
            ViewBag.BrojDogadjaja = db.Events.Count(e => e.UserID == userId);

            return View();
        }

        // GET: Dashboard/MojiDogadjaji
        public ActionResult MojiDogadjaji()
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            // Izvlačimo samo događaje trenutno ulogovanog korisnika
            var mojiDogadjaji = db.Events.Where(e => e.UserID == userId).ToList();

            return View(mojiDogadjaji);
        }

        // POST: Dashboard/DodajDogadjaj
        [HttpPost]
        public ActionResult DodajDogadjaj(string title, DateTime? date, string location, decimal totalBudget)
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");
            int userId = (int)Session["UserID"];

            if (!string.IsNullOrEmpty(title))
            {
                // Uzimamo prvi tip događaja iz baze da bismo ispunili obavezno polje EventTypesId
                var prviTip = db.EventTypes.FirstOrDefault();
                int tipId = prviTip != null ? prviTip.Id : 1;

                var noviDogadjaj = new Event
                {
                    UserID = userId,
                    EventTypesId = tipId,
                    Title = title,
                    Date = date,
                    Location = location,
                    TotalBudget = totalBudget
                };

                db.Events.Add(noviDogadjaj);
                db.SaveChanges(); // Trajno snimanje u SQL!
            }
            return RedirectToAction("MojiDogadjaji");
        }

        // GET: Dashboard/ObrisiDogadjaj
        public ActionResult ObrisiDogadjaj(int id)
        {
            var dogadjaj = db.Events.Find(id);
            if (dogadjaj != null)
            {
                db.Events.Remove(dogadjaj);
                db.SaveChanges();
            }
            return RedirectToAction("MojiDogadjaji");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}