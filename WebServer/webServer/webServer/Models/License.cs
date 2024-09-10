namespace LicenseApplication.Models
{
    public class License
    {
        public int LicenseId { get; set; }
        public int UserId { get; set; }
        public string LicenseKey { get; set; }
        public int PlanId { get; set; }
        public DateTime ExpirationDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public string LicenseIv { get; set; }
        public string DeviceInfo { get; set; }
        public string LicensePublicKey { get; set; }

        // İlişkili kullanıcı ve planlar
        //public virtual User User { get; set; }
        //public virtual LicensePlans LicensePlans { get; set; }
    }
}
