using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security.Cryptography;

namespace RSA_Cryptography
{
    class DeviceInfo
    {
        public string Info { get; private set; }
        public byte[] InfoHash;

        public DeviceInfo()
        {
            string[] infos;
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm || RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                infos = getInfoArm();
            }
            else
            {
                // x86/x64 tabanlı sistemler için gerekli sorguları yap
                infos = getInfox86();
            }  

            this.Info = string.Join("-", infos);
            this.InfoHash = ComputeSha256HashBytes(Info);
        }
        private static string[] getInfoArm()
        {
            try
            {
                // Seri numarasını almak için /proc/cpuinfo dosyasını okuma
                string serialNumber = ExecuteCommandArm("cat /proc/cpuinfo | grep Serial | awk '{print $3}'");
                string diskSerial = ExecuteCommandArm("lsblk -o NAME,SERIAL | grep -v NAME | awk '{print $2}'");
                string macAddress = ExecuteCommandArm("cat /sys/class/net/eth0/address");
                return new string[] {serialNumber.Trim(), diskSerial.Trim(), macAddress.Trim()};
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving device infos: " + ex.Message);
                return new string[] {"Unknown","Unknown","Unknown"};
            }
        }

        private static string ExecuteCommandArm(string command)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(processInfo))
            using (StreamReader reader = process.StandardOutput)
            {
                return reader.ReadToEnd();
            }
        }

        private static string[] getInfox86()
        {
            try
            {
                // Use sudo to ensure root permissions
                string cpuSerial = ExecuteCommandx86("sudo dmidecode -t processor | grep 'ID' | awk '{print $2}'");
                string memorySerial = ExecuteCommandx86("sudo dmidecode -t memory | grep 'Serial Number' | awk '{print $3}'");
                string macAddress = ExecuteCommandx86("cat /sys/class/net/eth0/address");
                string motherboardSerial = ExecuteCommandx86("sudo dmidecode -t baseboard | grep 'Serial Number' | awk '{print $3}'");
                string biosVersion= ExecuteCommandx86("sudo dmidecode -t bios | grep 'Version' | awk '{print $2}'");
                return new string[] {cpuSerial.Trim(),memorySerial.Trim(),macAddress.Trim(),motherboardSerial.Trim(),biosVersion.Trim()};
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving device info: " + ex.Message);
                return new string[] {"Unknown","Unknown","Unknown","Unknown","Unknown"};
            }
        }

        private static string ExecuteCommandx86(string command)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(processInfo))
            using (StreamReader reader = process.StandardOutput)
            {
                return reader.ReadToEnd();
            }
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
       
    }
}
