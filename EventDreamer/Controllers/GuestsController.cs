using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EventDreamer.Models;

namespace EventDreamer.Controllers
{
    public class GuestsController : Controller
    {
        private EventDreamerDBEntities db = new EventDreamerDBEntities();

        // GET: Guests
        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];

            //dogadjaji za padajuci meni za ulogovanog korisnika
            var mojiDogadjaji = db.Events.Where(e => e.UserID == userId).ToList();
            ViewBag.ListaDogadjaja = new SelectList(mojiDogadjaji, "Id", "Title");

            // samo gosti koji pripadaju tim dogadjajima
            var mojiDogadjajiIds = mojiDogadjaji.Select(e => e.Id).ToList();
            var mojiGosti = db.Guests.Where(g => mojiDogadjajiIds.Contains(g.EventId)).ToList();

            return View(mojiGosti);
        }

        // POST: Guests/DodajGosta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DodajGosta(string ime, string prezime, int EventId)
        {
            if (!string.IsNullOrEmpty(ime) && !string.IsNullOrEmpty(prezime))
            {
                var noviGost = new Guest
                {
                    FirstName = ime,
                    LastName = prezime,
                    RSVPStatus = "Na čekanju", // po defaultu
                    TableNumber = null,        // po defaultu
                    EventId = EventId
                };

                db.Guests.Add(noviGost);
                db.SaveChanges();
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

                //ako potvrdi i posalje broj stola, upisujemo ga u bazu
                if (noviStatus == "Potvrđeno" && brojStola.HasValue)
                {
                    gost.TableNumber = brojStola.Value;
                }
                // ako je otkazano ili na cekanju nema broja stola
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
            var gost = db.Guests.Find(id);
            if (gost != null)
            {
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