using System.Collections;
using System.ComponentModel;

namespace LicenseApplication.Models
{
    public class LicensePlans
    {
        public int LicensePlansId { get; set; }
        public string PlanName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }

        // Bir planın birden fazla lisansı olabilir
        //public virtual ICollection<License> License { get; set; }
    }
}
