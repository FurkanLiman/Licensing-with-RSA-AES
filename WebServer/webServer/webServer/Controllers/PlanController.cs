using LicenseApplication.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LicenseApplication.Controllers
{
    public class PlanController : Controller
    {
        private readonly ApplicationDbContext _context;

        // LicenseService bağımlılığı kurucu ile sağlanır
        public PlanController(ApplicationDbContext planService)
        {
            _context = planService;
        }

        // GET: planController
        [HttpPost]
        public ActionResult Index(IFormCollection planId)
        {
            
            int Id = int.Parse(planId["id"]);
            PlanSql getPlanInfo = new PlanSql();
            LicensePlans plan = getPlanInfo.PlanReadInfo(Id);
            
            return RedirectToAction("planDetails", "plan", plan);
        }
        public ActionResult Index()
        {
            return RedirectToAction("Index","Home");
        }
        public IActionResult Products()
        {
            PlanSql getplan = new PlanSql();
            List<LicensePlans> plans = getplan.planRead();
            return View(plans);
        }
        [HttpPost]
        public IActionResult Products(string bt)
        {

            PlanSql getplan = new PlanSql();
            List<LicensePlans> plans = getplan.planRead();
            LinqPlan askComm = new LinqPlan();
            return View(plans);
        }
      

        [HttpPost]
        public ActionResult planDetails(string comment,int id)
        {
            var UserCache = User.Claims.ToList();
            var UserEmail = UserCache[0].Value.ToString();
            var UserComment = UserCache[1].Value.ToString();
            UserComment += ": " + comment;
            LicensePlans plan = new LicensePlans();
            PlanSql commentSender= new PlanSql();
            plan = commentSender.MakeComment(UserComment, id);
            return RedirectToAction("planDetails","plan", plan);
        }
        [HttpGet]
        public ActionResult planDetails(LicensePlans plan)
        {
            return View(plan);
        }
        [HttpPost]
        public IActionResult searchInP(string ss)
        {
            PlanSql getplan = new PlanSql();
            List<LicensePlans> plans = getplan.planRead();
            LinqPlan askS = new LinqPlan();
            plans = askS.askS(plans, ss);
            return View(plans);
        }
        // GET: planController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: planController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: planController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: planController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: planController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: planController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
