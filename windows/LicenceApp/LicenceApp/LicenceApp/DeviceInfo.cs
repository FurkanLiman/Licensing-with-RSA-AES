using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Management;

namespace RSA_Cryptography
{
    class DeviceInfo
    {
        public string Info { get; private set; }
        public byte[] InfoHash;
        public DeviceInfo()
        {
            this.Info = GetCpuId() + GetBiosSerialNumber() + GetMotherboardSerialNumber();
            this.InfoHash = ComputeSha256HashBytes(Info);
        }
        private static byte[] ComputeSha256HashBytes(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Ham veriyi byte dizisine çevir ve hash hesapla
                byte[] byteArray = Encoding.UTF8.GetBytes(rawData);
                byte[] hashBytes = sha256Hash.ComputeHash(byteArray);
                return hashBytes;
            }
        }
        public byte[] getInfoBytes()
        {
            return Encoding.UTF8.GetBytes(Info);
        }
        private string GetCpuId()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select ProcessorId from Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["ProcessorId"]?.ToString() ?? "Unknown";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting CPU ID: " + ex.Message);
            }
            return "Unknown";
        }

        private string GetBiosSerialNumber()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select SerialNumber from Win32_BIOS"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["SerialNumber"]?.ToString() ?? "Unknown";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting BIOS Serial Number: " + ex.Message);
            }
            return "Unknown";
        }

        private string GetMotherboardSerialNumber()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select SerialNumber from Win32_BaseBoard"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj["SerialNumber"]?.ToString() ?? "Unknown";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting Motherboard Serial Number: " + ex.Message);
            }
            return "Unknown";
        }
    }
}
