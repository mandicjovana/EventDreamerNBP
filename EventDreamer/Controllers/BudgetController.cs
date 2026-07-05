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
        public ActionResult Index()
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            // Za potrebe analize, uzećemo prvi događaj u bazi
            // (U realnom sistemu ovdje bi prosljeđivala ID selektovanog događaja)
            var prviDogadjaj = db.Events.FirstOrDefault();

            if (prviDogadjaj != null)
            {
                // POZIV TVOJE STORED PROCEDURE IZ BAZE: AnalizaTroskovaDogadjaja
                // Pokrećemo proceduru i mapiramo rezultat na našu pomoćnu klasu TrosakAnalizaRow
                var analiza = db.Database.SqlQuery<TrosakAnalizaRow>(
                    "EXEC AnalizaTroskovaDogadjaja @p_EventID = {0}", prviDogadjaj.Id
                ).FirstOrDefault();

                if (analiza != null)
                {
                    ViewBag.NazivDogadjaja = analiza.Title;
                    ViewBag.Ukupno = analiza.TotalBudget;
                    ViewBag.Potroseno = analiza.UkupnoPotroseno;
                    ViewBag.Preostalo = analiza.PreostaliBudzet;
                }
            }
            else
            {
                ViewBag.Ukupno = 0;
                ViewBag.Potroseno = 0;
                ViewBag.Preostalo = 0;
            }

            var sviTroskovi = db.Expenses.ToList();
            return View(sviTroskovi);
        }

        // POST: Budget/DodajTrosak
        [HttpPost]
        public ActionResult DodajTrosak(string naziv, decimal planirano, decimal stvarno, string placeno)
        {
            if (!string.IsNullOrEmpty(naziv))
            {
                var prviDogadjaj = db.Events.FirstOrDefault();
                int dogadjajId = prviDogadjaj != null ? prviDogadjaj.Id : 1;

                var noviTrosak = new Expens
                {
                    ExpenseName = naziv,
                    PlannedAmount = planirano,
                    ActualAmount = stvarno,
                    IsPaid = (placeno == "Da"),
                    EventID = dogadjajId
                };

                db.Expenses.Add(noviTrosak);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // GET: Budget/ObrisiTrosak
        public ActionResult ObrisiTrosak(int id)
        {
            var trosak = db.Expenses.Find(id);
            if (trosak != null)
            {
                db.Expenses.Remove(trosak);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    // POMOĆNA KLASA: Mora imati identične nazive i tipove kolona kao SELECT u tvojoj SQL proceduri!
    public class TrosakAnalizaRow
    {
        public string Title { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal UkupnoPotroseno { get; set; }
        public decimal PreostaliBudzet { get; set; }
    }
}