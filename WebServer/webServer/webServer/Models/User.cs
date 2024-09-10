namespace LicenseApplication.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Authority { get; set; }
        public DateTime CreatedAt { get; set; }

        public int PlanId { get; set; }
        // Bir kullanıcının birden fazla lisansı olabilir
        //public virtual ICollection<License> Licenses { get; set; }
        private readonly UserSql _context;

        // LicenseService bağımlılığı kurucu ile sağlanır
        public User(UserSql _context)
        {
            _context = _context;
        }
        public User()
        {

        }
        public User(string email, string password)
        {
            this.Email = email;
            this.Password = password;
        }
        public bool UserControl(User User)
        {
            UserSql sqlQuestioning = new UserSql();
            return sqlQuestioning.UserCheck(User);
        }
        public async Task<bool> UserWriteSql(User User)
        {
            UserSql sqlkWrite = new UserSql();
            return await sqlkWrite.UserWrite(User);

        }
    }
}
