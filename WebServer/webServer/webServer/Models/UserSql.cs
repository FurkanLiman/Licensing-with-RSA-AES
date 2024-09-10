using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace LicenseApplication.Models
{
    public class UserSql
    {
        private readonly ApplicationDbContext _context;
        public UserSql()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=postgres;")
            .Options;

            _context = new ApplicationDbContext(options);
            
        }
        
        public User UserInfo(string email)
        {
            User account = new User(email, "*");
            var user = _context.Users
                .Where(u => u.Email == email)
                .Select(u => new User
                {
                    Email = u.Email,
                    Username = u.Username,
                    Authority = u.Authority
                })
                  .FirstOrDefault();

            // Kullanıcı bulunamazsa null döner, burada varsayılan bir kullanıcı nesnesi dönebiliriz
            return user ?? new User(email, "*");

        }
        public bool UserCheck(User User)
        {
            //"SELECT CASE WHEN EXISTS (SELECT * FROM \"Users\" WHERE \"User_Name\" = '" + User.Email + "' and \"User_Password\" = '" + User.Password + "') THEN CAST(true AS BOOL) ELSE CAST(false AS BOOL) END"))"
            var exists = _context.Users
           .Any(u => u.Email == User.Email && u.Password == User.Password);

            return exists;
        }
        public async Task<bool> UserWrite(User User)
        {

            bool userExists = await _context.Users
                .AnyAsync(u => u.Email == User.Email);

            if (userExists)
            {
                return false; // Kullanıcı zaten mevcut
            }
            else
            {
                // Kullanıcıyı ekle
                _context.Users.Add(User);
                await _context.SaveChangesAsync();
                return true; // Kullanıcı başarıyla eklendi

            }
        }
    }
}
