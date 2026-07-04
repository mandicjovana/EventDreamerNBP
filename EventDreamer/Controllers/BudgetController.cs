using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EventDreamer.Models;

namespace EventDreamer.Controllers
{
    public class BudgetController : Controller
    {
        // Povezivanje sa tvojom pravom bazom
        private EventDreamerDBEntities db = new EventDreamerDBEntities();

        // GET: Budget
        public ActionResult Index()
        {
            // Zaštita od neulogovanih
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            // Povlačimo sve troškove iz prave baze
            var sviTroskovi = db.Expenses.ToList();

            // Izračunavanje statistike na osnovu pravih iznosa (koristimo ActualAmount)
            ViewBag.Ukupno = sviTroskovi.Sum(t => t.ActualAmount);
            ViewBag.Placeno = sviTroskovi.Where(t => t.IsPaid).Sum(t => t.ActualAmount);
            ViewBag.Preostalo = ViewBag.Ukupno - ViewBag.Placeno;

            return View(sviTroskovi);
        }

        // POST: Budget/DodajTrosak
        [HttpPost]
        public ActionResult DodajTrosak(string naziv, decimal planirano, decimal stvarno, string placeno)
        {
            if (!string.IsNullOrEmpty(naziv))
            {
                var noviTrosak = new Expens
                {
                    ExpenseName = naziv,
                    PlannedAmount = planirano,
                    ActualAmount = stvarno,
                    IsPaid = (placeno == "Da"),
                    EventID = 1 // Privremeno vezujemo za prvi događaj zbog stranog ključa
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
}