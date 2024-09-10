using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LicenceApp;

public class TimeValidator
{
    private DateTime lastRecordedTime;
    private DateTime initialSystemTime;
    private byte[] publicKey;
    private byte[] privateKey;
    private string timeFilePath;
    private int licenseDuration = 365; // lisans süresi limiti 365 günlük lisans (day)
    private int deltaLimit = 6048000; // kullanıcının saat değiştirme hakkı 1 haftalık limit (second)
    public TimeValidator()
    {

        this.timeFilePath = FileManager.initialTimePath;

        if (File.Exists(timeFilePath) && File.Exists(FileManager.timePublicKeyPath) && File.Exists(FileManager.timePrivateKeyPath) && File.Exists(FileManager.timeDeltaPath)){
            byte[] lastRunTimeString = File.ReadAllBytes(timeFilePath);


            this.publicKey = File.ReadAllBytes(FileManager.timePublicKeyPath);
            this.privateKey = File.ReadAllBytes(FileManager.timePrivateKeyPath);


            lastRunTimeString =  DecryptTime(lastRunTimeString);
            string dd = Encoding.UTF8.GetString(lastRunTimeString);
            lastRecordedTime = DateTime.Parse(dd, null, System.Globalization.DateTimeStyles.RoundtripKind);
            initialSystemTime = DateTime.UtcNow;
            if (initialSystemTime >= lastRecordedTime)
            {
                lastRecordedTime = DateTime.UtcNow;
                byte[] enc = EncryptTime(Encoding.UTF8.GetBytes(lastRecordedTime.ToString("o")));
                File.WriteAllBytes(timeFilePath, enc);
            }
        }else{
            Logger.Log("Time files missing or corrupted");
        }
    }
    public bool timeValidation(int looptime){
        // saatin ana kontrol fonksiyonu.
        if (checkDelta())
        {
            if (isLicenceExpired()) { 
                int logTimeDifference = logTimeCheck(looptime);
                int timeDataDifference = timeDataCheck(looptime);
                if (logTimeDifference != 0 || timeDataDifference != 0)
                {
                    updateDelta((int)((logTimeDifference + timeDataDifference) / 2));
                    Logger.Log($"[Time] Time change detected: {(int)(logTimeDifference + timeDataDifference) / 2}");
                }
                return true;
            }
            else
            {
                Console.WriteLine("License has expired. Reactivate the license.");
                Logger.Log("License has expired. Reactivate the license.");
                return false;
            }
        }
        else
        {
            Console.WriteLine("1 Week Time Manipulation Detected. Delta Exceeding");
            Logger.Log("1 Week Time Manipulation Detected. Delta Exceeding");
            return false;
        }
    }
        private bool isLicenceExpired() {
        byte[] setUpTimeString = File.ReadAllBytes(FileManager.setUpTime);
        setUpTimeString= DecryptTime(setUpTimeString);
        string setUpTime= Encoding.UTF8.GetString(setUpTimeString);
        DateTime SetUpTime = DateTime.Parse(setUpTime, null, System.Globalization.DateTimeStyles.RoundtripKind);

        initialSystemTime = DateTime.UtcNow;
        TimeSpan elapsedTime = initialSystemTime - SetUpTime;
        return elapsedTime.TotalDays < licenseDuration;
    }
    private void updateDelta(int deltaIncrease){
        // timeDelta.dat dosyasına bakıyor ve kaçan süreleri topluyor.
        byte[] timeDelta = File.ReadAllBytes(FileManager.timeDeltaPath);
        timeDelta = DecryptTime(timeDelta);
        string deltaString = Encoding.UTF8.GetString(timeDelta);
        int delta = Convert.ToInt32(deltaString);
        delta += Math.Abs((int)deltaIncrease);
        byte[] deltaEncrypt = EncryptTime(Encoding.UTF8.GetBytes(delta.ToString()));
        File.WriteAllBytes(FileManager.timeDeltaPath, deltaEncrypt);
    }
    private int timeDataCheck(int looptime)
    {
        // initialTime.dat dosyasına bakıyor ve saati karşılaştırıyor.
        byte[] lastRunTimeString = File.ReadAllBytes(timeFilePath);
        lastRunTimeString = DecryptTime(lastRunTimeString);
        string dd = Encoding.UTF8.GetString(lastRunTimeString);
        lastRecordedTime = DateTime.Parse(dd, null, System.Globalization.DateTimeStyles.RoundtripKind);

        initialSystemTime = DateTime.UtcNow;                
        TimeSpan elapsedTime = initialSystemTime - lastRecordedTime;
        double differance = elapsedTime.TotalSeconds - looptime;
        byte[] enc = EncryptTime(Encoding.UTF8.GetBytes(initialSystemTime.ToString("o")));
        File.WriteAllBytes(timeFilePath, enc);
        return Math.Abs(differance)<30 ? 0 : (int)differance; 
     
    }
    private bool checkDelta(){
        // Saat sapmasının toplamı. 1 haftalık limite bakıyor. Eğer limit aşıldıysa -> False.
        byte[] timeDelta = File.ReadAllBytes(FileManager.timeDeltaPath);
        timeDelta = DecryptTime(timeDelta);
        string deltaString = Encoding.UTF8.GetString(timeDelta);
        int delta = Convert.ToInt32(deltaString);
        return delta < deltaLimit;
    }
    private byte[] EncryptTime(byte[] time)
    {

        using (var rsa = new RSACryptoServiceProvider(2048))
        {
            rsa.ImportCspBlob(publicKey);
            byte[] encryptedData = rsa.Encrypt(time, false);
            return encryptedData;
        }
    }
    private byte[] DecryptTime(byte[] encryptedTime)
    {
        using (var rsa = new RSACryptoServiceProvider(2048))
        {
            rsa.ImportCspBlob(privateKey);
            byte[] encryptedData = rsa.Decrypt(encryptedTime, false);
            return encryptedData;
        }
    }
    private int logTimeCheck(int second)
    {
        string distribution = DetectLinuxDistribution();
        string logFilePath = GetLogFilePath(distribution);
        second = (int)(second*1.2);
        Console.WriteLine(second);
        if (!File.Exists(logFilePath))
        {
            Console.WriteLine("Log file not found.");
            return 0;
        }
        else 
        {
            DateTime currentTime = DateTime.Now;
            DateTime timeThreshold = currentTime.AddSeconds(-second);
            string[] logLines = File.ReadAllLines(logFilePath);  
            int clockChangeDetected = 0;
            
            DateTime previousTime= DateTime.UtcNow;
            foreach (string line in logLines)
            {
                // Satırın tarih ve saat bilgisini al (ilk kelimeler tarih ve saat olacak şekilde varsayıyoruz)
                string[] parts = line.Split(' ');
                
                if (parts.Length > 2)
                {
                    string dateStr = parts[0] + " " + parts[1] + " " + parts[2];
                    DateTime currentLogDateTime;

                    // Tarih ve saat formatını kontrol et ve parse et
                    if (DateTime.TryParseExact(dateStr, "MMM d HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out currentLogDateTime))
                    {
                        // Log zamanının belirtilen zaman aralığında olup olmadığını kontrol et
                        if (LineContainsClockChange(line,distribution))
                        {
                            if (previousTime >= timeThreshold && previousTime <= currentTime)
                            {
                            TimeSpan timeDifference = currentLogDateTime - previousTime;
                            double hoursDifference = timeDifference.TotalSeconds;

                            clockChangeDetected = (int)hoursDifference;                            
                            }
                        }
                        previousTime = currentLogDateTime;
                    }
                }
            }
            return clockChangeDetected;
        }
    }
    private static string DetectLinuxDistribution()
    {
        if (File.Exists("/etc/os-release"))
        {
            string[] lines = File.ReadAllLines("/etc/os-release");
            foreach (string line in lines)
            {
                if (line.StartsWith("ID="))
                {
                    return line.Substring(3).Trim('"');
                }
            }
        }
        return "unknown";
    }
    private string GetLogFilePath(string distribution)
    {
        switch (distribution)
        {
            case "ubuntu":
            case "debian":
            case "linuxmint":
                return "/var/log/syslog";
            case "centos":
            case "fedora":
            case "rhel":
                return "/var/log/messages";
            case "arch":
            case "manjaro":
                return "/var/log/journal";
            case "opensuse":
                return "/var/log/messages";
            default:
                return null;
        }
    }
    private static bool LineContainsClockChange(string line, string distribution)
    {
        switch (distribution)
        {
            case "ubuntu":
            case "debian":
            case "linuxmint":
                return line.Contains("Clock change detected") || line.Contains("Time has been changed");
            case "centos":
            case "fedora":
            case "rhel":
                return line.Contains("System clock set") || line.Contains("Synchronized to time server");
            case "arch":
            case "manjaro":
                return line.Contains("Time synchronized") || line.Contains("Clock change detected");
        
            case "opensuse":
                return line.Contains("System clock set");
            default:
                return false;
        }
    }
}
