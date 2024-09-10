using LicenseApplication.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace LicenseApplication.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AccountController()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=postgres;")
            .Options;

            _context = new ApplicationDbContext(options);

        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(String Email, String Password)
        {//Başlangıç adresimiz Account/Login
           //Login .cshtml'dan Email ve Password verisini alıp Model üzerinden Sql kontrolü sağlayacak.
            User User = new User(Email, Password);
            UserSql check = new UserSql();
            if (User.UserControl(User))
            {//Başarılı giriş sonucu ana menüye göndermekte
                User = check.UserInfo(Email);

                TempData["UserData"] = JsonConvert.SerializeObject(User);

                HttpContext.Session.SetString("UserEmail",User.Email);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email,User.Email),
                    new Claim(ClaimTypes.Name,User.Username),
                    new Claim(ClaimTypes.Role,User.Authority)
                };
                var useridentity = new ClaimsIdentity(claims,"a");
                ClaimsPrincipal principal = new ClaimsPrincipal(useridentity);
                await HttpContext.SignInAsync(principal);
                return RedirectToAction("Index", "Home"); 
            }
            else
            {
                return View();
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SignIn(String Email, String Password, String Name)
        {
            User User = new User(Email,Password);
            User.Username = Name;
            User.Authority = "0";
            if (await User.UserWriteSql(User))
            {//kişi kayıdı başarılıysa logine giriş yapmaya yollandı
                return RedirectToAction("Login");
            }
            else
            {//kişi kaydında sıkıntı varsa tekrar kayıt sekmesine yollandı
                return View();
            }
        }
        [AllowAnonymous]
        public IActionResult SignIn()
        {
            //sign In işlemi yaptır
            return View();
        }
        [AllowAnonymous]
        public IActionResult Login()
        {

            return View();
        }
        public IActionResult Verify()
        {
            return View();  
        }
    }
}
