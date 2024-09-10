using LicenseApplication.Models;
using Microsoft.AspNetCore.Mvc;

namespace LicenseApplication.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        // LicenseService bağımlılığı kurucu ile sağlanır
        public ProfileController(ApplicationDbContext _context)
        {
            _context = _context;
        }
        public IActionResult Index()
        {
            var UserCache = User.Claims.ToList();
            var UserEmail = UserCache[0].Value.ToString();
            var UserName = UserCache[1].Value.ToString();
            var UserAuthority = UserCache[2].Value.ToString();
            UserSql CheckSql= new UserSql();
            User UserN = new User(UserEmail,"*");
            UserN.Authority = UserAuthority;
            UserN.Username= UserName;
            UserN = CheckSql.UserInfo(UserEmail);
            if (UserN.Authority == "1") { 
               
                return araAction();
                //return RedirectToAction("AdminPanel", "Profile");
            }
            else
            {
                PlanSql getplan = new PlanSql();
                List<LicensePlans> plans = getplan.planRead();
                return View(plans);
            }
        }
        
        public IActionResult araAction()
        {
           
            //ProfileController takesAdmin = new ProfileController();
            PlanSql getplan = new PlanSql();
            List<LicensePlans> plans = getplan.planRead();



            return AdminPanel(plans);
        }
        [HttpPost]
        public IActionResult AdminPanel(string id)
        {
            PlanSql getplan = new PlanSql();
            getplan.Deleteplan(id);
            return RedirectToAction("index", "Profile");

        }

        private IActionResult AdminPanel(List<LicensePlans> plans)
        {//panel Private boylece yalnızca admin ulaşıyor
            

            return View("AdminPanel",plans);


        }
        [HttpPost]
        public IActionResult AdminplanAdd(string name, string description, string price, string duration)
        {
            PlanSql addplan = new PlanSql();
            if(addplan.Addplan(name, description, price, duration))
            {
                return RedirectToAction("index");
            }
            else
            {
                TempData["AlertMessage"] = "Kullanıcı kaydı başarısız.";
            }

            return RedirectToAction("index", "Profile");
        }

        [HttpPost]
        public async Task<IActionResult> AdminUserAdd(string Name, string Email, string Password, string Authority)
        {
            User User = new User(Email, Password);
            User.Username = Name;
            User.Authority = Authority;

            if (await User.UserWriteSql(User))
            {//kişi kayıdı başarılıysa logine giriş yapmaya yollandı
                return RedirectToAction("index");
            }
            else
            {//kişi kaydında sıkıntı varsa tekrar kayıt sekmesine yollandı
                TempData["AlertMessage"] = "Kullanıcı kaydı başarısız.";
            }

            return RedirectToAction("index", "Profile");
        }
        [HttpPost]
        public IActionResult deleteComment(string id, string comId) 
        {

            PlanSql delComment= new PlanSql();
            delComment.deleteCommentSql(id, comId);
            return RedirectToAction("index", "Profile");
        }

    }
}
