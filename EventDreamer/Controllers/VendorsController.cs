using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EventDreamer.Models;

namespace EventDreamer.Controllers
{
    public class VendorsController : Controller
    {
        private EventDreamerDBEntities db = new EventDreamerDBEntities();

        public ActionResult Index(int? kId, decimal? maxCijena)
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");
            int userId = (int)Session["UserID"];

            int kategorijaId = kId ?? 0;
            // Ako korisnik nije unio cijenu, stavljamo veliku podrazumijevanu vrijednost (npr. 5000)
            decimal selektovanaCijena = maxCijena ?? 5000.00m;

            List<Vendor> listaVendora;

            // Ako je izabrana konkretna kategorija, aktiviramo SQL proceduru sa oba parametra!
            if (kategorijaId > 0)
            {
                var paramKategorija = new System.Data.SqlClient.SqlParameter("@p_KategorijaID", System.Data.SqlDbType.Int) { Value = kategorijaId };
                var paramCijena = new System.Data.SqlClient.SqlParameter("@p_MaxCijena", System.Data.SqlDbType.Decimal) { Value = selektovanaCijena };

                try
                {
                    listaVendora = db.Database.SqlQuery<Vendor>(
                        "EXEC PretragaVendora @p_KategorijaID = @p_KategorijaID, @p_MaxCijena = @p_MaxCijena",
                        paramKategorija,
                        paramCijena
                    ).ToList();
                }
                catch (Exception)
                {
                    // Fallback opcija ako mapiranje baci grešku
                    listaVendora = db.Vendors.Where(v => v.CategoryID == kategorijaId && v.BasePrice <= selektovanaCijena).ToList();
                }
            }
            else
            {
                // Ako su izabrani "Svi vendori" ili je tek otvorena stranica, prikazujemo sve ispod unijete cijene
                listaVendora = db.Vendors.Where(v => v.BasePrice <= selektovanaCijena).ToList();
            }

            // Za punjenje padajućeg menija kategorija na HTML stranici
            ViewBag.Kategorije = db.VendorCategories.ToList();
            ViewBag.TrenutnaKategorija = kategorijaId;
            ViewBag.TrenutnaCijena = maxCijena; // Da ostane upisana vrijednost u polju nakon pretrage

            ViewBag.MojiDogadjaji = db.Events.Where(e => e.UserID == userId).ToList();

            return View(listaVendora);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PosaljiUpit(int vendorId, int eventId, int? brojGostiju, string odabraniPaket)
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            var vendor = db.Vendors.Find(vendorId);
            var dogadjaj = db.Events.Find(eventId);

            if (vendor != null && dogadjaj != null)
            {
                decimal konacnaCijena = vendor.BasePrice;
                string opisTroska = $"Angažovanje: {vendor.Name}";
                string novaLokacija = null;

                // Računanje ako je vendor Sala / Restoran (Kategorija 1)
                if (vendor.CategoryID == 1)
                {
                    int gosti = brojGostiju ?? 1;
                    decimal cijenaPoOsobi = konacnaCijena;
                    string nazivPaketa = "Svadbeni paket (Premium)";

                    if (odabraniPaket == "standard")
                    {
                        cijenaPoOsobi = Math.Max(15m, konacnaCijena - 10m);
                        nazivPaketa = "Rođendanski paket (Standard)";
                    }
                    else if (odabraniPaket == "poslovni")
                    {
                        cijenaPoOsobi = Math.Max(10m, konacnaCijena - 15m);
                        nazivPaketa = "Poslovni paket";
                    }

                    konacnaCijena = cijenaPoOsobi * gosti;
                    opisTroska += $" ({nazivPaketa}, za {gosti} osoba - {cijenaPoOsobi} € po osobi)";

                    // Spremamo lokaciju da je proslijedimo SQL-u
                    novaLokacija = vendor.Name;
                }

                try
                {
                    // 🚀 Pozivamo transakcionu SQL proceduru da završi posao
                    db.Database.ExecuteSqlCommand(
                        "EXEC sp_RezervisiVendora @p_EventID={0}, @p_NovaLokacija={1}, @p_OpisTroska={2}, @p_KonacnaCijena={3}",
                        dogadjaj.Id,
                        novaLokacija != null ? (object)novaLokacija : DBNull.Value,
                        opisTroska,
                        konacnaCijena
                    );

                    TempData["Poruka"] = $"🎉 Uspješno ste rezervisali: {vendor.Name}! Trošak od {konacnaCijena} € je dodat u budžet i označen kao plaćen.";
                }
                catch (Exception)
                {
                    TempData["Greska"] = "Sistem nije uspio da sačuva rezervaciju u bazu.";
                }
            }
            else
            {
                TempData["Greska"] = "Došlo je do greške prilikom rezervacije. Nepostojeći vendor ili događaj.";
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