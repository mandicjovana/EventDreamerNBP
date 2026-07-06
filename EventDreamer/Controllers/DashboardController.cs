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

        // za dashboard prikazivanje broja događaja i korisnika
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

            // pozivamo stored proceduru koja vraća događaje sa statistikom
            var dogadjajiSaStatistikom = db.Database.SqlQuery<DogadjajStatistikaRow>(
                "EXEC sp_MojeDogadjajiStatistika @p_UserID = {0}", userId
            ).ToList();

            // dodajemo rjecnike za broj gostiju i broj zadataka po događaju
            var potvrdjeniGostiPoDogadjaju = new Dictionary<int, int>();
            var brojZadatakaPoDogadjaju = new Dictionary<int, int>();

            //za prikzivanje u view-u, kreiramo listu događaja
            var mojiDogadjaji = new List<Event>();

            foreach (var item in dogadjajiSaStatistikom)
            {
                potvrdjeniGostiPoDogadjaju.Add(item.Id, item.BrojGostiju);
                brojZadatakaPoDogadjaju.Add(item.Id, item.BrojZadataka);

                mojiDogadjaji.Add(new Event
                {
                    Id = item.Id,
                    Title = item.Title,
                    Date = item.Date,
                    Location = item.Location,
                    TotalBudget = item.TotalBudget
                });
            }

            ViewBag.PotvrdjeniGosti = potvrdjeniGostiPoDogadjaju;
            ViewBag.BrojZadataka = brojZadatakaPoDogadjaju;

            // 1 za kategoriju restorana
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

            try
            {
                // procedura za brisanje dogadjaja
                db.Database.ExecuteSqlCommand("EXEC sp_ObrisiDogadjaj @p_EventID = {0}", id);
            }
            catch (Exception)
            {
                TempData["Greska"] = "Došlo je do greške prilikom brisanja događaja.";
            }

            return RedirectToAction("MojiDogadjaji");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
        // Pomoćna klasa za statistiku događaja
        public class DogadjajStatistikaRow
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public DateTime? Date { get; set; }
            public string Location { get; set; }
            public decimal TotalBudget { get; set; }
            public int BrojGostiju { get; set; }
            public int BrojZadataka { get; set; }
        }
    }
}