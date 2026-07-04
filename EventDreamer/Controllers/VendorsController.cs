using System;
using System.Linq;
using System.Web.Mvc;
using EventDreamer.Models;

namespace EventDreamer.Controllers
{
    public class VendorsController : Controller
    {
        // Konekcija sa pravom SQL bazom
        private EventDreamerDBEntities db = new EventDreamerDBEntities();

        public ActionResult Index(string pretragaKategorije)
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            // ID trenutno ulogovanog korisnika
            int userId = (int)Session["UserID"];

            // 1. Vučemo PRAVE VENDORE iz baze (zajedno sa njihovom kategorijom)
            var filtrirano = db.Vendors.Include("VendorCategory").AsQueryable();

            if (!string.IsNullOrEmpty(pretragaKategorije) && pretragaKategorije != "Sve")
            {
                filtrirano = filtrirano.Where(v => v.VendorCategory.CategoryName == pretragaKategorije);
            }

            // 2. Vučemo PRAVE DOGAĐAJE tog korisnika kako bi mu se prikazali u onom iskačućem prozoru (modalu)
            ViewBag.MojiDogadjaji = db.Events.Where(e => e.UserID == userId).ToList();

            return View(filtrirano.ToList());
        }

        [HttpPost]
        public ActionResult PosaljiUpit(int vendorId, string dogadjajNaziv, string datum, int brojGostiju)
        {
            // Pronalazimo pravog vendora u bazi da bismo mu uzeli ime
            var vendor = db.Vendors.Find(vendorId);
            if (vendor != null)
            {
                TempData["Poruka"] = $"Upit za {brojGostiju} gostiju (Događaj: {dogadjajNaziv}) na dan {datum} je uspješno poslat saradniku: {vendor.Name}!";
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