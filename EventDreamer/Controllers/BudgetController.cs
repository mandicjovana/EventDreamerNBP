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

        // GET: Budget
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
            // ako je korisnik izabrao iz menija, uzimamo taj. Inače uzimamo njegov prvi.
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

        // POST: Budget/DodajTrosak
        [HttpPost]
        public ActionResult DodajTrosak(string naziv, decimal planirano, decimal stvarno, string placeno, int EventId)
        {
            if (!string.IsNullOrEmpty(naziv))
            {
                var noviTrosak = new Expens // novi trosak
                {
                    ExpenseName = naziv,
                    PlannedAmount = planirano,
                    ActualAmount = stvarno,
                    IsPaid = (placeno == "Da"),
                    EventID = EventId // trosak vezujemo za odredjeni dogadjaj
                };

                db.Expenses.Add(noviTrosak);
                db.SaveChanges();
            }

            // vratimo na Index ali smo idalje na istom dogadjaju
            return RedirectToAction("Index", new { eventId = EventId });
        }

        // GET: Budget/ObrisiTrosak
        public ActionResult ObrisiTrosak(int id, int eventId)
        {
            var trosak = db.Expenses.Find(id);
            if (trosak != null)
            {
                db.Expenses.Remove(trosak);
                db.SaveChanges();
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