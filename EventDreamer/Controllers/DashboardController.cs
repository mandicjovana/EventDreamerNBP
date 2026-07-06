using System;
using System.Collections.Generic;
using System.Data.Entity;
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
            ViewBag.BrojKorisnika = db.Users.Count();

            return View();
        }

        // GET: Dashboard/MojiDogadjaji
        public ActionResult MojiDogadjaji()
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            // Povlačenje svih događaja za ulogovanog korisnika
            var mojiDogadjaji = db.Events.Where(e => e.UserID == userId).ToList();

            // 1. Brojanje potvrđenih gostiju preko SQL skalarne funkcije
            var potvrdjeniGostiPoDogadjaju = new Dictionary<int, int>();
            foreach (var e in mojiDogadjaji)
            {
                int brojGostiju = db.Database.SqlQuery<int>("SELECT dbo.BrojPotvrdjenihGostiju({0})", e.Id).FirstOrDefault();
                potvrdjeniGostiPoDogadjaju.Add(e.Id, brojGostiju);
            }
            ViewBag.PotvrdjeniGosti = potvrdjeniGostiPoDogadjaju;

            // 🚀 POPRAVLJENO: Brojanje ZADATAKA sada koristi tvoju novu SQL skalarnu funkciju iz baze!
            var brojZadatakaPoDogadjaju = new Dictionary<int, int>();
            foreach (var e in mojiDogadjaji)
            {
                int brojZadataka = db.Database.SqlQuery<int>("SELECT dbo.BrojZadataka({0})", e.Id).FirstOrDefault();
                brojZadatakaPoDogadjaju.Add(e.Id, brojZadataka);
            }
            ViewBag.BrojZadataka = brojZadatakaPoDogadjaju;

            // Lokacije (Kategorija 1 su Restorani/Sale)
            int idKategorijeRestorana = 1;
            var restorani = db.Vendors
                              .Where(v => v.CategoryID == idKategorijeRestorana)
                              .Select(v => v.Name)
                              .ToList();

            ViewBag.ListaLokacija = new SelectList(restorani);

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
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            var dogadjaj = db.Events.Find(id);
            if (dogadjaj != null)
            {
                try
                {
                    // Kaskadno čišćenje tabela prije brisanja glavnog događaja
                    db.Guests.RemoveRange(db.Guests.Where(g => g.EventId == id));
                    db.Tasks.RemoveRange(db.Tasks.Where(t => t.EventID == id));
                    db.Expenses.RemoveRange(db.Expenses.Where(ex => ex.EventID == id));

                    // Tek na kraju brišemo sam događaj
                    db.Events.Remove(dogadjaj);
                    db.SaveChanges();
                }
                catch (Exception)
                {
                    TempData["Greska"] = "Došlo je do greške prilikom brisanja događaja.";
                }
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