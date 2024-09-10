using LicenceApp;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web;

namespace RSA_Cryptography
{
    internal class AESControl
    {
        private readonly byte[] deviceInfo;
        private readonly byte[] key;
        private readonly byte[] iv;
        private readonly byte[] encryptedTextFromFile;
        private readonly string connectionLink = "https://localhost:7133";
        public AESControl()
        {

        }
        public bool Control(byte[] deviceInfo, byte[] Key, byte[] IV, byte[] licenceData)
        {
            byte[] encryptedLicence = EncryptStringToBytes_Aes(deviceInfo, Key, IV);
            return encryptedLicence.SequenceEqual(licenceData);
        }
        public AESControl(byte[] deviceInfo,string state)
        {

            if (isFilesCorrupted())
            {

                (this.key, this.iv) = ReadKeyAndIvFromFile();
                this.deviceInfo = deviceInfo;
                this.encryptedTextFromFile = File.ReadAllBytes(FileManager.AESTextPath);
                Logger.Log(Convert.ToBase64String(key), Convert.ToBase64String(iv), Convert.ToBase64String(deviceInfo),state);
            }
            else
            {
                Logger.Log("[AES] files been corrupted.");
            }

        }
        public bool ControlAES()
        {
            byte[] encryptedText = EncryptStringToBytes_Aes(this.deviceInfo, this.key, this.iv);

            return ControlandRegenerate(encryptedText);

        }
        public async Task<bool> OnlineAESCheck()
        {
            string state = GetLastAESLogState();
            DeviceInfo deviceInfo = new DeviceInfo();
            byte[] ss = deviceInfo.getInfoBytes();
            RSAEncryption rsa = new RSAEncryption();
            byte[] combined;
            byte[] keyIv = null;
            if (state == "1")
            {//web kontrol etmiş
                byte[] encAES= File.ReadAllBytes(FileManager.AESTextPath);
                combined = rsa.Combiner(deviceInfo.getInfoBytes(),encAES);
                Logger.Log("[Network] Licence check request...");
                Console.WriteLine("?Licence check request...");
            }
            else
            {//offline kontrol olmuş
                keyIv = File.ReadAllBytes(FileManager.AESKeyIVPath);
                combined = rsa.Combiner(deviceInfo.getInfoBytes(),keyIv);
                Logger.Log("[Network] Offline key-iv sync request...");
                Console.WriteLine("?Offline key-iv sync request...");
            }
            byte[] encrypted = rsa.EncryptText(combined, File.ReadAllBytes(FileManager.primaryPublicKeyPath));
            string encodedEncrypted = HttpUtility.UrlEncode(Convert.ToBase64String(encrypted));
            string url = $"{connectionLink}/license/LicenceAktivationCheck?state={state}&encrypted={encodedEncrypted}";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string encResult = await response.Content.ReadAsStringAsync();
                        byte[] result = rsa.DecryptResult(Convert.FromBase64String(encResult));
            
                        if (result.Length == 1 && result[0] == 0)
                        {
                            Logger.Log("[Network] Licence Check Failed.");
                            Console.WriteLine("-Licence Check Failed.");
                            return false;
                        }else if (result.Length == 1 && result[0] == 1)
                        {
                            if(keyIv != null)
                            {

                                (byte[] key, byte[] iv)= ReadKeyAndIvFromFile();
                                byte[] encryptedText = EncryptStringToBytes_Aes(deviceInfo.getInfoBytes(), key, iv);
                                File.WriteAllBytes(FileManager.AESTextPath, encryptedText);
                                Logger.Log(Convert.ToBase64String(key), Convert.ToBase64String(iv),Convert.ToBase64String(deviceInfo.getInfoBytes()),"1");
                                Logger.Log("[Network] key-iv sync completed successfully.");
                                Console.WriteLine("+Sync completed successfully.");
                            }
                            //senkronizasyon yaptı tekrar request
                            return true;
                        }
                        else
                        {
                            //result aldı
                            byte[][] separated = rsa.Separator(result);
                            AESControl aes = new AESControl();
                            aes.WriteKeyAndIvToFile(separated[0], separated[1]);
                            byte[] encryptedText = EncryptStringToBytes_Aes(deviceInfo.getInfoBytes(), separated[0], separated[1]);
                            File.WriteAllBytes(FileManager.AESTextPath, encryptedText);
                            Logger.Log(Convert.ToBase64String(separated[0]), Convert.ToBase64String(separated[1]), Convert.ToBase64String(deviceInfo.getInfoBytes()), "1");
                            Logger.Log("[Network] licence checked successfully. New key and iv saved.");
                            Console.WriteLine("+Licence checked successfully. New key and iv saved.");
                            return true;
                        }
                    }
                    else
                    {
                        Logger.Log($"[Network] Response Error: {response.StatusCode}");
                        Console.WriteLine($"*Error: {response.StatusCode}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"[Network] Request Error: {ex.Message}");
                    Console.WriteLine($"*Error during request: {ex.Message}");
                    return false;
                }
            }
        }

