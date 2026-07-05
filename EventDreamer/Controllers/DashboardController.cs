using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EventDreamer.Models;

namespace EventDreamer.Controllers
{
    public class DashboardController : Controller
    {
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

            // OVDJE POZIVAMO TVOJU SQL FUNKCIJU IZ BAZE: BrojPotvrdjenihGostiju
            var potvrdjeniGostiPoDogadjaju = new Dictionary<int, int>();
            foreach (var e in mojiDogadjaji)
            {
                // Izvršavamo funkciju nad svakim pojedinačnim ID-jem događaja
                int brojGostiju = db.Database.SqlQuery<int>("SELECT dbo.BrojPotvrdjenihGostiju({0})", e.Id).FirstOrDefault();
                potvrdjeniGostiPoDogadjaju.Add(e.Id, brojGostiju);
            }

            // Šaljemo rječnik u View kako bismo ispisali broj gostiju pored svakog događaja
            ViewBag.PotvrdjeniGosti = potvrdjeniGostiPoDogadjaju;

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
                db.SaveChanges();
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