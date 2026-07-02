using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EventDreamer.Models;

namespace EventDreamer.Controllers
{
    public class AccountController : Controller
    {
        // Koristimo tvoj pravi Entity Framework kontekst iz modela
        private EventDreamerDBEntities db = new EventDreamerDBEntities();

        // GET: Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Tražimo korisnika u pravoj tabeli "Users"
                // Napomena: Ako ti se kolone u User.cs zovu na našem jeziku (npr. Lozinka umjesto Password), samo ih prepravi ovdje
                var korisnik = db.Users.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);

                if (korisnik != null)
                {
                    // Punimo podatke u sesiju
                    Session["UserEmail"] = korisnik.Email;
                    Session["UserFirstName"] = korisnik.FirstName ?? "Korisnik";
                    Session["UserID"] = korisnik.Id;

                    // --- PRAVA RASKRSNICA ZA ULOGE (Preko RoleId) ---
                    // Pretpostavljamo da je Admin npr. RoleId == 1 (ili provjeravamo preko RoleName)
                    if (korisnik.RoleId == 1 || korisnik.Email.ToLower().Contains("admin"))
                    {
                        Session["IsAdmin"] = true;
                        return RedirectToAction("Index", "Admin");
                    }
                    else
                    {
                        Session["IsAdmin"] = false;
                        return RedirectToAction("Index", "Dashboard");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Pogrešan email ili lozinka.");
                }
            }

            return View(model);
        }

        // GET: Account/Register
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Provjera da li email već postoji u tabeli Users
                var postojiKorisnik = db.Users.Any(u => u.Email == model.Email);
                if (postojiKorisnik)
                {
                    ModelState.AddModelError("", "Korisnik sa ovom email adresom već postoji.");
                    return View(model);
                }

                // Kreiramo novog korisnika u bazi
                var noviKorisnik = new User
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Password = model.Password,
                    RoleId = 2 // Po defaultu 2 (npr. običan korisnik/klijent)
                };

                db.Users.Add(noviKorisnik);
                db.SaveChanges();

                // Automatski login nakon registracije
                Session["UserEmail"] = noviKorisnik.Email;
                Session["UserFirstName"] = noviKorisnik.FirstName;
                Session["UserID"] = noviKorisnik.Id;
                Session["IsAdmin"] = false;

                return RedirectToAction("Index", "Dashboard");
            }

            return View(model);
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
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