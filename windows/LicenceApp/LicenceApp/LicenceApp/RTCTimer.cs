using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
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

        if (File.Exists(timeFilePath) && File.Exists(FileManager.timePublicKeyPath) && File.Exists(FileManager.timePrivateKeyPath) && File.Exists(FileManager.timeDeltaPath))
        {
            byte[] lastRunTimeString = ProtectedData.Unprotect(File.ReadAllBytes(timeFilePath), null, DataProtectionScope.CurrentUser);


            this.publicKey = ProtectedData.Unprotect(File.ReadAllBytes(FileManager.timePublicKeyPath), null, DataProtectionScope.CurrentUser);
            this.privateKey = ProtectedData.Unprotect(File.ReadAllBytes(FileManager.timePrivateKeyPath), null, DataProtectionScope.CurrentUser);


            lastRunTimeString = DecryptTime(lastRunTimeString);
            string dd = Encoding.UTF8.GetString(lastRunTimeString);
            lastRecordedTime = DateTime.Parse(dd, null, System.Globalization.DateTimeStyles.RoundtripKind);
            initialSystemTime = DateTime.UtcNow;
            if (initialSystemTime >= lastRecordedTime)
            {
                lastRecordedTime = DateTime.UtcNow;
                byte[] enc = EncryptTime(Encoding.UTF8.GetBytes(lastRecordedTime.ToString("o")));
                File.WriteAllBytes(timeFilePath, ProtectedData.Protect(enc, null, DataProtectionScope.CurrentUser));
            }
        }
        else
        {
            Logger.Log("Time files missing or corrupted");
        }
    }
    public bool timeValidation(int looptime)
    {
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
        byte[] setUpTimeString = ProtectedData.Unprotect(File.ReadAllBytes(FileManager.setUpTime), null, DataProtectionScope.CurrentUser);
        setUpTimeString= DecryptTime(setUpTimeString);
        string setUpTime= Encoding.UTF8.GetString(setUpTimeString);
        DateTime SetUpTime = DateTime.Parse(setUpTime, null, System.Globalization.DateTimeStyles.RoundtripKind);

        initialSystemTime = DateTime.UtcNow;
        TimeSpan elapsedTime = initialSystemTime - SetUpTime;
        return elapsedTime.TotalDays < licenseDuration;
    }
    private void updateDelta(int deltaIncrease)
    {
        // timeDelta.dat dosyasına bakıyor ve kaçan süreleri topluyor.

        byte[] timeDelta = ProtectedData.Unprotect(File.ReadAllBytes(FileManager.timeDeltaPath), null, DataProtectionScope.CurrentUser);;
        timeDelta = DecryptTime(timeDelta);
        string deltaString = Encoding.UTF8.GetString(timeDelta);
        int delta = Convert.ToInt32(deltaString);
        delta += Math.Abs((int)deltaIncrease);
        byte[] deltaEncrypt = EncryptTime(Encoding.UTF8.GetBytes(delta.ToString()));
        File.WriteAllBytes(FileManager.timeDeltaPath, ProtectedData.Protect(deltaEncrypt, null, DataProtectionScope.CurrentUser));
    }
    private int timeDataCheck(int looptime)
    {
        // initialTime.dat dosyasına bakıyor ve saati karşılaştırıyor.
        byte[] lastRunTimeString = ProtectedData.Unprotect(File.ReadAllBytes(timeFilePath), null, DataProtectionScope.CurrentUser);
        lastRunTimeString = DecryptTime(lastRunTimeString);
        string dd = Encoding.UTF8.GetString(lastRunTimeString);
        lastRecordedTime = DateTime.Parse(dd, null, System.Globalization.DateTimeStyles.RoundtripKind);

        initialSystemTime = DateTime.UtcNow;
        TimeSpan elapsedTime = initialSystemTime - lastRecordedTime;
        double differance = elapsedTime.TotalSeconds - looptime;
        byte[] enc = EncryptTime(Encoding.UTF8.GetBytes(initialSystemTime.ToString("o")));
        File.WriteAllBytes(timeFilePath, ProtectedData.Protect(enc, null, DataProtectionScope.CurrentUser));
        return Math.Abs(differance) < 30 ? 0 : (int)differance;

    }
    private bool checkDelta()
    {
        // Saat sapmasının toplamı. 1 haftalık limite bakıyor. Eğer limit aşıldıysa -> False.
        byte[] timeDelta = ProtectedData.Unprotect(File.ReadAllBytes(FileManager.timeDeltaPath), null, DataProtectionScope.CurrentUser);
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

    private int logTimeCheck(int seconds)
    {
        DateTime lastCheckTime = DateTime.Now;  

        EventLog log = new EventLog("Security");

        // Şu anki zamanı kaydedelim
        DateTime currentTime = DateTime.Now;

        // Sadece son `seconds` saniye içindeki olayları al
        var entries = log.Entries.Cast<EventLogEntry>()
            .Where(x => Math.Abs((x.TimeWritten-DateTime.Now).TotalSeconds)<= (seconds*1.2))
            .OrderByDescending(x => x.TimeWritten) // En yeni olaylar en başta olacak şekilde sırala
            .Select(x => new
            {
                x.InstanceId,
                x.ReplacementStrings,
                x.TimeWritten // Zaman damgasını da alın
            }).ToList();

        if (entries.Any()) // Eğer yeni olay varsa, lastCheckTime'ı güncelle
        {
            lastCheckTime = currentTime;
        }

        foreach (var entry in entries)
        {
            if (entry.InstanceId == 4616)
            {
                // Kullanıcı SID kontrolleri
                if (entry.ReplacementStrings[0].Contains("S-1-5-18") ||
                    entry.ReplacementStrings[0].Contains("S-1-5-19") ||
                    entry.ReplacementStrings[0].Contains("S-1-5-21"))
                {
                    // ReplacementStrings dizi uzunluğu kontrolü
                    if (entry.ReplacementStrings.Length > 5) // Tarih bilgileri 4. ve 5. indekslerde
                    {
                        string oldTimeStr = entry.ReplacementStrings[4]; // Eski saat
                        string newTimeStr = entry.ReplacementStrings[5]; // Yeni saat

                        DateTime oldTime;
                        DateTime newTime;

                        if (DateTime.TryParse(oldTimeStr, out oldTime) && DateTime.TryParse(newTimeStr, out newTime))
                        {
                            TimeSpan timeDifference = newTime - oldTime;
                            int totalSeconds = (int)timeDifference.TotalSeconds;

                            Logger.Log($"[Windows Security Log] Time change detected: old time:{oldTimeStr}, new time: {newTimeStr}, total seconds:{totalSeconds}");
                            return totalSeconds;
                        }
                        else
                        {
                            Logger.Log($"[TimeLog] Date format error: old time:{oldTimeStr}, new time: {newTimeStr}");
                        }
                    }
                    else
                    {
                        Logger.Log("Expected date information is missing or incorrect.");
                    }
                }
            }
        }

        return 0; // Saat değişikliği bulunamadıysa 0 döndür
    }
}
