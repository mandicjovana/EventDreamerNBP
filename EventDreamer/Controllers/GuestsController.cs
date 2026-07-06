using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EventDreamer.Models;
using System.Data.Entity;

namespace EventDreamer.Controllers
{
    public class GuestsController : Controller
    {
        private EventDreamerDBEntities db = new EventDreamerDBEntities();

        // GET: Guests/Index
        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];

            // svi dogadjaji za padajuci meni
            var mojiDogadjaji = db.Events
                                  .Where(e => e.UserID == userId)
                                  .ToList();

            ViewBag.ListaDogadjaja = new SelectList(mojiDogadjaji, "Id", "Title");

            // id-evi mojih dogadjaja za filtriranje gostiju
            var mojiDogadjajiIds = mojiDogadjaji
                                   .Select(e => e.Id)
                                   .ToList();

            // gosti koji pripadaju mojim dogadjajima
            var mojiGosti = db.Guests
                              .Include(g => g.DietaryRequirements)
                              .Where(g => mojiDogadjajiIds.Contains(g.EventId))
                              .ToList();

            return View(mojiGosti);
        }

        // POST: Guests/DodajGosta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DodajGosta(string ime, string prezime, int EventId, string meni)
        {
            if (!string.IsNullOrEmpty(ime) && !string.IsNullOrEmpty(prezime))
            {
                try
                {
                    // procedura sp_DodajGosta dodaje gosta u bazu i vraća njegov ID
                    db.Database.ExecuteSqlCommand(
                        "EXEC sp_DodajGosta @p_EventID = {0}, @p_Ime = {1}, @p_Prezime = {2}, @p_Meni = {3}",
                        EventId,
                        ime,
                        prezime,
                        string.IsNullOrEmpty(meni) ? null : meni
                    );
                }
                catch (Exception)
                {
                    TempData["Greska"] = "Došlo je do greške prilikom dodavanja gosta u bazu.";
                }
            }

            return RedirectToAction("Index");
        }

        // POST: Guests/AzurirajStatus
        [HttpPost]
        public ActionResult AzurirajStatus(int id, string noviStatus, int? brojStola)
        {
            var gost = db.Guests.Find(id);

            if (gost != null)
            {
                gost.RSVPStatus = noviStatus;

                // ako gost potvrdi dolazak i poslat je broj stola, upisujemo ga
                if (noviStatus == "Potvrđeno" && brojStola.HasValue)
                {
                    gost.TableNumber = brojStola.Value;
                }
                // nema broja stola
                else
                {
                    gost.TableNumber = null;
                }

                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // GET: Guests/ObrisiGosta
        public ActionResult ObrisiGosta(int id)
        {
            try
            {
                // procedura obrisi gosta
                db.Database.ExecuteSqlCommand("EXEC sp_ObrisiGosta @p_GuestID = {0}", id);
            }
            catch (Exception)
            {
                TempData["Greska"] = "Sistem ne može bezbjedno obrisati odabranog gosta.";
            }

            return RedirectToAction("Index");
        }

        // oslobadjanje resursa konekcije sa bazom
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