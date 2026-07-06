using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EventDreamer.Models;

namespace EventDreamer.Controllers
{
    public class AccountController : Controller
    {
        // Entity Framework kontekst iz modela
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
                // trazimo korisnika iz tabele Users
                var korisnik = db.Users.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);

                if (korisnik != null)
                {
                    // popunimo sesiju podacima
                    Session["UserEmail"] = korisnik.Email;
                    Session["UserFirstName"] = korisnik.FirstName ?? "Korisnik";
                    Session["UserID"] = korisnik.Id;

                    // za uloge preko RoleId
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
        public ActionResult Register(string FirstName, string LastName, string Email, string Password)
        {
            // provjeravamo osnovne podatke iz forme
            if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName) &&
                !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password))
            {
                // provjeravamo da li već postoji korisnik sa tim emailom
                var postojiKorisnik = db.Users.Any(u => u.Email.ToLower() == Email.ToLower());
                if (postojiKorisnik)
                {
                    ModelState.AddModelError("", "Korisnik sa ovom email adresom već postoji.");
                    return View();
                }

                // kreiramo novog korisnika
                var noviKorisnik = new User
                {
                    FirstName = FirstName,
                    LastName = LastName,
                    Email = Email,
                    Password = Password,
                    RoleId = 2 // Običan korisnik
                };

                db.Users.Add(noviKorisnik);
                db.SaveChanges();

                // saljemo na login stranicu sa porukom o uspješnoj registraciji
                TempData["PorukaNakonRegistracije"] = "Uspješno ste se registrovali! Sada se možete prijaviti sa svojim podacima.";

                return RedirectToAction("Login", "Account");
            }

            ModelState.AddModelError("", "Sva polja su obavezna.");
            return View();
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