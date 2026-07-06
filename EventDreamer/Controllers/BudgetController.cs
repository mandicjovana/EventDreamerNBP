using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EventDreamer.Models;

namespace EventDreamer.Controllers
{
    public class BudgetController : Controller
    {
        private EventDreamerDBEntities db = new EventDreamerDBEntities();

        // za budzet
        public ActionResult Index(int? eventId)
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            // dogadjaji ulogovanog korisnika
            var mojiDogadjaji = db.Events.Where(e => e.UserID == userId).ToList();

            if (!mojiDogadjaji.Any())
            {
                ViewBag.NemaDogadjaja = true;
                return View(new List<Expens>()); // prazno ako nema dogadjaja
            }

            //koji dogadjaj posmatramo iz padajuceg menija
            // ako je korisnik izabrao iz menija, uzimamo taj. inače uzimamo njegov prvi.
            int odabraniDogadjajId = eventId ?? mojiDogadjaji.First().Id;

            // posaljemo padajuci meni u View
            ViewBag.EventId = new SelectList(mojiDogadjaji, "Id", "Title", odabraniDogadjajId);
            ViewBag.TrenutniDogadjajId = odabraniDogadjajId;

            // stored procedura iz baze za analizu troskova
            var analiza = db.Database.SqlQuery<TrosakAnalizaRow>(
                "EXEC AnalizaTroskovaDogadjaja @p_EventID = {0}", odabraniDogadjajId
            ).FirstOrDefault();

            if (analiza != null)
            {
                ViewBag.NazivDogadjaja = analiza.Title;
                ViewBag.Ukupno = analiza.TotalBudget;
                ViewBag.Potroseno = analiza.UkupnoPotroseno;
                ViewBag.Preostalo = analiza.PreostaliBudzet;
            }
            else
            {
                // u slucaju da procedura vrati prazno
                var samDogadjaj = mojiDogadjaji.First(e => e.Id == odabraniDogadjajId);
                ViewBag.NazivDogadjaja = samDogadjaj.Title;
                ViewBag.Ukupno = samDogadjaj.TotalBudget;
                ViewBag.Potroseno = 0;
                ViewBag.Preostalo = samDogadjaj.TotalBudget;
            }

            // vratimo troskove
            var troskoviDogadjaja = db.Expenses.Where(t => t.EventID == odabraniDogadjajId).ToList();

            return View(troskoviDogadjaja);
        }

        // za dodavanje troska
        [HttpPost]
        public ActionResult DodajTrosak(string naziv, decimal planirano, decimal stvarno, string placeno, int EventId)
        {
            if (!string.IsNullOrEmpty(naziv))
            {
                // string u bit
                int jeLiPlaceno = (placeno == "Da") ? 1 : 0;

                try
                {
                    // poziv procedure
                    db.Database.ExecuteSqlCommand(
                        "EXEC sp_DodajTrosak @p_EventID={0}, @p_Naziv={1}, @p_Planirano={2}, @p_Stvarno={3}, @p_Placeno={4}",
                        EventId, naziv, planirano, stvarno, jeLiPlaceno
                    );
                }
                catch (Exception ex)
                {
                    // greska ako neko unese negativan stvarni trosak zbog trigera iz baze
                    TempData["Greska"] = ex.Message;
                }
            }

            return RedirectToAction("Index", new { eventId = EventId });
        }

        // za brisanje troska
        public ActionResult ObrisiTrosak(int id, int eventId)
        {
            try
            {
                // procedura za brisanje
                db.Database.ExecuteSqlCommand("EXEC sp_ObrisiTrosak @p_TrosakID={0}", id);
            }
            catch (Exception)
            {
                TempData["Greska"] = "Došlo je do greške prilikom brisanja troška.";
            }

            return RedirectToAction("Index", new { eventId = eventId });
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    // Za proceduru pomocna klasa
    public class TrosakAnalizaRow
    {
        public string Title { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal UkupnoPotroseno { get; set; }
        public decimal PreostaliBudzet { get; set; }
    }
}