
using Microsoft.EntityFrameworkCore;

namespace LicenseApplication.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // LicensePlans için birincil anahtarı tanımla
            modelBuilder.Entity<User>()
                .ToTable("users");

            modelBuilder.Entity<LicensePlans>()
                .ToTable("licenseplans");

            modelBuilder.Entity<License>()
                .ToTable("licenses");

            modelBuilder.Entity<PrimaryKeys>()
                .ToTable("PrimaryKeys");


            modelBuilder.Entity<LicensePlans>()
                .HasKey(lp => lp.LicensePlansId); // Burada PlanId'yi birincil anahtar olarak tanımlıyoruz
            modelBuilder.Entity<LicensePlans>()
                .Property(lp => lp.LicensePlansId)
                .HasColumnName("LicensePlansId"); // Sütun adının doğru olduğundan emin olun


            modelBuilder.Entity<PrimaryKeys>()
                .HasKey(lp => lp.Id); // Burada PlanId'yi birincil anahtar olarak tanımlıyoruz
            modelBuilder.Entity<PrimaryKeys>()
                .Property(lp => lp.Id)
                .HasColumnName("id"); // Sütun adının doğru olduğundan emin olun
            base.OnModelCreating(modelBuilder);
        }
        
        public DbSet<User> Users { get; set; }
        public DbSet<License> Licenses { get; set; }
        public DbSet<LicensePlans> LicensePlans { get; set; }
        public DbSet<PrimaryKeys> PrimaryKeys { get; set; }

    }
}