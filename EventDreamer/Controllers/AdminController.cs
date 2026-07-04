using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EventDreamer.Models;

namespace EventDreamer.Controllers
{
    public class AdminController : Controller
    {
        private EventDreamerDBEntities db = new EventDreamerDBEntities();

        // 1. KONTROLNA TABLA / STATISTIKA
        public ActionResult Index()
        {
            // Osnovna statistika za prve dvije kartice
            ViewBag.UkupnoKorisnika = db.Users.Count();
            ViewBag.UkupnoDogadjaja = db.Events.Count();

            // Brojanje po kategorijama vendora (pretpostavljamo da su ID-jevi u bazi od 1 do 5)
            // Ako su ID-jevi drugačiji u tvojoj bazi, samo ih prilagodi
            ViewBag.BrojRestorana = db.Vendors.Count(v => v.CategoryID == 1);
            ViewBag.BrojFotografa = db.Vendors.Count(v => v.CategoryID == 2);
            ViewBag.BrojMuzike = db.Vendors.Count(v => v.CategoryID == 3);
            ViewBag.BrojDekoracije = db.Vendors.Count(v => v.CategoryID == 4);
            ViewBag.BrojTorti = db.Vendors.Count(v => v.CategoryID == 5);

            return View();
        }

        // 2. UPRAVLJANJE KORISNICIMA
        public ActionResult ManageUsers()
        {
            var korisnici = db.Users.ToList();
            return View(korisnici);
        }

        // 3. UPRAVLJANJE VENDORIMA (Prikaz tabele i forme)
        public ActionResult ManageVendors()
        {
            // Šaljemo sve kategorije u ViewBag da bi ih Dropdown u formi mogao izlistati
            ViewBag.Kategorije = db.VendorCategories.ToList();

            var vendori = db.Vendors.ToList();
            return View(vendori);
        }

        // POST: Dodavanje vendora sa uploadom slike sa kompjutera
        [HttpPost]
        public ActionResult DodajVendora(string naziv, int kategorijaId, string kontakt, decimal cijena, HttpPostedFileBase slikaFajl)
        {
            if (!string.IsNullOrEmpty(naziv))
            {
                // Podrazumijevana slika sa interneta u slučaju da korisnik ne izabere ništa
                string slikaUrl = "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?q=80&w=600&auto=format&fit=crop";

                // Provjera da li je fajl stvarno poslat sa računara
                if (slikaFajl != null && slikaFajl.ContentLength > 0)
                {
                    // Putanja do foldera unutar tvog projekta
                    string folderPath = Server.MapPath("~/Images/Vendors/");

                    // Ako folder slučajno ne postoji, sistem ga sam pravi
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    // Uzimamo ekstenziju (.jpg, .png...) i generišemo jedinstveno nasumično ime
                    string extension = Path.GetExtension(slikaFajl.FileName);
                    string jedinstvenoIme = Guid.NewGuid().ToString().Substring(0, 8) + extension;

                    // Spajamo putanju i snimamo fajl na tvoj hard disk u sklopu projekta
                    string punaPutanja = Path.Combine(folderPath, jedinstvenoIme);
                    slikaFajl.SaveAs(punaPutanja);

                    // Relativna putanja koju upisujemo u bazu (kako bi HTML mogao da je prikaže preko /Images/...)
                    slikaUrl = "/Images/Vendors/" + jedinstvenoIme;
                }

                // Kreiranje objekta i čuvanje u bazu podataka
                var noviVendor = new Vendor
                {
                    Name = naziv,
                    CategoryID = kategorijaId,
                    Contact = kontakt,
                    BasePrice = cijena,
                    ImagePath = slikaUrl
                };

                db.Vendors.Add(noviVendor);
                db.SaveChanges();
            }

            return RedirectToAction("ManageVendors");
        }

        // Brisanje vendora iz sistema
        public ActionResult ObrisiVendora(int id)
        {
            var vendor = db.Vendors.Find(id);
            if (vendor != null)
            {
                db.Vendors.Remove(vendor);
                db.SaveChanges();
            }
            return RedirectToAction("ManageVendors");
        }

        // 4. FINANSIJSKI IZVJEŠTAJI / REPORTS
        // 4. FINANSIJSKI IZVJEŠTAJI / REPORTS
        public ActionResult Reports()
        {
            // Prenosimo osnovne podatke
            ViewBag.UkupnoKorisnika = db.Users.Count();
            ViewBag.UkupnoDogadjaja = db.Events.Count();

            // Ispravka za Goste: Brojimo koliko ukupno redova ima u cijeloj tabeli Guests u bazi!
            ViewBag.UkupnoGostiju = db.Guests.Count();

            // Ispravka za Budžet: Sabiramo kolonu 'TotalBudget' koja stvarno postoji u tvom Event.cs!
            ViewBag.UkupnoTroskova = db.Events.Sum(e => (decimal?)e.TotalBudget) ?? 0;

            return View();
        }

        // Oslobađanje resursa baze kada se kontroler ugasi
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