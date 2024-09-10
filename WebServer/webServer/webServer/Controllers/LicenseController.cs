using LicenseApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;

namespace LicenseApplication.Controllers
{
    public class LicenseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LicenseController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> UserLicenceDB(string encrypted)
        {
            if (string.IsNullOrEmpty(encrypted))
            {
                return BadRequest("Username and LicenseKey are required");
            }
            //string decodedString = HttpUtility.UrlDecode(encrypted);
            byte[] encryptedByte = Convert.FromBase64String(encrypted);

            // Base64 decode işlemi

            // Veriyi şifresini çözme (burada şifre çözme işlemini uygulayın)
            var primaryKey = await _context.PrimaryKeys.Select(k => k.PrimaryPrivateKey).FirstOrDefaultAsync();
            byte[] primaryKeyByte = Convert.FromBase64String(primaryKey);

            RsaEncryption rsaEncryption = new RsaEncryption();
            byte[] decryptedData= rsaEncryption.DecrypLicence(encryptedByte,primaryKeyByte);

            byte[][] separatedInfos = RsaEncryption.Separator(decryptedData);
            byte[] usernameByte  = separatedInfos[0];
            byte[] deviceInfoByte= separatedInfos[1];

            string username = Encoding.UTF8.GetString(usernameByte);
            string deviceInfo= Encoding.UTF8.GetString(deviceInfoByte);

            var userId = await _context.Users
                .Where(u => u.Username == username)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();

            var licencePubKey= await _context.Licenses
                .Where(l => l.UserId == userId && l.DeviceInfo == deviceInfo)
                .Select(l => l.LicensePublicKey)
                .FirstOrDefaultAsync();

            DateTime ExpirationDate = await _context.Licenses
                .Where(l => l.UserId == userId && l.DeviceInfo == deviceInfo)
                .Select(l => l.ExpirationDate)
                .FirstOrDefaultAsync();
            DateTime currentDate = DateTime.Now; // Şu anki tarih


            byte[] result;
            if (licencePubKey!= null)
            {
                if (currentDate < ExpirationDate)
                {// son kullanma tarihi geçmemiş
                    result = new byte[]{1};

                }
                else
                {
                    result = new byte[] { 0 };
                }
            }
            else
            {
                result = new byte[]{0};
            }
            byte[] encResult= RsaEncryption.EncryptText(result, Convert.FromBase64String(licencePubKey));
            string encResultBase64 = Convert.ToBase64String(encResult);



            return Ok(encResultBase64);
        }
        
        [AllowAnonymous]
        public async Task<IActionResult> LicenceAktivationCheck(string state, string encrypted)
        {
            
            if (string.IsNullOrEmpty(encrypted) || string.IsNullOrEmpty(state))
            {
                return BadRequest("enc data required");
            }

            //string decodedString = HttpUtility.UrlDecode(encrypted);

            byte[] encryptedByte = Convert.FromBase64String(encrypted);

            var primaryKey = await _context.PrimaryKeys.Select(k => k.PrimaryPrivateKey).FirstOrDefaultAsync();
            byte[] primaryKeyByte = Convert.FromBase64String(primaryKey);

            RsaEncryption rsaEncryption = new RsaEncryption();
            byte[] decryptedData = rsaEncryption.DecrypLicence(encryptedByte, primaryKeyByte);

            byte[][] separatedInfos = RsaEncryption.Separator(decryptedData);
            byte[] deviceInfo = separatedInfos[0];
            byte[] data = separatedInfos[1];
            string devv = Encoding.UTF8.GetString(deviceInfo);
            if (state == "1")
            {


                byte[] result;
                byte[] combined;
                byte[] publicLicenceKey;
                //lisans kontrolü yapılacak ve yeni key iv belirlenecek.
                AESControl aesControl = new AESControl(deviceInfo, data, _context);
                if (aesControl.ControlAES())
                {
                    (byte[] key, byte[] iv, publicLicenceKey) = await aesControl.GenerateAndStoreKeyAndIV();
                    //tamamdır yeni key oluştur.
                    combined = RsaEncryption.Combiner(key, iv);

                }
                else
                {
                    var licencePubKey = await _context.Licenses
                        .Where(l => l.DeviceInfo == Encoding.UTF8.GetString(deviceInfo))
                        .Select(l => l.LicensePublicKey)
                        .FirstOrDefaultAsync();
                    publicLicenceKey = Convert.FromBase64String(licencePubKey);
                    //sıkıntı var.
                    combined = new byte[] { 0 };
                }
                result = RsaEncryption.EncryptText(combined, publicLicenceKey);
                string resultBase64 = Convert.ToBase64String(result);
                return Ok(resultBase64);
            }
            else
            {   
                AESControl aes = new AESControl();
                (byte[] key, byte[] iv) =  aes.ReadKeyAndIvFromData(data);
                
                var license = await _context.Licenses
                    .FirstOrDefaultAsync(l => l.DeviceInfo == Encoding.UTF8.GetString(deviceInfo));

                // Yeni key ve iv değerlerini atayın
                license.LicenseKey = Convert.ToBase64String(key);
                license.LicenseIv = Convert.ToBase64String(iv);

                // Veritabanına değişiklikleri kaydet
                await _context.SaveChangesAsync();
                
                
                var licencePubKey = await _context.Licenses
                        .Where(l => l.DeviceInfo == Encoding.UTF8.GetString(deviceInfo))
                        .Select(l => l.LicensePublicKey)
                        .FirstOrDefaultAsync();
                byte[] publicLicenceKey = Convert.FromBase64String(licencePubKey);

                byte[] result = RsaEncryption.EncryptText(new byte[] { 1 }, publicLicenceKey);
                string resultBase64 = Convert.ToBase64String(result);
                return Ok(resultBase64);
            }
        }
        
