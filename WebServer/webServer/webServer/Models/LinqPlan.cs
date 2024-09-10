using NuGet.Protocol.Core.Types;
using System.Linq;
using System.Xml.Linq;

namespace LicenseApplication.Models
{
    public class LinqPlan
    {
      
        public List<LicensePlans> askS (List<LicensePlans> plans, string sthing)
        {
            return plans.Where(x => x.PlanName.Contains(sthing) || x.Description.Contains(sthing) || x.Price.ToString().Contains(sthing)).ToList();
        }
    }
}
