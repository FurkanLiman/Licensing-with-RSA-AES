using LicenseApplication.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Security.Cryptography.Xml;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using NuGet.Packaging.Licenses;

namespace LicenseApplication.Models
{
    public class AESControl
    {
        private readonly byte[] deviceInfo;
        private readonly byte[] key;
        private readonly byte[] iv;
        private readonly byte[] encryptedTextFromFile;
        private readonly ApplicationDbContext _context;

        public AESControl()
        {

        }

        public bool Control(byte[] deviceInfo, byte[] Key, byte[] IV, byte[] licenceData)
        {
            byte[] encryptedLicence = EncryptStringToBytes_Aes(deviceInfo, Key, IV);
            return encryptedLicence.SequenceEqual(licenceData);
        }
        
        public AESControl(byte[] deviceInfo, byte[] encryptedTextFromFile, ApplicationDbContext context)
        {
            _context = context;
            (this.key,this.iv) = checkaes(deviceInfo).Result; 
            this.deviceInfo = deviceInfo;
            this.encryptedTextFromFile = encryptedTextFromFile;
        }

        private async Task<(byte[], byte[])> checkaes(byte[] deviceInfo)
        {
            string deviceInfoS = Encoding.UTF8.GetString(deviceInfo);
            var KeyIV = await _context.Licenses
                .Where(l => l.DeviceInfo == deviceInfoS)
                .Select(l => new
                {
                    keys = l.LicenseKey,
                    ivs = l.LicenseIv
                })
                .FirstOrDefaultAsync();

            string keyS = KeyIV.keys;
            string ivS = KeyIV.ivs;
            byte[] key = Convert.FromBase64String(keyS);
            byte[] iv = Convert.FromBase64String(ivS);
            return (key,iv);
        }
        
        public bool ControlAES()
        {
            byte[] encryptedText = EncryptStringToBytes_Aes(this.deviceInfo, this.key, this.iv);
            return encryptedText.SequenceEqual(encryptedTextFromFile);
        }

        /*private bool ControlandRegenerate(byte[] encryptedText)
        {
            // Şifreli metinleri karşılaştır
            bool isMatch = encryptedText.SequenceEqual(encryptedTextFromFile);

            if (isMatch)
            {
                Console.WriteLine("AES check completed successfully.");
                byte[] key;
                byte[] iv;
                // Yeni key ve IV oluştur
                GenerateAndStoreKeyAndIV();

                // Metni yeni key ve IV ile şifrele
                encryptedText = EncryptStringToBytes_Aes(deviceInfo, key, iv);

                // Şifrelenmiş metni dosyaya yaz
                File.WriteAllBytes(FileManager.AESTextPath, ProtectedData.Protect(encryptedText, null, DataProtectionScope.CurrentUser));

                

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

        }*/
        
        public async Task<(byte[], byte[], byte[])> GenerateAndStoreKeyAndIV()
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.GenerateKey();
                aesAlg.GenerateIV();
                byte[] key = aesAlg.Key;
                byte[] iv = aesAlg.IV;
                var license = await _context.Licenses
                    .FirstOrDefaultAsync(l => l.DeviceInfo == Encoding.UTF8.GetString(this.deviceInfo));

                // Kayıt bulunamazsa hata döndür
                if (license == null)
                {
                    //
                }

                // Yeni key ve iv değerlerini atayın
                license.LicenseKey = Convert.ToBase64String(key);
                license.LicenseIv = Convert.ToBase64String(iv);

                // Veritabanına değişiklikleri kaydet
                await _context.SaveChangesAsync();
                return (key, iv, Convert.FromBase64String(license.LicensePublicKey));
            }
        }
        public static byte[] EncryptStringToBytes_Aes(byte[] deviceInfo, byte[] Key, byte[] IV)
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

        /*public string DecryptStringFromBytes_Aes(string cipherText)
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
        }*/
        
        private static void ClearBytes(byte[] buffer)
        {
            if (buffer != null)
            {
                Array.Clear(buffer, 0, buffer.Length);
            }
        }

        public (byte[] key, byte[] iv) ReadKeyAndIvFromData(byte[] combined)
        {
            // Anahtar ve IV uzunlukları bilindiği için ayır
            byte[] key = new byte[32]; // AES-256 için 32 byte
            byte[] iv = new byte[16];  // AES için 16 byte IV

            Buffer.BlockCopy(combined, 0, key, 0, key.Length);
            Buffer.BlockCopy(combined, key.Length, iv, 0, iv.Length);

            return (key, iv);
        }
    }
}

