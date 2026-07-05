using System;
using System.Linq;
using System.Web.Mvc;
using EventDreamer.Models;

namespace EventDreamer.Controllers
{
    public class VendorsController : Controller
    {
        private EventDreamerDBEntities db = new EventDreamerDBEntities();

        public ActionResult Index(int? kId)
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");
            int userId = (int)Session["UserID"];

            var filtrirano = db.Vendors.Include("VendorCategory").AsQueryable();

            if (kId.HasValue && kId.Value > 0)
            {
                filtrirano = filtrirano.Where(v => v.CategoryID == kId.Value);
            }

            ViewBag.MojiDogadjaji = db.Events.Where(e => e.UserID == userId).ToList();

            return View(filtrirano.ToList());
        }

        [HttpPost]
        public ActionResult PosaljiUpit(int vendorId, int eventId, string datum, int? brojGostiju, string odabraniPaket)
        {
            var vendor = db.Vendors.Find(vendorId);
            var dogadjaj = db.Events.Find(eventId);

            if (vendor != null && dogadjaj != null)
            {
                decimal konacnaCijena = vendor.BasePrice;
                string opisTroska = $"Angažovanje: {vendor.Name}";

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
                }

                if (DateTime.TryParse(datum, out DateTime noviDatum))
                {
                    dogadjaj.Date = noviDatum;
                }

                var noviTrosak = new Expens
                {
                    ExpenseName = opisTroska,
                    PlannedAmount = konacnaCijena,
                    ActualAmount = konacnaCijena,
                    IsPaid = false,
                    EventID = dogadjaj.Id
                };

                db.Expenses.Add(noviTrosak);
                db.SaveChanges();

                TempData["Poruka"] = $"🎉 Uspješno ste rezervisali: {vendor.Name}! Trošak od {konacnaCijena} € je automatski dodat u Vaš budžet.";
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