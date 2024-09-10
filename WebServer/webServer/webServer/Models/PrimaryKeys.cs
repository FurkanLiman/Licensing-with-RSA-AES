using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LicenseApplication.Models
{
    public class PrimaryKeys
    {
        public int Id { get; set; }
        public string PrimaryPublicKey { get; set; }
        public string PrimaryPrivateKey { get; set; }

    }
}
