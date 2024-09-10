using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Npgsql;
using NuGet.Packaging;
using NuGet.Protocol;
using System.Collections;
using System.Linq;
using static System.Net.WebRequestMethods;

namespace LicenseApplication.Models
{
    public class PlanSql
    {
        private readonly ApplicationDbContext _context;
        public PlanSql()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=postgres;")
            .Options;

            _context = new ApplicationDbContext(options);

        }
        public List<LicensePlans> planRead()
        {

                // Tüm planları listele
            var planList = _context.LicensePlans
                .Select(plan => new LicensePlans
                {
                    LicensePlansId = plan.LicensePlansId,
                    PlanName = plan.PlanName,
                    Description = plan.Description,
                    Price = plan.Price,
                    CreatedAt = plan.CreatedAt
                })
                .ToList(); // Liste olarak döndür

            return planList;

        }
        public void deleteCommentSql(string id, string comId)
        {//SİL BUNU

        }
        public bool Addplan(string name, string description="-", string price = "0.0"   , string duration = "-")
        {
            // 1. İlk olarak, eğer aynı isimde bir plan var mı kontrol et
            var existingPlan = _context.Users
                .Any(u => u.Email == name); // 'users' tablosunda 'email' ile kontrol yapıyoruz

            // Eğer plan zaten mevcutsa false döndür
            if (existingPlan)
            {
                return false;
            }

            // 2. Eğer plan mevcut değilse, yeni bir LicensePlan ekle
            var newPlan = new LicensePlans
            {
                PlanName = name,
                Description = description,
                Price = Convert.ToDecimal(price), // Fiyatı decimal olarak kaydet
                CreatedAt = DateTime.UtcNow // Oluşturulma tarihini otomatik olarak ekle
            };

            // Veritabanına ekle ve kaydet
            _context.LicensePlans.Add(newPlan);
            _context.SaveChanges(); // Değişiklikleri kaydet

            return true; // Başarıyla eklendiğinde true 
        }
        public void Deleteplan(string id)
        {
            var planToDelete = _context.LicensePlans.FirstOrDefault(plan => plan.LicensePlansId == Int32.Parse(id));

            // Eğer plan bulunursa, silme işlemini gerçekleştir
            if (planToDelete != null)
            {
                _context.LicensePlans.Remove(planToDelete); // Planı sil
                _context.SaveChanges(); // Değişiklikleri veritabanına kaydet
                Console.WriteLine("Plan silindi.");
            }
            else
            {
                Console.WriteLine("Plan bulunamadı.");
            }
        }
        public LicensePlans PlanReadInfo(int id)
        {
            var plan = _context.LicensePlans
            .Where(p => p.LicensePlansId == id) // Plan ID'sine göre filtreleme
            .Select(p => new LicensePlans
            {
                LicensePlansId = p.LicensePlansId,
                PlanName = p.PlanName,
                Description = p.Description,
                Price = p.Price,
                CreatedAt = p.CreatedAt
            })
            .FirstOrDefault(); // İlk eşleşen planı getir (yoksa null döner)

        return plan; // Plan

        }
        public LicensePlans MakeComment(string comment, int id)
        {//SİL BUNU
            return new LicensePlans();
        }


    }
}
