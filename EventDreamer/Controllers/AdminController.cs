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

            ViewBag.UkupnoKorisnika = db.Users.Count();
            ViewBag.UkupnoDogadjaja = db.Events.Count();

            // brojanje vendora po kategorijama vendora
            ViewBag.BrojRestorana = db.Vendors.Count(v => v.CategoryID == 1);
            ViewBag.BrojFotografa = db.Vendors.Count(v => v.CategoryID == 2);
            ViewBag.BrojMuzike = db.Vendors.Count(v => v.CategoryID == 3);
            ViewBag.BrojDekoracije = db.Vendors.Count(v => v.CategoryID == 4);
            ViewBag.BrojTorti = db.Vendors.Count(v => v.CategoryID == 5);

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
                // da ne mozemo da obrisemo admina
                if (korisnik.RoleId == 1)
                {
                    TempData["Greska"] = "Ne možete obrisati administratorski nalog!";
                    return RedirectToAction("ManageUsers");
                }

                try
                {
                    // prvo brisemo sve podatke vezane za korisnika
                    var dogadjajiKorisnika = db.Events.Where(e => e.UserID == id).ToList();
                    foreach (var dogadjaj in dogadjajiKorisnika)
                    {
                        db.Guests.RemoveRange(db.Guests.Where(g => g.EventId == dogadjaj.Id));
                        db.Tasks.RemoveRange(db.Tasks.Where(t => t.EventID == dogadjaj.Id));
                        db.Expenses.RemoveRange(db.Expenses.Where(ex => ex.EventID == dogadjaj.Id));
                    }
                    // onda i same dogadjaje
                    db.Events.RemoveRange(dogadjajiKorisnika);

                    // i na kraju korisnika
                    db.Users.Remove(korisnik);
                    db.SaveChanges();

                    TempData["Poruka"] = $"Korisnik {korisnik.FirstName} {korisnik.LastName} i svi njegovi podaci su uspješno obrisani.";
                }
                catch (Exception)
                {
                    TempData["Greska"] = "Došlo je do greške u bazi prilikom brisanja korisnika.";
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
            decimal maxCijena = 5000.00m; // Podrazumijevana maksimalna cijena

            System.Collections.Generic.List<Vendor> rezultatiVendori;

            // 🚀 AKO JE IZABRANA KATEGORIJA (Id > 0) -> KORISTIMO PROCEDURU
            if (kategorijaId > 0)
            {
                var paramKategorija = new System.Data.SqlClient.SqlParameter("@p_KategorijaID", System.Data.SqlDbType.Int) { Value = kategorijaId };
                var paramCijena = new System.Data.SqlClient.SqlParameter("@p_MaxCijena", System.Data.SqlDbType.Decimal) { Value = maxCijena };

                try
                {
                    rezultatiVendori = db.Database.SqlQuery<Vendor>(
                        "EXEC PretragaVendora @p_KategorijaID = @p_KategorijaID, @p_MaxCijena = @p_MaxCijena",
                        paramKategorija,
                        paramCijena
                    ).ToList();
                }
                catch (System.Exception ex)
                {
                    TempData["Greska"] = "Greška u proceduri: " + ex.Message;
                    rezultatiVendori = db.Vendors.Where(v => v.CategoryID == kategorijaId).ToList();
                }

                ViewBag.IsFiltered = true;
                var kat = db.VendorCategories.Find(kategorijaId);
                ViewBag.NazivKategorije = kat != null ? kat.CategoryName : "Filtrirano";
            }
            // 🚀 AKO JE KLIKNUTO IZ SIDEBARA (Svi vendori, Id == 0) -> POVLAČIMO SVE IZ BAZE
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
    }
}