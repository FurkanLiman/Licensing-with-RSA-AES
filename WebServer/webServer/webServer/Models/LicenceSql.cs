using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.ComponentModel;

namespace LicenseApplication.Models
{
    public static class LicenceSql
    {
        private readonly static ApplicationDbContext _context;

        // LicenseService bağımlılığı kurucu ile sağlanır
        
        public static List<License> licanceRead()
        {

        var licenceList = _context.Licenses
            .Select(licence => new License
            {
                LicenseId = licence.LicenseId,
                UserId = licence.UserId,
                LicenseKey = licence.LicenseKey,
                PlanId = licence.PlanId,
                ExpirationDate = licence.ExpirationDate,
                CreatedAt = licence.CreatedAt
            })
            .ToList(); // Listeye dönüştür ve döndür

        return licenceList;
        }

            public static List<License> licanceRead(string username,string deviceInfo)
            {
                

                using (var connection = new NpgsqlConnection("Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=postgres;"))
                using (var command = new NpgsqlDataAdapter())
                using (var insertCommand = new NpgsqlCommand($"select user_id from \"users\" where username='{username}'"))
                {
                    insertCommand.Connection = connection;
                    command.InsertCommand = insertCommand;
                    List<License> licenceList = new List<License>();

                    connection.Open();
                    NpgsqlDataReader licences = insertCommand.ExecuteReader();
                    if (licences.HasRows == true)
                    {
                        while (licences.Read())
                        {
                            License licence = new License();
                        licence.LicenseId = licences.GetInt32(0);
                        licence.UserId = licences.GetInt32(1);
                        licence.LicenseKey = licences.GetString(2);
                        licence.PlanId = licences.GetInt32(3);
                        licence.ExpirationDate = licences.GetDateTime(4);
                        licence.CreatedAt = licences.GetDateTime(5);
                        licenceList.Add(licence);
                        }
                    }
                    else
                    {

                    }
                    return licenceList;

                }

            }
        }
    }
