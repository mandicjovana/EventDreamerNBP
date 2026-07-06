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
        public ActionResult Index()
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            // procedura za admin statistiku iz baze
            var stats = db.Database.SqlQuery<AdminStatsRow>("EXEC AdminStatistika").FirstOrDefault();

            // ako baza vrati podatke
            if (stats != null)
            {
                ViewBag.UkupnoKorisnika = stats.UkupnoKorisnika;
                ViewBag.UkupnoDogadjaja = stats.UkupnoDogadjaja;
                ViewBag.BrojRestorana = stats.BrojRestorana;
                ViewBag.BrojFotografa = stats.BrojFotografa;
                ViewBag.BrojMuzike = stats.BrojMuzike;
                ViewBag.BrojDekoracije = stats.BrojDekoracije;
                ViewBag.BrojTorti = stats.BrojTorti;
            }
            else
            {
                // u slucaju da je baza prazna
                ViewBag.UkupnoKorisnika = 0; 
                ViewBag.UkupnoDogadjaja = 0;
                ViewBag.BrojRestorana = 0; 
                ViewBag.BrojFotografa = 0;
                ViewBag.BrojMuzike = 0; 
                ViewBag.BrojDekoracije = 0; 
                ViewBag.BrojTorti = 0;
            }

            return View();
        }

        // za upravljanje korisnicima
        public ActionResult ManageUsers()
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            var korisnici = db.Users.ToList();
            return View(korisnici);
        }
        public ActionResult ObrisiKorisnika(int id)
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            var korisnik = db.Users.Find(id);
            if (korisnik != null)
            {
                // da ne možemo obrisati admina
                if (korisnik.RoleId == 1)
                {
                    TempData["Greska"] = "Ne možete obrisati administratorski nalog!";
                    return RedirectToAction("ManageUsers");
                }

                try
                {
                    // procedura iz baze za brisanje korisnika
                    db.Database.ExecuteSqlCommand("EXEC sp_ObrisiKorisnika @p_KorisnikID = {0}", id);

                    TempData["Poruka"] = $"Korisnik {korisnik.FirstName} {korisnik.LastName} i svi njegovi podaci su uspješno obrisani iz sistema.";
                }
                catch (Exception ex)
                {
                    TempData["Greska"] = "Došlo je do greške u bazi prilikom brisanja korisnika: " + ex.Message;
                }
            }
            return RedirectToAction("ManageUsers");
        }
        // Upravljanje vendorima
        public ActionResult ManageVendors(int? filterKategorijaId)
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            ViewBag.Kategorije = db.VendorCategories.ToList();

            int kategorijaId = filterKategorijaId ?? 0;
            System.Collections.Generic.List<Vendor> rezultatiVendori;

            // poziv funkcije iz baze
            string nazivKategorije = db.Database.SqlQuery<string>(
                "SELECT dbo.FiltrirajVendoraPoKategoriji({0})", kategorijaId
            ).FirstOrDefault();

            ViewBag.NazivKategorije = nazivKategorije;

            // filtriranje liste vendora na osnovu ID
            if (kategorijaId > 0)
            {
                rezultatiVendori = db.Vendors.Where(v => v.CategoryID == kategorijaId).ToList();
                ViewBag.IsFiltered = true;
            }
            else
            {
                rezultatiVendori = db.Vendors.ToList();
                ViewBag.IsFiltered = false;
            }

            return View(rezultatiVendori);
        }
        // dodavanje vendora sa uploadom slike sa kompjutera
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DodajVendora(string naziv, int kategorijaId, string kontakt, decimal cijena, HttpPostedFileBase slikaFajl)
        {
            if (!string.IsNullOrEmpty(naziv))
            {
                // ako nema slike nista, ostaje prazan string u bazi
                string slikaUrl = "";

                // provjera da li je fajl stvarno poslat sa računara
                if (slikaFajl != null && slikaFajl.ContentLength > 0)
                {
                    string folderPath = Server.MapPath("~/Images/Vendors/");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string extension = Path.GetExtension(slikaFajl.FileName);
                    string jedinstvenoIme = Guid.NewGuid().ToString().Substring(0, 8) + extension;

                    string punaPutanja = Path.Combine(folderPath, jedinstvenoIme);
                    slikaFajl.SaveAs(punaPutanja);

                    slikaUrl = "/Images/Vendors/" + jedinstvenoIme;
                }

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
        // izmjena vendora
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AzurirajVendora(int id, string naziv, int kategorijaId, string kontakt, decimal cijena, HttpPostedFileBase slikaFajlUredi)
        {
            var vendor = db.Vendors.Find(id);

            if (vendor != null && !string.IsNullOrEmpty(naziv))
            {
                vendor.Name = naziv;
                vendor.CategoryID = kategorijaId;
                vendor.Contact = kontakt;
                vendor.BasePrice = cijena;

                // promjena nove slike
                if (slikaFajlUredi != null && slikaFajlUredi.ContentLength > 0)
                {
                    string folderPath = Server.MapPath("~/Images/Vendors/");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string extension = Path.GetExtension(slikaFajlUredi.FileName);
                    string jedinstvenoIme = Guid.NewGuid().ToString().Substring(0, 8) + extension;

                    string punaPutanja = Path.Combine(folderPath, jedinstvenoIme);
                    slikaFajlUredi.SaveAs(punaPutanja);

                    // nova putanja, stara se brise
                    vendor.ImagePath = "/Images/Vendors/" + jedinstvenoIme;
                }
                // ako ne izmijenimo sliku, ostaje stara

                db.SaveChanges();
            }

            return RedirectToAction("ManageVendors");
        }

        // brisanje vendora iz baze
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

        // svi dogadjaji
        public ActionResult ManageEvents()
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            // svi događaji iz baze zajedno sa podacima o vlasniku i troškovima
            var sviDogadjaji = db.Events.Include("User").Include("Expenses").ToList();
            return View(sviDogadjaji);
        }
        

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        //pomocna funkcija za statistiku iz baze
        public class AdminStatsRow
        {
            public int UkupnoKorisnika { get; set; }
            public int UkupnoDogadjaja { get; set; }
            public int BrojRestorana { get; set; }
            public int BrojFotografa { get; set; }
            public int BrojMuzike { get; set; }
            public int BrojDekoracije { get; set; }
            public int BrojTorti { get; set; }
        }
    }
}