using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EventDreamer.Models;

namespace EventDreamer.Controllers
{
    public class TasksController : Controller
    {
        private EventDreamerDBEntities db = new EventDreamerDBEntities();

        // GET: Tasks
        public ActionResult Index()
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            // Padajući meni sa događajima korisnika
            var mojiDogadjaji = db.Events.Where(e => e.UserID == userId).ToList();
            ViewBag.ListaDogadjaja = new SelectList(mojiDogadjaji, "Id", "Title");

            // Povuci sve zadatke koji pripadaju tim događajima
            var mojiDogadjajiIds = mojiDogadjaji.Select(e => e.Id).ToList();
            var mojiZadaci = db.Tasks.Where(t => mojiDogadjajiIds.Contains(t.EventID)).ToList();

            return View(mojiZadaci);
        }

        // POST: Tasks/DodajZadatak
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DodajZadatak(string taskName, int EventId)
        {
            if (!string.IsNullOrEmpty(taskName))
            {
                var noviZadatak = new Task
                {
                    TaskName = taskName,
                    IsCompleted = false,
                    EventID = EventId
                };

                db.Tasks.Add(noviZadatak);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // POST: Tasks/PromijeniStatus
        [HttpPost]
        public ActionResult PromijeniStatus(int id)
        {
            var zadatak = db.Tasks.Find(id);
            if (zadatak != null)
            {
                zadatak.IsCompleted = !zadatak.IsCompleted;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // GET: Tasks/ObrisiZadatak
        public ActionResult ObrisiZadatak(int id)
        {
            var zadatak = db.Tasks.Find(id);
            if (zadatak != null)
            {
                db.Tasks.Remove(zadatak);
                db.SaveChanges();
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