        static string GetLastAESLogState()
        {
            if (!File.Exists(FileManager.LogFilePath))
            {
                return null; // Log dosyası yoksa null döndür
            }

            string[] logLines = File.ReadAllLines(FileManager.LogFilePath);
            string lastAESLog = null;

            // AES loglarını ve state değerini bulmak için regex deseni
            string aesLogPattern = @"\[AES\].*state=(\d+)";

            for (int i = logLines.Length - 1; i >= 0; i--)
            {
                if (Regex.IsMatch(logLines[i], aesLogPattern))
                {
                    lastAESLog = logLines[i];
                    break;
                }
            }

            if (lastAESLog != null)
            {
                // 'state' değerini çekmek için regex
                Match match = Regex.Match(lastAESLog, aesLogPattern);
                if (match.Success)
                {
                    return match.Groups[1].Value; // State değerini döndür
                }
            }

            return null; // Eğer AES logu veya state değeri yoksa null döndür
        }

        public void WriteKeyAndIvToFile(byte[] key, byte[] iv)
        {
            // Anahtar ve IV'yi birleştirip yaz
            byte[] combined = new byte[key.Length + iv.Length];
            Buffer.BlockCopy(key, 0, combined, 0, key.Length);
            Buffer.BlockCopy(iv, 0, combined, key.Length, iv.Length);
            File.WriteAllBytes(FileManager.AESKeyIVPath, combined);
        }
        private static (byte[] key, byte[] iv) ReadKeyAndIvFromFile()
        {
            byte[] combined = File.ReadAllBytes(FileManager.AESKeyIVPath);

            // Anahtar ve IV uzunlukları bilindiği için ayır
            byte[] key = new byte[32]; // AES-256 için 32 byte
            byte[] iv = new byte[16];  // AES için 16 byte IV

            Buffer.BlockCopy(combined, 0, key, 0, key.Length);
            Buffer.BlockCopy(combined, key.Length, iv, 0, iv.Length);

            return (key, iv);
        }
        private bool isFilesCorrupted()
        {
            if (File.Exists(FileManager.AESKeyIVPath) && File.Exists(FileManager.AESTextPath))
            {
                return true;
            }
            else
            {
                Console.WriteLine("Files Been Corrupted! Please Contact With Manager!");
                return false;
            }
        }
        private bool ControlandRegenerate(byte[] encryptedText)
        {
            // Şifreli metinleri karşılaştır
            bool isMatch = encryptedText.SequenceEqual(encryptedTextFromFile); ;

            if (isMatch)
            {
                Console.WriteLine("AES check completed successfully.");
                byte[] key;
                byte[] iv;
                // Yeni key ve IV oluştur
                GenerateAndStoreKeyAndIV(out key, out iv);

                // Metni yeni key ve IV ile şifrele
                encryptedText = EncryptStringToBytes_Aes(deviceInfo, key, iv);

                // Şifrelenmiş metni dosyaya yaz
                File.WriteAllBytes(FileManager.AESTextPath, encryptedText);

                /*
                Console.WriteLine("New key and IV created and file encrypted.");
                Console.WriteLine($"New Key: {Convert.ToBase64String(key)}");
                Console.WriteLine($"New  IV: {Convert.ToBase64String(iv)}");
                */

                //  Bellek temizliği
                ClearBytes(key);
                ClearBytes(iv);
                return true;
            }
            else
            {
                Console.WriteLine("Licence is expired or corrupted! Contact With Manager.");
                return false;
            }

        }
        private void GenerateAndStoreKeyAndIV(out byte[] key, out byte[] iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.GenerateKey();
                aesAlg.GenerateIV();
                key = aesAlg.Key;
                iv = aesAlg.IV;

                // Yeni key ve IV değerlerini güvenli bir şekilde dosyaya yaz
                WriteKeyAndIvToFile(key, iv);
            }
        }
        private static byte[] EncryptStringToBytes_Aes(byte[] deviceInfo, byte[] Key, byte[] IV)
        {

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(deviceInfo);
                        }
                        return msEncrypt.ToArray();
                    }
                }
            }
        }
        public string DecryptStringFromBytes_Aes(string cipherText)
        {
            DeviceInfo device = new DeviceInfo();

            int middle = device.InfoHash.Length / 2;

            byte[] Key = new byte[middle];
            byte[] IV = new byte[middle];

            // İlk yarıyı kopyala
            Array.Copy(device.InfoHash, 0, Key, 0, middle);

            // İkinci yarıyı kopyala
            Array.Copy(device.InfoHash, middle, IV, 0, middle);
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
        private static void ClearBytes(byte[] buffer)
        {
            if (buffer != null)
            {
                Array.Clear(buffer, 0, buffer.Length);
            }
        }
    }
}