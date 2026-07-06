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

        // GET: Tasks/Index
        public ActionResult Index()
        {
            if (Session["UserID"] == null) return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            // padajući meni sa događajima korisnika
            var mojiDogadjaji = db.Events.Where(e => e.UserID == userId).ToList();
            ViewBag.ListaDogadjaja = new SelectList(mojiDogadjaji, "Id", "Title");

            // povuci sve zadatke koji pripadaju tim događajima
            var mojiDogadjajiIds = mojiDogadjaji.Select(e => e.Id).ToList();
            var mojiZadaci = db.Tasks.Where(t => mojiDogadjajiIds.Contains(t.EventID)).ToList();

            return View(mojiZadaci);
        }

        // POST: Tasks/DodajZadatak
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DodajZadatak(string taskName, int EventId, DateTime? dueDate)
        {
            if (!string.IsNullOrEmpty(taskName))
            {
                try
                {
                    // poziv procedure iz baze
                    db.Database.ExecuteSqlCommand(
                        "EXEC sp_DodajZadatak @p_EventID={0}, @p_TaskName={1}, @p_DueDate={2}",
                        EventId, taskName, dueDate.HasValue ? (object)dueDate.Value : DBNull.Value
                    );
                }
                catch (Exception ex)
                {
                    // triger reaguje ako unesemo negativan datum
                    TempData["Greska"] = ex.Message;
                }
            }
            return RedirectToAction("Index");
        }

        // POST: Tasks/PromijeniStatus
        [HttpPost]
        public ActionResult PromijeniStatus(int id)
        {
            try
            {
                db.Database.ExecuteSqlCommand("EXEC sp_PromijeniStatusZadatka @p_TaskID={0}", id);
            }
            catch (Exception)
            {
                TempData["Greska"] = "Došlo je do greške prilikom izmjene statusa.";
            }
            return RedirectToAction("Index");
        }

        // GET: Tasks/ObrisiZadatak
        public ActionResult ObrisiZadatak(int id)
        {
            try
            {
                db.Database.ExecuteSqlCommand("EXEC sp_ObrisiZadatak @p_TaskID={0}", id);
            }
            catch (Exception)
            {
                TempData["Greska"] = "Ne mogu obrisati zadatak.";
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