        [AllowAnonymous]
        public async Task<IActionResult> LicenceSetUp(string username,string sendAdmin)
        {

            if (string.IsNullOrEmpty(sendAdmin) || string.IsNullOrEmpty(username))
            {
                return BadRequest("enc data required");
            }

            //string decodedString = HttpUtility.UrlDecode(encrypted);
            byte[] encryptedByte = Convert.FromBase64String(sendAdmin);

            var primaryKey = await _context.PrimaryKeys.Select(k => k.PrimaryPrivateKey).FirstOrDefaultAsync();
            byte[] primaryKeyByte = Convert.FromBase64String(primaryKey);

            var decryptedText = RsaEncryption.DecrypLicence4096(Convert.FromBase64String(sendAdmin), primaryKeyByte);


            byte[][] veriler = RsaEncryption.Separator(decryptedText);
            byte[] deviceInfo = veriler[0];
            byte[] publicKey = veriler[1];

            var userId = await _context.Users
                .Where(u => u.Username == username)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();

            var planId = await _context.Users
                 .Where(u => u.Username == username)
                 .Select(u => u.PlanId)
                 .FirstOrDefaultAsync();
            string rr;
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();

                // Şifreleme
                byte[] encrypted = AESControl.EncryptStringToBytes_Aes(deviceInfo, aes.Key, aes.IV);

                byte[] all = RsaEncryption.Combiner(encrypted, aes.Key, aes.IV);

                byte[] encryptedText = RsaEncryption.EncryptText(all, publicKey);
                rr = Convert.ToBase64String(encryptedText);
                var existingLicense = await _context.Licenses
                    .FirstOrDefaultAsync(l => l.UserId == userId && l.DeviceInfo== Encoding.UTF8.GetString(deviceInfo));

                if (existingLicense != null)
                {
                    // Kayıt mevcutsa güncelle
                    existingLicense.LicenseKey = Convert.ToBase64String(aes.Key);
                    existingLicense.PlanId = planId;
                    existingLicense.ExpirationDate = DateTime.UtcNow.AddYears(1); // Örnek: 1 yıl geçerli
                    existingLicense.CreatedAt = DateTime.UtcNow; // Mevcut kayıt oluşturulma zamanını da güncelle
                    existingLicense.LicenseIv = Convert.ToBase64String(aes.IV);
                    existingLicense.LicensePublicKey = Convert.ToBase64String(publicKey);
                    // Veritabanında güncelleme işlemi yap
                    await _context.SaveChangesAsync();
                }
                else
                {

                    var newLicense = new Models.License
                    {
                        UserId = userId,
                        LicenseKey = Convert.ToBase64String(aes.Key),
                        PlanId = planId,
                        ExpirationDate = DateTime.UtcNow.AddYears(1),// Örnek: Lisansın 1 yıl geçerli olması
                        CreatedAt = DateTime.UtcNow,
                        LicenseIv = Convert.ToBase64String(aes.IV),
                        DeviceInfo = Encoding.UTF8.GetString(deviceInfo),
                        LicensePublicKey = Convert.ToBase64String(publicKey)
                    };
                    _context.Licenses.Add(newLicense);

                    // Değişiklikleri veritabanına kaydet
                    await _context.SaveChangesAsync();
                }
            }
            return Ok(rr);
        }
        public async Task<IActionResult> UploadFile(IFormFile fileUpload, string username)
        {
            byte[] encryptedText  = new byte[] { 0 };
            if (fileUpload != null && fileUpload.Length > 0)
            {
                // Dosyayı byte[] olarak okuma
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await fileUpload.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }
                byte[] sendAdmin = fileBytes;
                // Dosya içeriğini işleme
                

                //string decodedString = HttpUtility.UrlDecode(encrypted);
                byte[] encryptedByte =fileBytes;

                var primaryKey = await _context.PrimaryKeys.Select(k => k.PrimaryPrivateKey).FirstOrDefaultAsync();
                byte[] primaryKeyByte = Convert.FromBase64String(primaryKey);

                var decryptedText = RsaEncryption.DecrypLicence4096(sendAdmin, primaryKeyByte);


                byte[][] veriler = RsaEncryption.Separator(decryptedText);
                byte[] deviceInfo = veriler[0];
                byte[] publicKey = veriler[1];

                var userId = await _context.Users
                    .Where(u => u.Username == username)
                    .Select(u => u.UserId)
                    .FirstOrDefaultAsync();

                var planId = await _context.Users
                     .Where(u => u.Username == username)
                     .Select(u => u.PlanId)
                     .FirstOrDefaultAsync();
                string rr;
                using (Aes aes = Aes.Create())
                {
                    aes.GenerateKey();
                    aes.GenerateIV();

                    // Şifreleme
                    byte[] encrypted = AESControl.EncryptStringToBytes_Aes(deviceInfo, aes.Key, aes.IV);

                    byte[] all = RsaEncryption.Combiner(encrypted, aes.Key, aes.IV);

                    encryptedText = RsaEncryption.EncryptText(all, publicKey);
                    rr = Convert.ToBase64String(encryptedText);
                    var existingLicense = await _context.Licenses
                        .FirstOrDefaultAsync(l => l.UserId == userId && l.DeviceInfo == Encoding.UTF8.GetString(deviceInfo));

                    if (existingLicense != null)
                    {
                        // Kayıt mevcutsa güncelle
                        existingLicense.LicenseKey = Convert.ToBase64String(aes.Key);
                        existingLicense.PlanId = planId;
                        existingLicense.ExpirationDate = DateTime.UtcNow.AddYears(1); // Örnek: 1 yıl geçerli
                        existingLicense.CreatedAt = DateTime.UtcNow; // Mevcut kayıt oluşturulma zamanını da güncelle
                        existingLicense.LicenseIv = Convert.ToBase64String(aes.IV);
                        existingLicense.LicensePublicKey = Convert.ToBase64String(publicKey);
                        // Veritabanında güncelleme işlemi yap
                        await _context.SaveChangesAsync();
                    }
                    else
                    {

                        var newLicense = new Models.License
                        {
                            UserId = userId,
                            LicenseKey = Convert.ToBase64String(aes.Key),
                            PlanId = planId,
                            ExpirationDate = DateTime.UtcNow.AddYears(1),// Örnek: Lisansın 1 yıl geçerli olması
                            CreatedAt = DateTime.UtcNow,
                            LicenseIv = Convert.ToBase64String(aes.IV),
                            DeviceInfo = Encoding.UTF8.GetString(deviceInfo),
                            LicensePublicKey = Convert.ToBase64String(publicKey)
                        };
                        _context.Licenses.Add(newLicense);

                        // Değişiklikleri veritabanına kaydet
                        await _context.SaveChangesAsync();
                    }
                }


            }
                    return File(encryptedText, "application/octet-stream", "getLicence.dat");

        }

        [AllowAnonymous]
        public IActionResult GetTime()
        {
            try
            {
                // UTC zamanını al
                var utcTime = DateTime.UtcNow;

                // JSON olarak zamanı döndür
                return Ok(new { utc_time = utcTime });
            }
            catch (Exception ex)
            {
                // Hata durumunda, uygun bir hata mesajı döndür
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }
        public IActionResult CreateNewPrimaryKeys()
        {
            (byte[] primaryPublicKey,byte[] primaryPrivateKey) =  RsaEncryption.CreatePrimaryKeyPair();
            PrimaryKeys newKeyPair = new PrimaryKeys
            {
                PrimaryPublicKey = Convert.ToBase64String(primaryPublicKey),
                PrimaryPrivateKey =  Convert.ToBase64String(primaryPrivateKey),
            };

            _context.PrimaryKeys.Add(newKeyPair);
            _context.SaveChanges();
            return RedirectToAction("index","Profile");
        }

       
    }
}
