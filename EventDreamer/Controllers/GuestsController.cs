using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EventDreamer.Models; // Provjeri da li je ovaj namespace tačan za tvoje modele

namespace EventDreamer.Controllers
{
    public class GuestsController : Controller
    {
        // Povezujemo se na tvoju pravu SQL bazu podataka
        private EventDreamerDBEntities db = new EventDreamerDBEntities();

        // GET: Guests
        public ActionResult Index()
        {
            // Ako neko pokuša ručno da ode na /Guests bez logina:
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var sviGosti = db.Guests.ToList();
            return View(sviGosti);
        }

        // POST: Guests/DodajGosta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DodajGosta(string ime, string prezime, string status, int brojStola)
        {
            if (!string.IsNullOrEmpty(ime) && !string.IsNullOrEmpty(prezime))
            {
                // Kreiramo objekat tvoje prave klase Guest iz baze
                var noviGost = new Guest
                {
                    FirstName = ime,
                    LastName = prezime,
                    RSVPStatus = status,
                    TableNumber = brojStola,
                    EventId = 1 // Privremeno vezujemo za prvi događaj jer je to obavezno polje (strani ključ) u tvojoj bazi
                };

                // Upisujemo u bazu podataka
                db.Guests.Add(noviGost);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // GET: Guests/ObrisiGosta
        public ActionResult ObrisiGosta(int id)
        {
            // Nalazimo gosta u bazi po njegovom ID-ju
            var gost = db.Guests.Find(id);
            if (gost != null)
            {
                // Brišemo ga iz baze podataka
                db.Guests.Remove(gost);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}