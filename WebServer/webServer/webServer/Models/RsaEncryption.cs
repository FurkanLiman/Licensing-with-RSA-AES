using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace LicenseApplication.Models
{
    public class RsaEncryption
    {
        private readonly string publicKey;
        private readonly string privateKey;

        public RsaEncryption()
        {
        }

        public (byte[], byte[]) getKeys()
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {

                byte[] publicKey = rsa.ExportCspBlob(false);
                byte[] privateKey = rsa.ExportCspBlob(true);

                return (publicKey, privateKey);
            }
        }

        public static byte[] Combiner(params byte[][] arrays)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                foreach (byte[] array in arrays)
                {
                    // Her bir verinin uzunluğunu başına yaz
                    writer.Write(array.Length);
                    // Veriyi yaz
                    writer.Write(array);
                }
                // Birleştirilmiş byte dizisini döndür
                return ms.ToArray();
            }
        }

        public static byte[][] Separator(byte[] combinedData)
        {
            using (MemoryStream ms = new MemoryStream(combinedData))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                var separatedData = new System.Collections.Generic.List<byte[]>();

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    // Verinin uzunluğunu oku
                    int length = reader.ReadInt32();
                    // Veriyi oku
                    byte[] data = reader.ReadBytes(length);
                    // Veriyi listeye ekle
                    separatedData.Add(data);
                }

                return separatedData.ToArray();
            }
        }

        public static byte[] EncryptText(byte[] data, byte[] publicKey)
        {
            using (var rsa = new RSACryptoServiceProvider(1024))
            {
                rsa.ImportCspBlob(publicKey);
                byte[] encryptedData = rsa.Encrypt(data, false);
                return encryptedData;
            }
        }

        public byte[] DecrypLicence(byte[] encryptedText, byte[] privateKey)
        {
            using (var rsa = new RSACryptoServiceProvider(1024))
            {
                rsa.ImportCspBlob(privateKey);
                byte[] decryptedData = rsa.Decrypt(encryptedText, false);

                return decryptedData;

            }
        }
        public static byte[] DecrypLicence4096(byte[] encryptedBytes, byte[] privateKey)
        {
            using (var rsa = new RSACryptoServiceProvider(4096))
            {
                rsa.ImportCspBlob(privateKey);
                var decryptedBytes = rsa.Decrypt(encryptedBytes, false);
                return decryptedBytes;
            }
        }
        public static (byte[], byte[]) CreatePrimaryKeyPair()
        {
            using (var rsa = new RSACryptoServiceProvider(4096))
            {
                //rsa.PersistKeyInCsp = false; // Anahtarları Csp'de saklamayı kapat
                byte[] publicKey = rsa.ExportCspBlob(false); // Sadece public key
                byte[] privateKey = rsa.ExportCspBlob(true); // Hem public hem de private key

                // Anahtarları dosyalara yaz
                return (publicKey, privateKey);
            }
        }
    }
}


